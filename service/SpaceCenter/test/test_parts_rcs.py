import unittest
import krpctest
from krpctest.geometry import cross, distance, dot, norm


class RCSTestBase:

    # Keyed by language-independent internal part name (part.name); the inline
    # comment is the English title (part.title) for readability.
    rcs_data = {
        "linearRcs": {  # Place-Anywhere 7 Linear RCS Port
            "propellants": {"MonoPropellant": 1},
            "max_vac_thrust": 2000,
            "msl_isp": 100,
            "vac_isp": 240,
            "thrusters": 1,
        },
        "RCSBlock.v2": {  # RV-105 RCS Thruster Block
            "propellants": {"MonoPropellant": 1},
            "max_vac_thrust": 1000,
            "msl_isp": 100,
            "vac_isp": 240,
            # The craft uses the default Angled 4-horn variant. The mesh also
            # carries the Orthogonal 5-horn pool, which the variant leaves
            # inactive.
            "thrusters": 4,
        },
        "vernierEngine": {  # Vernor Engine
            "propellants": {"LiquidFuel": 9.0 / 11.0, "Oxidizer": 1},
            "max_vac_thrust": 12000,
            "msl_isp": 140,
            "vac_isp": 260,
            "thrusters": 1,
        },
    }

    @classmethod
    def add_rcs_data(cls, name, data):
        for k, v in data.items():
            cls.rcs_data[name][k] = v

    def get_rcs(self, name):
        return self.parts.with_name(name)[0].rcs

    def set_fuel_enabled(self, value):
        for r in self.vessel.resources.all:
            r.enabled = value
        self.wait()


