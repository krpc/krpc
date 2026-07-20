import math

import krpctest


def magnitude(vector):
    return math.sqrt(sum(x * x for x in vector))


class TestVesselPhysicsRange(krpctest.TestCase):
    """Tests Vessel.physics_range against a second vessel held at a controlled distance.

    Both halves of Multi.craft are undocked into two vessels sharing a circular orbit,
    and the active vessel is teleported along that orbit to place the other one. The
    thresholds are only evaluated against non-active vessels, so a second vessel is the
    only way to exercise them.
    """

    # A vessel is put on rails at 350m in orbit, so it is loaded but frozen well inside
    # the 2.5km unload distance. This is the distance the tests use to show that.
    PACKED_DISTANCE = 1000
    RANGE = 5000

    # Teleporting calls PrepTeleport, which puts vessels on rails and then blocks
    # unpacking for a countdown of physics frames, so the state needs time to settle.
    SETTLE = 4

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        next(iter(cls.space_center.active_vessel.parts.docking_ports)).undock()
        cls.wait(1)
        cls.vessel = cls.space_center.active_vessel
        cls.other = next(v for v in cls.space_center.vessels if v != cls.vessel)
        cls.semi_major_axis = cls.other.orbit.semi_major_axis
        cls.epoch = cls.other.orbit.epoch
        cls.anomaly_zero = cls.calibrate()

    def tearDown(self):
        self.other.physics_range = 0

    @classmethod
    def set_anomaly(cls, anomaly):
        cls.set_orbit("Kerbin", cls.semi_major_axis, 0, 0, 0, 0, anomaly, cls.epoch)
        cls.wait(cls.SETTLE)

    @classmethod
    def calibrate(cls):
        """Find the mean anomaly at epoch that puts the active vessel alongside the other
        one. A circular equatorial orbit is degenerate in its angles - the game reports a
        non-zero longitude of ascending node and argument of periapsis with the mean
        anomaly compensating - so the phase cannot be read off the other vessel's
        elements and is measured instead. The chord between two placements on the same
        orbit is 2a*sin(offset/2), which gives the size of the offset; a second placement
        resolves its sign."""
        cls.set_anomaly(0.0)
        distance = magnitude(cls.other.position(cls.vessel.reference_frame))
        offset = 2 * math.asin(min(1.0, distance / (2 * cls.semi_major_axis)))
        cls.set_anomaly(offset)
        if magnitude(cls.other.position(cls.vessel.reference_frame)) > distance:
            offset = -offset
        return offset

    def place_at(self, distance):
        """Teleport the active vessel so the other vessel sits at roughly the given
        distance, and return the distance actually achieved."""
        self.set_anomaly(self.anomaly_zero + distance / self.semi_major_axis)
        return self.distance()

    def distance(self):
        return magnitude(self.other.position(self.vessel.reference_frame))

    def assert_state(self, loaded, packed, message):
        """Assert the other vessel's loaded and packed state, and check the game's
        invariant that an unpacked vessel is always loaded."""
        self.assertEqual(loaded, self.other.loaded, "loaded, " + message)
        self.assertEqual(packed, self.other.packed, "packed, " + message)
        if not self.other.packed:
            self.assertTrue(self.other.loaded, "unpacked but not loaded, " + message)

    def rcs_speed_change(self):
        """Run the other vessel's RCS at full forward translation and return the change
        in its orbital speed. A packed vessel has kinematic rigidbodies and discards all
        forces, so this is zero however successfully the commands are issued."""
        before = self.other.orbit.speed
        self.other.control.rcs = True
        self.other.control.forward = 1.0
        self.wait(5)
        after = self.other.orbit.speed
        self.other.control.forward = 0.0
        self.other.control.rcs = False
        return abs(after - before)

    def test_default_is_the_pack_distance(self):
        # In orbit the game packs a vessel at 350m, long before it unloads at 2500m, so
        # the physics range is the much shorter of the two.
        self.assertEqual(
            self.space_center.VesselSituation.orbiting, self.other.situation
        )
        self.assertAlmostEqual(350, self.other.physics_range, places=1)

    def test_set_and_clear(self):
        self.other.physics_range = self.RANGE
        self.assertAlmostEqual(self.RANGE, self.other.physics_range, places=1)
        # The addon re-applies the range every physics tick; it must survive that.
        self.wait(1)
        self.assertAlmostEqual(self.RANGE, self.other.physics_range, places=1)
        self.other.physics_range = 0
        self.assertAlmostEqual(350, self.other.physics_range, places=1)

    def test_stock_vessel_is_packed_within_loading_range(self):
        # Loaded but on rails: the state the physics range exists to avoid.
        self.place_at(self.PACKED_DISTANCE)
        self.assert_state(True, True, "stock ranges at ~1km")

    def test_override_keeps_vessel_in_physics(self):
        # The whole point of the feature. Same vessel, same distance, and the only
        # difference is the physics range: without it the vessel is frozen and ignores
        # its controls, with it the vessel is live and its RCS actually moves it.
        self.place_at(self.PACKED_DISTANCE)
        self.assert_state(True, True, "stock ranges at ~1km")
        self.assertEqual(
            self.space_center.ControlState.full,
            self.other.control.state,
            "a packed vessel still reports itself fully controllable",
        )
        self.assertLess(
            self.rcs_speed_change(),
            0.01,
            "a packed vessel accepts control input but does not move",
        )

        self.other.physics_range = self.RANGE
        self.wait(self.SETTLE)
        self.assert_state(True, False, "physics range raised at ~1km")
        self.assertGreater(
            self.rcs_speed_change(),
            0.5,
            "an unpacked vessel responds to control input",
        )

    def test_override_moves_the_thresholds(self):
        # The range is applied as pack = unload = range, and load = unpack = 0.9 * range,
        # so well inside it the vessel runs physics and well outside it unloads.
        self.other.physics_range = self.RANGE
        self.wait(self.SETTLE)
        self.place_at(3000)
        self.assert_state(True, False, "inside a raised physics range")
        self.place_at(int(self.RANGE * 1.5))
        self.assert_state(False, True, "outside a raised physics range")
        # Coming back inside restores it, so the range survives an unload/load cycle.
        self.place_at(3000)
        self.assert_state(True, False, "back inside a raised physics range")


class TestVesselPhysicsRangeSceneChange(krpctest.TestCase):
    """A scene change destroys the vessels and rebuilds them from their protovessels,
    and a rebuilt vessel re-copies the game's default ranges in Vessel.Awake. The range
    has to be re-applied for it to survive, which is what the addon is for. Uses the
    active vessel, the one vessel guaranteed to survive the round trip."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center

    def test_survives_scene_change(self):
        krpc = self.connect().krpc
        self.space_center.active_vessel.physics_range = 5000

        krpc.game_scene = krpc.GameScene.space_center
        self.wait_until(
            lambda: krpc.game_scene == krpc.GameScene.space_center,
            message="never reached the space center",
        )
        krpc.game_scene = krpc.GameScene.flight
        self.wait_until(
            lambda: krpc.game_scene == krpc.GameScene.flight,
            message="never returned to flight",
        )
        self.wait(3)

        vessel = self.space_center.active_vessel
        # physics_range reads the vessel's live ranges rather than the requested value,
        # so this only passes if the range really was re-applied to the rebuilt vessel.
        self.assertAlmostEqual(5000, vessel.physics_range, places=1)
        vessel.physics_range = 0
        self.assertAlmostEqual(350, vessel.physics_range, places=1)
