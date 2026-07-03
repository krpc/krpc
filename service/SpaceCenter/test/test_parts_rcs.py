import unittest
import krpctest
from krpctest.geometry import distance, norm


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
            # The RV-105 mesh carries the "RCSthruster" transforms for every
            # variant (the default Angled 4-horn plus the Orthogonal 5-horn
            # pool), and kRPC enumerates them all rather than just the active
            # variant's. So it reports 9 thrusters and sums available torque
            # over all 9 (see the per-scenario torque values below).
            "thrusters": 9,
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
                # Summed over all 9 "RCSthruster" transforms in the mesh (see
                # the thrusters note above); the spread of nozzle positions
                # makes the roll term asymmetric. Refreshed against KSP 1.12.5.
                "pos_torque": (892, 460, 751),
                "neg_torque": (-892, -460, -1084),
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
                # Summed over all 9 nozzle transforms; refreshed for KSP 1.12.5
                # (see TestPartsRCSMSL / the thrusters note for details).
                "pos_torque": (1989, 1176, 1815),
                "neg_torque": (-1990, -1176, -2533),
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