class RCSTest(RCSTestBase):

    def assert_torque_almost_equal(self, expected, actual):
        # Available torque is a center-of-mass-dependent golden value that
        # drifts slightly between KSP versions and runs. Compare each axis with
        # a 2% relative tolerance plus a small absolute floor, so large kN*m
        # torques (e.g. the Vernor engine, ~4-7 kN*m) are not failed by sub-
        # percent drift while near-zero axes keep the original tight check.
        for axis, (exp, act) in enumerate(zip(expected, actual)):
            self.assertAlmostEqual(
                exp,
                act,
                delta=max(10, abs(exp) * 0.02),
                msg="torque %s not almost equal to %s (axis %d)"
                % (expected, actual, axis),
            )

    def check_properties(self, rcs):
        data = self.rcs_data[rcs.part.name]
        self.control.rcs = True
        self.wait()
        self.assertTrue(rcs.active)
        self.assertTrue(rcs.pitch_enabled)
        self.assertTrue(rcs.yaw_enabled)
        self.assertTrue(rcs.roll_enabled)
        self.assertTrue(rcs.forward_enabled)
        self.assertTrue(rcs.up_enabled)
        self.assertTrue(rcs.right_enabled)
        self.assert_torque_almost_equal(data["pos_torque"], rcs.available_torque[0])
        self.assert_torque_almost_equal(data["neg_torque"], rcs.available_torque[1])

        rcs.thrust_limit = 1
        self.assertAlmostEqual(data["max_thrust"], rcs.available_thrust, delta=1)
        self.assertAlmostEqual(data["max_thrust"], rcs.max_thrust, delta=1)
        self.assertEqual(data["max_vac_thrust"], rcs.max_vacuum_thrust)
        self.assertAlmostEqual(1.0, rcs.thrust_limit)

        rcs.thrust_limit = 0.25
        self.assertAlmostEqual(data["max_thrust"] * 0.25, rcs.available_thrust, delta=1)
        self.assertAlmostEqual(data["max_thrust"], rcs.max_thrust, delta=1)
        self.assertEqual(data["max_vac_thrust"], rcs.max_vacuum_thrust)
        self.assertAlmostEqual(0.25, rcs.thrust_limit)
        self.assert_torque_almost_equal(
            tuple(x * 0.25 for x in data["pos_torque"]), rcs.available_torque[0]
        )
        self.assert_torque_almost_equal(
            tuple(x * 0.25 for x in data["neg_torque"]), rcs.available_torque[1]
        )

        rcs.thrust_limit = 1

        self.assertEqual(data["thrusters"], len(rcs.thrusters))
        self.assertAlmostEqual(data["isp"], rcs.specific_impulse, places=1)
        self.assertEqual(data["vac_isp"], rcs.vacuum_specific_impulse)
        self.assertEqual(data["msl_isp"], rcs.kerbin_sea_level_specific_impulse)
        self.assertCountEqual(data["propellants"].keys(), rcs.propellants)
        self.assertAlmostEqual(data["propellants"], rcs.propellant_ratios, places=3)
        self.assertTrue(rcs.has_fuel)
        self.control.rcs = False
        self.wait()

    def test_rcs_single(self):
        rcs = self.get_rcs("linearRcs")
        self.check_properties(rcs)

    def test_rcs_block(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.check_properties(rcs)

    def test_vernor_engine(self):
        rcs = self.get_rcs("vernierEngine")
        self.check_properties(rcs)


class TestPartsRCS(krpctest.TestCase, RCSTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "PartsRCS":
            cls.launch_vessel_from_vab("PartsRCS")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts

    def test_active_and_enabled(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        rcs.enabled = True
        self.wait()
        self.assertTrue(self.control.rcs)
        self.assertTrue(rcs.enabled)
        self.assertFalse(rcs.part.shielded)
        self.assertTrue(rcs.active)
        rcs.enabled = False
        self.wait()
        self.assertFalse(rcs.enabled)
        self.assertFalse(rcs.active)
        rcs.enabled = True
        self.wait()
        self.assertTrue(rcs.enabled)
        self.assertTrue(rcs.active)
        self.control.rcs = False
        self.wait()
        self.assertFalse(rcs.active)

    def test_enabled_properties(self):
        rcs = self.get_rcs("RCSBlock.v2")
        props = (
            "enabled",
            "pitch_enabled",
            "yaw_enabled",
            "roll_enabled",
            "forward_enabled",
            "up_enabled",
            "right_enabled",
        )
        for prop in props:
            for prop2 in props:
                self.assertTrue(getattr(rcs, prop2))
            setattr(rcs, prop, False)
            self.wait()
            for prop2 in props:
                if prop2 == prop:
                    self.assertFalse(getattr(rcs, prop2))
                else:
                    self.assertTrue(getattr(rcs, prop2))
            setattr(rcs, prop, True)
            self.wait()
            for prop2 in props:
                self.assertTrue(getattr(rcs, prop2))

    def test_input_override(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.assertFalse(rcs.input_override)
        # The getters return zero when the override is not active
        self.assertAlmostEqual((0, 0, 0), rcs.rotation_override)
        self.assertAlmostEqual((0, 0, 0), rcs.translation_override)

        rcs.input_override = True
        self.wait()
        self.assertTrue(rcs.input_override)

        # The rotation and translation demands round-trip
        for rotation in ((1, 0, 0), (0, -0.5, 0.5), (0, 0, 0)):
            rcs.rotation_override = rotation
            self.assertAlmostEqual(rotation, rcs.rotation_override)
        for translation in ((0, 1, 0), (-0.5, 0, 0.25), (0, 0, 0)):
            rcs.translation_override = translation
            self.assertAlmostEqual(translation, rcs.translation_override)
        # Commands are clamped to [-1, 1]
        rcs.rotation_override = (2, -2, 0)
        self.assertAlmostEqual((1, -1, 0), rcs.rotation_override)

        # Disabling releases the override
        rcs.input_override = False
        self.wait()
        self.assertFalse(rcs.input_override)
        self.assertAlmostEqual((0, 0, 0), rcs.rotation_override)
        self.assertAlmostEqual((0, 0, 0), rcs.translation_override)

    def test_input_override_released_on_disconnect(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.assertFalse(rcs.input_override)

        conn = self.connect(use_cached=False)
        parts = conn.space_center.active_vessel.parts
        other = parts.with_name("RCSBlock.v2")[0].rcs
        other.input_override = True
        other.rotation_override = (1, 0, 0)
        self.wait()
        self.assertTrue(rcs.input_override)
        conn.close()

        self.wait()
        self.assertFalse(rcs.input_override)
        self.assertAlmostEqual((0, 0, 0), rcs.rotation_override)

    def override(self, rcs, rotation=(0, 0, 0), translation=(0, 0, 0)):
        """Drive a block with the given demands, and wait for the game to
        allocate thrust to its nozzles."""
        self.control.rcs = True
        rcs.input_override = True
        rcs.rotation_override = rotation
        rcs.translation_override = translation
        self.wait(0.5)

    def check_override_matches_control(self, rcs, name, demand, rotation):
        """Drive an axis with the cooked control, then with the equivalent
        override demand, and compare what the block applies."""
        setattr(self.control, name, 1)
        self.wait(0.5)
        cooked = rcs.torque if rotation else rcs.force
        setattr(self.control, name, 0)
        self.wait(0.5)
        if rotation:
            self.override(rcs, rotation=demand)
        else:
            self.override(rcs, translation=demand)
        self.assertGreater(norm(cooked), 0)
        self.assertAlmostEqual(cooked, rcs.torque if rotation else rcs.force, delta=2)
        rcs.input_override = False
        self.wait(0.5)

    def test_translation_override_axes(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        try:
            for name, demand in (
                ("right", (1, 0, 0)),
                ("up", (0, 1, 0)),
                ("forward", (0, 0, 1)),
            ):
                self.check_override_matches_control(rcs, name, demand, False)
        finally:
            for name in ("right", "up", "forward"):
                setattr(self.control, name, 0)
            rcs.input_override = False

    def test_rotation_override_axes(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        try:
            for name, demand in (
                ("pitch", (1, 0, 0)),
                ("roll", (0, 1, 0)),
                ("yaw", (0, 0, 1)),
            ):
                self.check_override_matches_control(rcs, name, demand, True)
        finally:
            for name in ("pitch", "roll", "yaw"):
                setattr(self.control, name, 0)
            rcs.input_override = False

    def test_force_and_torque(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        self.wait()
        self.assertAlmostEqual((0, 0, 0), rcs.force)
        self.assertAlmostEqual((0, 0, 0), rcs.torque)
        try:
            self.override(rcs, translation=(0, 0, 1))
            self.assertGreater(norm(rcs.force), 0)
        finally:
            rcs.input_override = False
        self.wait(0.5)
        self.assertAlmostEqual((0, 0, 0), rcs.force)
        self.assertAlmostEqual((0, 0, 0), rcs.torque)

    def test_force_and_torque_match_the_thrusters(self):
        rcs = self.get_rcs("RCSBlock.v2")
        frame = self.vessel.reference_frame
        try:
            self.override(rcs, rotation=(1, 0, 0), translation=(0, 0, 1))
            force = (0.0, 0.0, 0.0)
            torque = (0.0, 0.0, 0.0)
            for thruster in rcs.thrusters:
                thrust = thruster.thrust
                nozzle = tuple(x * thrust for x in thruster.thrust_direction(frame))
                position = thruster.thrust_position(frame)
                force = tuple(a + b for a, b in zip(force, nozzle))
                torque = tuple(a + b for a, b in zip(torque, cross(position, nozzle)))
            self.assertGreater(norm(force), 0)
            self.assertAlmostEqual(force, rcs.force, delta=1)
            self.assertAlmostEqual(torque, rcs.torque, delta=1)
        finally:
            rcs.input_override = False

    def test_override_force_and_torque_predict_what_is_applied(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        self.wait()
        demands = (
            ((0, 0, 0), (0, 0, 1)),
            ((1, 0, 0), (0, 0, 0)),
            ((0, 0.5, 0), (0.5, 0, 0)),
        )
        try:
            for rotation, translation in demands:
                force = rcs.override_force(rotation, translation)
                torque = rcs.override_torque(rotation, translation)
                self.override(rcs, rotation=rotation, translation=translation)
                self.assertAlmostEqual(force, rcs.force, delta=1)
                self.assertAlmostEqual(torque, rcs.torque, delta=1)
        finally:
            rcs.input_override = False

    def test_override_force_lies_within_the_available_force(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        self.wait()
        positive, negative = rcs.available_force
        for axis in range(3):
            for sign in (1, -1):
                translation = tuple(sign if i == axis else 0 for i in range(3))
                force = rcs.override_force((0, 0, 0), translation)
                for i in range(3):
                    self.assertLessEqual(force[i], positive[i] + 1)
                    self.assertGreaterEqual(force[i], negative[i] - 1)

    def lever_distance(self, thruster, frame):
        """The perpendicular distance from the center of mass to a nozzle's thrust
        line, which precision mode divides the nozzle's thrust by."""
        position = thruster.thrust_position(frame)
        direction = thruster.thrust_direction(frame)
        along = dot(position, direction)
        return norm(tuple(p - along * d for p, d in zip(position, direction)))

    def firing_translation(self, rcs):
        """A translation demand that fires the part. The cooked control axes reach the
        module through the same mapping, so the demand doubles as a control setting."""
        for demand in (
            (1, 0, 0),
            (-1, 0, 0),
            (0, 1, 0),
            (0, -1, 0),
            (0, 0, 1),
            (0, 0, -1),
        ):
            if norm(rcs.override_force((0, 0, 0), demand)) > 0:
                return demand
        raise AssertionError("no translation demand fires the part")

    def set_translation(self, demand):
        self.control.right, self.control.up, self.control.forward = demand

    def test_precision_mode_weakens_the_cooked_control(self):
        rcs = self.get_rcs("linearRcs")
        self.control.rcs = True
        self.wait()
        lever = self.lever_distance(rcs.thrusters[0], self.vessel.reference_frame)
        # Below one meter the game does not divide, and the test is vacuous
        self.assertGreater(lever, 1)
        demand = self.firing_translation(rcs)
        try:
            self.set_translation(demand)
            self.wait(0.5)
            full = rcs.force
            self.assertGreater(norm(full), 0)
            self.control.precision_mode = True
            self.wait(0.5)
            self.assertAlmostEqual(tuple(x / lever for x in full), rcs.force, delta=2)
        finally:
            self.control.precision_mode = False
            self.set_translation((0, 0, 0))
            self.wait()

    def test_precision_mode_leaves_the_override_alone(self):
        rcs = self.get_rcs("linearRcs")
        self.control.rcs = True
        self.wait()
        demand = self.firing_translation(rcs)
        try:
            self.override(rcs, translation=demand)
            full = rcs.force
            self.assertGreater(norm(full), 0)
            rcs.input_override = False
            self.wait(0.5)
            # Install the override while precision mode is on
            self.control.precision_mode = True
            self.override(rcs, translation=demand)
            self.assertAlmostEqual(full, rcs.force, delta=2)
            self.assertAlmostEqual(
                rcs.override_force((0, 0, 0), demand), rcs.force, delta=2
            )
        finally:
            self.control.precision_mode = False
            rcs.input_override = False
            self.wait()

    def test_releasing_the_override_restores_precision_mode(self):
        rcs = self.get_rcs("linearRcs")
        self.control.rcs = True
        self.wait()
        lever = self.lever_distance(rcs.thrusters[0], self.vessel.reference_frame)
        self.assertGreater(lever, 1)
        demand = self.firing_translation(rcs)
        try:
            self.override(rcs, translation=demand)
            full = rcs.force
            self.assertGreater(norm(full), 0)
            self.control.precision_mode = True
            self.wait(0.5)
            rcs.input_override = False
            self.wait(0.5)
            # The same demand through the cooked control is weakened again
            self.set_translation(demand)
            self.wait(0.5)
            self.assertAlmostEqual(tuple(x / lever for x in full), rcs.force, delta=2)
        finally:
            self.control.precision_mode = False
            self.set_translation((0, 0, 0))
            rcs.input_override = False
            self.wait()

    def test_thruster_thrust(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.control.rcs = True
        self.wait()
        for thruster in rcs.thrusters:
            self.assertEqual(0, thruster.thrust)
        try:
            self.override(rcs, translation=(0, 0, 1))
            thrusts = [thruster.thrust for thruster in rcs.thrusters]
            self.assertGreater(max(thrusts), 0)
            for thrust in thrusts:
                self.assertLessEqual(thrust, rcs.max_thrust + 1)
        finally:
            rcs.input_override = False
        self.wait(0.5)
        for thruster in rcs.thrusters:
            self.assertEqual(0, thruster.thrust)

    def test_thrusters_match_the_part_variant(self):
        """The RV-105 mesh carries the nozzles of every variant, and the game
        fires only the ones the chosen variant leaves active."""
        rcs = self.get_rcs("RCSBlock.v2")
        frame = self.vessel.reference_frame
        thrusters = rcs.thrusters
        self.assertEqual(4, len(thrusters))
        # The Angled 4-horn variant is balanced, so its directions cancel. The
        # Orthogonal pool adds a nozzle along the mount axis, which breaks that.
        total = (0.0, 0.0, 0.0)
        for thruster in thrusters:
            total = tuple(
                t + d for t, d in zip(total, thruster.thrust_direction(frame))
            )
        self.assertAlmostEqual((0, 0, 0), total, places=3)

    def test_has_fuel(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.assertTrue(rcs.has_fuel)

    def test_has_no_fuel(self):
        rcs = self.get_rcs("RCSBlock.v2")
        self.set_fuel_enabled(False)
        self.assertFalse(rcs.has_fuel)
        self.set_fuel_enabled(True)


class TestPartsRCSMSL(krpctest.TestCase, RCSTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsRCS")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts
        cls.add_rcs_data(
            "linearRcs",
            {
                "max_thrust": 842,
                "isp": 101,
                "pos_torque": (0, 153, 0),
                "neg_torque": (-534, 0, -1040),
            },
        )
        cls.add_rcs_data(
            "RCSBlock.v2",
            {
                "max_thrust": 420,
                "isp": 101,
                # The four nozzles are balanced, so each axis is symmetric
                "pos_torque": (446, 232, 379),
                "neg_torque": (-447, -232, -379),
            },
        )
        cls.add_rcs_data(
            "vernierEngine",
            {
                "max_thrust": 6503,
                "isp": 140.9,
                "pos_torque": (4032, 0, 0),
                "neg_torque": (0, -178, -4129),
            },
        )


class TestPartsRCSVacuum(krpctest.TestCase, RCSTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsRCS")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 250000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts
        cls.add_rcs_data(
            "linearRcs",
            {
                "max_thrust": 2000,
                "isp": 240,
                "pos_torque": (0, 326, 0),
                "neg_torque": (-1212, 0, -2367),
            },
        )
        cls.add_rcs_data(
            "RCSBlock.v2",
            {
                "max_thrust": 1000,
                "isp": 240,
                "pos_torque": (996, 593, 916),
                "neg_torque": (-997, -594, -916),
            },
        )
        cls.add_rcs_data(
            "vernierEngine",
            {
                "max_thrust": 12000,
                "isp": 260,
                "pos_torque": (6931, 0, 0),
                "neg_torque": (0, 0, -6931),
            },
        )

    def test_input_override_attitude(self):
        """The override actually flies the vessel: a rotation demand spins it and
        a translation demand accelerates it, both burning monopropellant."""
        rcs = self.get_rcs("RCSBlock.v2")
        vessel = self.vessel
        body_frame = vessel.orbit.body.non_rotating_reference_frame

        def angular_speed():
            return norm(vessel.angular_velocity(body_frame))

        vessel.control.sas = False
        vessel.control.rcs = True
        rcs.enabled = True

        # A rotation demand spins the vessel up and consumes monopropellant.
        self.set_pitch_heading_roll(0, 90, 0)  # start still
        mono_before = vessel.resources.amount("MonoPropellant")
        rcs.input_override = True
        rcs.rotation_override = (0, 0, 1)  # yaw
        self.wait_until(
            lambda: angular_speed() > 0.1,
            timeout=20,
            message="the RCS override to rotate the vessel",
        )
        self.assertLess(vessel.resources.amount("MonoPropellant"), mono_before)
        rcs.rotation_override = (0, 0, 0)
        rcs.input_override = False

        # A translation demand changes the orbital velocity.
        self.set_pitch_heading_roll(0, 90, 0)  # start still
        velocity_before = vessel.velocity(body_frame)
        rcs.input_override = True
        rcs.translation_override = (0, 0, 1)  # forward
        self.wait_until(
            lambda: distance(vessel.velocity(body_frame), velocity_before) > 1,
            timeout=20,
            message="the RCS override to accelerate the vessel",
        )
        rcs.translation_override = (0, 0, 0)
        rcs.input_override = False
        self.set_pitch_heading_roll(0, 90, 0)  # leave the vessel at rest


if __name__ == "__main__":
    unittest.main()
