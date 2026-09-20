import time
import unittest

import krpctest
from krpctest.geometry import cross, distance, norm, vector

# Points to try for a stretch of deep water, in order. The first with a sea bed well
# below sea level is used, so a candidate that turns out to be land or shoreline is
# skipped rather than failing the run.
OCEAN_CANDIDATES = [
    (0, -60),
    (0, -40),
    (0, -20),
    (10, -50),
    (-10, -50),
    (0, 100),
]


class TestPartsBuoyancy(krpctest.TestCase):
    settled = False

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        active_vessel = cls.space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Buoyancy":
            cls.launch_vessel_from_vab("Buoyancy")
            cls.remove_other_vessels()
        cls.kerbin = cls.space_center.bodies["Kerbin"]
        cls.vessel = cls.space_center.active_vessel

    def ocean_point(self):
        for latitude, longitude in OCEAN_CANDIDATES:
            if self.kerbin.bedrock_height(latitude, longitude) < -100:
                return latitude, longitude
        self.fail("no deep water found among the candidate points")

    def afloat(self):
        """Put the vessel on the ocean and wait for it to settle, then return its flight
        telemetry in the surface reference frame."""
        flight = self.vessel.flight(self.vessel.surface_reference_frame)
        if TestPartsBuoyancy.settled:
            return flight
        latitude, longitude = self.ocean_point()
        self.set_landed("Kerbin", latitude, longitude)
        self.wait_until(
            lambda: self.vessel.situation == self.space_center.VesselSituation.splashed,
            timeout=60,
            message="vessel did not reach the water",
        )
        # The craft is dropped in as a vertical stack. It plunges, bobs back up and
        # topples onto its side, so wait for both motions to die away before reading
        # any force. A single quiet sample lands at the top of a bob, so hold out for
        # a couple of seconds of calm.
        self.wait_for_calm()
        TestPartsBuoyancy.settled = True
        return flight

    def wait_for_calm(self, timeout=180):
        """Block until the vessel stops rising, falling and rolling. The depth is what
        settles, and it is read rather than the vertical speed: the surface reference
        frame is anchored to the vessel, so every speed in it is zero."""
        flight = self.vessel.flight(self.vessel.surface_reference_frame)
        frame = self.vessel.orbital_reference_frame
        calm = 0
        last = None
        deadline = time.time() + timeout
        while time.time() < deadline:
            depth = flight.depth
            still = (
                last is not None
                and abs(depth - last) < 0.01
                and norm(vector(self.vessel.angular_velocity(frame))) < 0.05
            )
            last = depth
            calm = calm + 1 if still else 0
            if calm >= 15:
                return
            time.sleep(0.2)
        self.fail("vessel did not settle on the water")

    def test_ocean_properties(self):
        self.assertTrue(self.kerbin.has_ocean)
        self.assertAlmostEqual(1000, self.kerbin.ocean_density, delta=1)
        self.assertFalse(self.space_center.bodies["Mun"].has_ocean)
        self.assertEqual(0, self.space_center.bodies["Mun"].ocean_density)
        self.assertAlmostEqual(1.2, self.space_center.buoyancy_scalar, delta=0.001)

    def test_part_displacement(self):
        for part in self.vessel.parts.all:
            self.assertGreater(part.displacement, 0)
            self.assertGreater(part.buoyancy_multiplier, 0)
        total = sum(part.displacement for part in self.vessel.parts.all)
        self.assertAlmostEqual(total, self.vessel.flight().displacement, delta=1e-6)

    def test_part_buoyant_force_at(self):
        density = self.kerbin.ocean_density
        gravity = self.kerbin.surface_gravity
        scalar = self.space_center.buoyancy_scalar
        for part in self.vessel.parts.all:
            expected = (
                part.displacement
                * density
                * gravity
                * scalar
                * part.buoyancy_multiplier
            )
            self.assertAlmostEqual(
                expected, part.buoyant_force_at(density, gravity), delta=expected * 1e-6
            )

    def test_dry_vessel(self):
        if TestPartsBuoyancy.settled:
            self.skipTest("vessel is already in the water")
        flight = self.vessel.flight(self.vessel.surface_reference_frame)
        for part in self.vessel.parts.all:
            self.assertFalse(part.splashed)
            self.assertEqual(0, part.submerged_portion)
            self.assertEqual(0, part.displaced_volume)
            self.assertAlmostEqual(
                0, norm(vector(part.buoyant_force(self.vessel.reference_frame)))
            )
        self.assertEqual(0, flight.displaced_volume)
        self.assertEqual(0, flight.submerged_portion)
        self.assertAlmostEqual(0, norm(vector(flight.buoyant_force)))
        self.assertIsNone(flight.center_of_buoyancy)
        # Water pressure is charged to the submerged parts, and there are none
        for part in self.vessel.parts.all:
            self.assertEqual(0, part.submerged_dynamic_pressure)
        self.assertEqual(0, flight.submerged_dynamic_pressure)

    def test_floating_equilibrium(self):
        flight = self.afloat()
        weight = self.vessel.mass * self.kerbin.surface_gravity
        force = norm(vector(flight.buoyant_force))
        self.assertAlmostEqual(weight, force, delta=weight * 0.05)
        # The buoyant force holds the vessel up, so it points away from the body. The
        # surface reference frame's x axis is up.
        self.assertGreater(flight.buoyant_force[0], 0)
        self.assertAlmostEqual(
            force / self.vessel.mass,
            norm(vector(flight.buoyant_acceleration)),
            delta=0.01,
        )

    def test_floating_submersion(self):
        flight = self.afloat()
        self.assertGreater(flight.displaced_volume, 0)
        self.assertLess(flight.displaced_volume, flight.displacement)
        self.assertGreater(flight.submerged_portion, 0)
        self.assertLess(flight.submerged_portion, 1)
        self.assertTrue(any(part.splashed for part in self.vessel.parts.all))
        for part in self.vessel.parts.all:
            self.assertGreaterEqual(part.submerged_portion, 0)
            self.assertLessEqual(part.submerged_portion, 1)
            self.assertLessEqual(part.min_depth, part.max_depth)

    def test_part_centers(self):
        self.afloat()
        # Both points are built from a part-local offset, so each one lands inside
        # the part it belongs to. A world space figure would read hundreds of
        # kilometers out.
        for part in self.vessel.parts.all:
            frame = part.reference_frame
            lower, upper = part.bounding_box(frame)
            for point in (
                part.center_of_buoyancy(frame),
                part.center_of_displacement(frame),
            ):
                self.assertEqual(3, len(point))
                for i in range(3):
                    self.assertGreaterEqual(point[i], lower[i] - 0.5)
                    self.assertLessEqual(point[i], upper[i] + 0.5)

    def test_floating_trim(self):
        flight = self.afloat()
        center_of_buoyancy = flight.center_of_buoyancy
        self.assertIsNotNone(center_of_buoyancy)
        center_of_mass = flight.center_of_mass
        # A hull at rest carries its center of buoyancy on the same vertical line as
        # its center of mass. Any horizontal offset is a couple that rolls or pitches
        # the hull until the two line up, so at equilibrium it is small. The surface
        # reference frame's x axis is up, and its y and z axes span the horizontal.
        offset = norm(
            (
                center_of_buoyancy[1] - center_of_mass[1],
                center_of_buoyancy[2] - center_of_mass[2],
            )
        )
        self.assertLess(offset, 0.5)
        # The two are not the same point: the center of buoyancy is the centroid of
        # the submerged hull, and the center of mass is the centroid of its mass
        self.assertGreater(abs(center_of_buoyancy[0] - center_of_mass[0]), 0.001)

    def test_floating_depth(self):
        flight = self.afloat()
        self.assertLess(abs(flight.depth), 5)
        submerged = [part for part in self.vessel.parts.all if part.splashed]
        self.assertTrue(submerged)
        for part in submerged:
            self.assertGreater(part.max_depth, 0)
            self.assertGreaterEqual(part.submerged_dynamic_pressure, 0)
            self.assertGreater(part.submerged_drag_multiplier, 0)
        # A settled hull is barely moving, so it carries almost no water pressure
        self.assertGreaterEqual(flight.submerged_dynamic_pressure, 0)
        self.assertLess(flight.submerged_dynamic_pressure, 1000)

    def test_floating_torque(self):
        flight = self.afloat()
        frame = self.vessel.surface_reference_frame
        center_of_mass = vector(flight.center_of_mass)
        expected = vector((0.0, 0.0, 0.0))
        for part in self.vessel.parts.all:
            lever = vector(part.center_of_buoyancy(frame)) - center_of_mass
            expected = expected + vector(cross(lever, part.buoyant_force(frame)))
        # Allow for the hull rocking between the reads, as a few centimeters of lever
        # on the weight
        weight = self.vessel.mass * self.kerbin.surface_gravity
        self.assertAlmostEqual(
            tuple(expected), flight.buoyant_torque, delta=weight * 0.05
        )

    def test_floating_fully_submerged(self):
        flight = self.afloat()
        density = self.kerbin.ocean_density
        gravity = self.kerbin.surface_gravity
        force = flight.buoyant_force_at(density, gravity)
        total = sum(
            part.buoyant_force_at(density, gravity) for part in self.vessel.parts.all
        )
        self.assertAlmostEqual(total, force, delta=force * 1e-6)
        # The center stays inside the hull whatever the waterline
        center_of_buoyancy = flight.fully_submerged_center_of_buoyancy
        lower, upper = self.vessel.bounding_box(self.vessel.surface_reference_frame)
        for i in range(3):
            self.assertGreaterEqual(center_of_buoyancy[i], lower[i])
            self.assertLessEqual(center_of_buoyancy[i], upper[i])
        # The force pushes up through that center. The surface reference frame's x
        # axis is up.
        lever = vector(center_of_buoyancy) - vector(flight.center_of_mass)
        expected = cross(lever, (force, 0, 0))
        self.assertAlmostEqual(
            expected, flight.buoyant_torque_at(density, gravity), delta=force * 0.01
        )

    def test_floating_static_pressure(self):
        flight = self.afloat()
        frame = self.kerbin.reference_frame
        submerged = 0
        for part in self.vessel.parts.all:
            self.assertGreater(part.max_pressure, 0)
            altitude = self.kerbin.altitude_at_position(part.position(frame), frame)
            expected = self.kerbin.pressure_at(altitude)
            self.assertAlmostEqual(
                expected, part.static_pressure, delta=max(expected * 0.01, 100)
            )
            if altitude < 0:
                submerged += 1
                self.assertGreater(part.static_pressure, self.kerbin.pressure_at(0))
        self.assertGreater(submerged, 0)
        expected = self.kerbin.pressure_at(flight.mean_altitude)
        self.assertAlmostEqual(
            expected, flight.static_pressure, delta=max(expected * 0.01, 100)
        )

    def test_pressure_at_depth(self):
        # Water adds its weight per meter of depth. The atmosphere curve extrapolated
        # below sea level adds a few kilopascals more.
        water = self.kerbin.ocean_density * self.kerbin.surface_gravity * 700
        self.assertAlmostEqual(
            water,
            self.kerbin.pressure_at(-700) - self.kerbin.pressure_at(0),
            delta=20000,
        )
        self.assertEqual(0, self.space_center.bodies["Mun"].pressure_at(-700))


class TestEditorBuoyancy(krpctest.TestCase):
    """The buoyancy members that work in the editor, where a hull is trimmed before
    it is launched."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Buoyancy")
        cls.space_center = cls.connect().space_center
        cls.kerbin = cls.space_center.bodies["Kerbin"]

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_vessel_displacement(self):
        vessel = self.space_center.editor.vessel
        self.assertGreater(vessel.displacement, 0)
        total = sum(part.displacement for part in vessel.parts.all)
        self.assertAlmostEqual(total, vessel.displacement, delta=1e-6)

    def test_vessel_buoyant_force_at(self):
        vessel = self.space_center.editor.vessel
        density = self.kerbin.ocean_density
        gravity = self.kerbin.surface_gravity
        total = sum(
            part.buoyant_force_at(density, gravity) for part in vessel.parts.all
        )
        force = vessel.buoyant_force_at(density, gravity)
        self.assertGreater(force, 0)
        self.assertAlmostEqual(total, force, delta=force * 1e-6)
        # The hull floats, which is what the readout is consulted for
        self.assertGreater(force, vessel.mass * gravity)

    def test_vessel_center_of_buoyancy(self):
        vessel = self.space_center.editor.vessel
        frame = vessel.parts.root.reference_frame
        center_of_buoyancy = vessel.center_of_buoyancy(frame)
        self.assertEqual(3, len(center_of_buoyancy))
        # It sits on the stack, within its length of the root part
        self.assertLess(norm(vector(center_of_buoyancy)), 100)
        # The fully submerged center of buoyancy is the mean of the per-part points
        # weighted by how much water each part pushes aside
        weighted = vector((0.0, 0.0, 0.0))
        total = 0.0
        for part in vessel.parts.all:
            weight = part.displacement * part.buoyancy_multiplier
            weighted = weighted + vector(part.center_of_buoyancy(frame)) * weight
            total += weight
        self.assertGreater(total, 0)
        self.assertAlmostEqual(
            tuple(x / total for x in weighted), center_of_buoyancy, places=3
        )
        # A distinct point from the center of mass, which is what a hull is trimmed against
        self.assertGreater(
            distance(center_of_buoyancy, vessel.center_of_mass(frame)), 0
        )

    def test_vessel_buoyant_torque_at(self):
        vessel = self.space_center.editor.vessel
        density = self.kerbin.ocean_density
        gravity = self.kerbin.surface_gravity
        # The craft is an unrotated vertical stack, so the root part's y axis is up
        frame = vessel.parts.root.reference_frame
        force = vessel.buoyant_force_at(density, gravity)
        lever = vector(vessel.center_of_buoyancy(frame)) - vector(
            vessel.center_of_mass(frame)
        )
        expected = cross(lever, (0, force, 0))
        self.assertAlmostEqual(
            expected,
            vessel.buoyant_torque_at(density, gravity, frame),
            delta=force * 1e-4,
        )

    def test_part_members(self):
        for part in self.space_center.editor.vessel.parts.all:
            self.assertGreater(part.max_pressure, 0)
            self.assertGreater(part.displacement, 0)
            self.assertGreater(part.buoyancy_multiplier, 0)
            self.assertGreaterEqual(part.water_angular_drag_multiplier, 0)
            frame = part.reference_frame
            self.assertEqual(3, len(part.center_of_buoyancy(frame)))
            self.assertEqual(3, len(part.center_of_displacement(frame)))

    def test_flight_only_part_members_raise(self):
        part = self.space_center.editor.vessel.parts.root
        for name in (
            "splashed",
            "submerged_portion",
            "displaced_volume",
            "depth",
            "min_depth",
            "max_depth",
            "submerged_dynamic_pressure",
            "submerged_drag_multiplier",
            "submerged_lift_multiplier",
            "static_pressure",
        ):
            self.assertRaises(RuntimeError, getattr, part, name)
        self.assertRaises(RuntimeError, part.buoyant_force, part.reference_frame)


class TestEditorBuoyancyMatchesFlight(krpctest.TestCase):
    """The displaced volume is recomputed from the drag cubes so that the editor and
    the flight scene agree on it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center

    def test_displacement_matches_flight(self):
        # PartsParachute carries the parts the two scenes can disagree on: the game
        # names the packed drag cube on a parachute only once the part runs in flight.
        self.enter_editor("VAB", craft="PartsParachute")
        editor_vessel = self.space_center.editor.vessel
        self.assertTrue(
            any("parachute" in part.name for part in editor_vessel.parts.all)
        )
        editor_total = editor_vessel.displacement
        kerbin = self.space_center.bodies["Kerbin"]
        density = kerbin.ocean_density
        gravity = kerbin.surface_gravity
        editor_force = editor_vessel.buoyant_force_at(density, gravity)
        editor_parts = sorted(part.displacement for part in editor_vessel.parts.all)
        self.space_center.editor.launch_vessel("LaunchPad")
        flight_vessel = self.space_center.active_vessel
        flight_parts = sorted(part.displacement for part in flight_vessel.parts.all)
        self.assertEqual(len(editor_parts), len(flight_parts))
        for editor_part, flight_part in zip(editor_parts, flight_parts):
            self.assertAlmostEqual(
                editor_part, flight_part, delta=max(flight_part * 0.01, 1e-6)
            )
        self.assertAlmostEqual(
            editor_total,
            flight_vessel.flight().displacement,
            delta=editor_total * 0.01,
        )
        self.assertAlmostEqual(
            editor_force,
            flight_vessel.flight().buoyant_force_at(density, gravity),
            delta=editor_force * 0.01,
        )


if __name__ == "__main__":
    unittest.main()
