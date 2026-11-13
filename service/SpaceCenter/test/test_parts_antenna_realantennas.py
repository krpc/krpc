import math
import unittest

from krpctools.game.geometry import angle_between
from krpctools.game.ksp import TestCase


def menu_field(antenna, name):
    """A field of the RealAntennas part module."""
    module = next(m for m in antenna.part.modules if m.name == "ModuleRealAntenna")
    return next(f for f in module.field_list if f.name == name)


def menu_watts(antenna, name):
    """A power draw from the RealAntennas menu, which shows it as "<watts> Watts"."""
    return float(menu_field(antenna, name).value.split()[0])


class TestPartsAntennaRealAntennas(TestCase):
    mods = ["RealAntennas"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsAntennaRealAntennas")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        vessel = cls.space_center.active_vessel
        assert vessel is not None
        cls.vessel = vessel
        parts = vessel.parts
        cls.dish = parts.with_name("HighGainAntenna")[0].antenna
        cls.deployable_dish = parts.with_name("HighGainAntenna5.v2")[0].antenna
        cls.omni = parts.with_name("longAntenna")[0].antenna
        cls.TargetMode = cls.space_center.AntennaTargetMode
        cls.wait_until(
            lambda: cls.vessel.comms.control_path,
            message="the vessel to connect to a ground station",
        )

    def test_available(self):
        self.assertTrue(self.space_center.real_antennas_available)

    def test_every_antenna_is_a_real_antenna(self):
        # RealAntennas gives the pod an antenna as well
        self.assertEqual(4, len(self.vessel.parts.antennas))
        for antenna in self.vessel.parts.antennas:
            self.assertGreater(antenna.data_rate, 0)

    def test_link_budget(self):
        for antenna in (self.dish, self.omni):
            self.assertAlmostEqual(
                menu_field(antenna, "Gain").float_value, antenna.gain, delta=0.05
            )
            self.assertAlmostEqual(
                menu_field(antenna, "TxPower").float_value,
                antenna.transmit_power,
                delta=0.05,
            )
            self.assertGreaterEqual(antenna.transmit_power, 0)
            self.assertLessEqual(antenna.transmit_power, antenna.max_transmit_power)
            self.assertGreaterEqual(antenna.tech_level, 0)
            self.assertLessEqual(antenna.tech_level, antenna.max_tech_level)
            self.assertNotEqual("", antenna.band)
            self.assertGreater(antenna.frequency, 1e6)
            self.assertGreater(antenna.data_rate, 0)
            self.assertAlmostEqual(
                math.sqrt(52525 / 10 ** (antenna.gain / 10)),
                antenna.beamwidth,
                delta=0.01,
            )
            self.assertAlmostEqual(
                menu_watts(antenna, "sIdlePowerConsumed"),
                antenna.idle_power_draw,
                delta=0.01,
            )
            self.assertAlmostEqual(
                menu_watts(antenna, "sActivePowerConsumed"),
                antenna.active_power_draw,
                delta=0.01,
            )
            self.assertGreater(antenna.active_power_draw, antenna.idle_power_draw)
            self.assertTrue(antenna.enabled)
        self.assertGreater(self.dish.gain, self.omni.gain)
        self.assertLess(self.dish.beamwidth, self.omni.beamwidth)

    def test_stock_members(self):
        self.assertTrue(self.deployable_dish.deployable)
        self.assertFalse(self.deployable_dish.deployed)
        self.assertGreater(self.dish.packet_interval, 0)
        self.assertGreater(self.dish.packet_size, 0)

    def test_settings_are_editor_only(self):
        self.assertRaises(RuntimeError, setattr, self.dish, "transmit_power", 10)
        self.assertRaises(RuntimeError, setattr, self.dish, "tech_level", 1)
        self.assertRaises(RuntimeError, setattr, self.dish, "enabled", False)

    def test_comms(self):
        comms = self.vessel.comms
        self.assertTrue(comms.can_communicate)
        link = comms.control_path[0]
        self.assertEqual(self.vessel, link.start.vessel)
        self.assertTrue(link.end.is_home)
        self.assertGreater(link.forward_data_rate, 0)
        self.assertGreater(link.reverse_data_rate, 0)
        self.assertIn(link.start_antenna, self.vessel.parts.antennas)
        self.assertIsNone(link.end_antenna)

    def test_omni_cannot_target(self):
        self.assertFalse(self.omni.can_target)
        self.assertEqual(self.TargetMode.none, self.omni.target_mode)
        self.assertIsNone(self.omni.target_vessel)
        self.assertIsNone(self.omni.target_body)
        ref = self.vessel.reference_frame
        self.assertRaises(RuntimeError, self.omni.target_direction, ref)
        with self.assertRaises(RuntimeError):
            self.omni.target_vessel = self.vessel
        self.assertRaises(RuntimeError, self.omni.set_target_azimuth_elevation, 0, 0)

    def check_direction(self, expected, antenna, ref):
        self.assertLess(angle_between(expected, antenna.target_direction(ref)), 1)

    def test_default_target(self):
        # RealAntennas points a dish at the center of the home body until it is given a
        # target. Runs before the tests that set one.
        self.assertTrue(self.deployable_dish.can_target)
        self.assertEqual(self.TargetMode.body, self.deployable_dish.target_mode)
        self.assertEqual(
            self.space_center.bodies["Kerbin"], self.deployable_dish.target_body
        )

    def test_target_body(self):
        mun = self.space_center.bodies["Mun"]
        self.dish.target_body = mun
        self.assertEqual(self.TargetMode.body, self.dish.target_mode)
        self.assertEqual(mun, self.dish.target_body)
        self.assertIsNone(self.dish.target_vessel)
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        to_mun = tuple(
            a - b for a, b in zip(mun.position(ref), self.vessel.position(ref))
        )
        self.check_direction(to_mun, self.dish, ref)

    def test_target_position(self):
        kerbin = self.space_center.bodies["Kerbin"]
        self.dish.set_target_position(kerbin, 10, 120, 5000)
        self.assertEqual(self.TargetMode.position, self.dish.target_mode)
        self.assertEqual(kerbin, self.dish.target_body)
        ref = kerbin.reference_frame
        target = kerbin.position_at_altitude(10, 120, 5000, ref)
        to_target = tuple(a - b for a, b in zip(target, self.vessel.position(ref)))
        self.check_direction(to_target, self.dish, ref)

    def test_target_vessel(self):
        self.dish.target_vessel = self.vessel
        self.assertEqual(self.TargetMode.vessel, self.dish.target_mode)
        self.assertEqual(self.vessel, self.dish.target_vessel)
        self.assertIsNone(self.dish.target_body)

    def test_target_azimuth_elevation(self):
        self.dish.set_target_azimuth_elevation(90, 30)
        self.assertEqual(self.TargetMode.azimuth_elevation, self.dish.target_mode)
        self.assertIsNone(self.dish.target_vessel)
        # The surface reference frame has x up, y north and z east
        ref = self.vessel.surface_reference_frame
        direction = self.dish.target_direction(ref)
        self.assertAlmostEqual(math.sin(math.radians(30)), direction[0], delta=0.02)
        self.check_direction(
            (math.sin(math.radians(30)), 0, math.cos(math.radians(30))), self.dish, ref
        )
        self.assertRaises(ValueError, self.dish.set_target_azimuth_elevation, 361, 0)
        self.assertRaises(ValueError, self.dish.set_target_azimuth_elevation, 0, 91)

    def test_target_orbit_relative(self):
        ref = self.vessel.orbit.body.non_rotating_reference_frame

        def angle_to(axis):
            return lambda: angle_between(axis(), self.dish.target_direction(ref))

        def prograde():
            return self.vessel.velocity(ref)

        def up():
            return self.vessel.position(ref)

        def wait_for_angle(angle, axis):
            self.wait_until(
                lambda: abs(angle_to(axis)() - angle) < 1,
                timeout=5,
                message="the antenna to point %d degrees from the axis" % angle,
            )

        self.dish.set_target_orbit_relative(0, 0)
        self.assertEqual(self.TargetMode.orbit_relative, self.dish.target_mode)
        wait_for_angle(0, prograde)
        # A deflection turns the direction in the orbital plane, towards or away from up
        self.dish.set_target_orbit_relative(90, 0)
        wait_for_angle(90, prograde)
        self.assertLess(min(angle_to(up)(), 180 - angle_to(up)()), 1)
        # An elevation turns it out of the orbital plane
        self.dish.set_target_orbit_relative(0, 90)
        wait_for_angle(90, prograde)
        self.assertAlmostEqual(90, angle_to(up)(), delta=1)
        self.assertRaises(ValueError, self.dish.set_target_orbit_relative, 181, 0)

    def test_z_disable_permanently(self):
        # Runs last, as the antenna stays disabled
        self.assertTrue(self.omni.enabled)
        self.omni.disable_permanently()
        self.assertFalse(self.omni.enabled)


class TestEditorAntennaRealAntennas(TestCase):
    mods = ["RealAntennas"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="PartsAntennaRealAntennas")
        parts = cls.connect().space_center.editor.vessel.parts
        cls.dish = parts.with_name("HighGainAntenna")[0].antenna

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_transmit_power(self):
        dish = self.dish
        gain = dish.gain
        dish.transmit_power = 20
        self.assertEqual(20, dish.transmit_power)
        low = dish.active_power_draw
        dish.transmit_power = 30
        self.assertEqual(30, dish.transmit_power)
        self.assertGreater(dish.active_power_draw, low)
        self.assertEqual(gain, dish.gain)
        self.assertAlmostEqual(
            menu_field(dish, "TxPower").float_value, dish.transmit_power, delta=0.05
        )
        self.assertRaises(ValueError, setattr, dish, "transmit_power", -1)
        self.assertRaises(
            ValueError, setattr, dish, "transmit_power", dish.max_transmit_power + 1
        )

    def test_tech_level(self):
        dish = self.dish
        self.assertGreater(dish.max_tech_level, 0)
        dish.tech_level = 0
        self.assertEqual(0, dish.tech_level)
        low = dish.data_rate
        dish.tech_level = dish.max_tech_level
        self.assertEqual(dish.max_tech_level, dish.tech_level)
        self.assertGreater(dish.data_rate, low)
        self.assertRaises(ValueError, setattr, dish, "tech_level", -1)
        self.assertRaises(
            ValueError, setattr, dish, "tech_level", dish.max_tech_level + 1
        )

    def test_enabled(self):
        dish = self.dish
        dish.enabled = False
        self.assertFalse(dish.enabled)
        dish.enabled = True
        self.assertTrue(dish.enabled)


if __name__ == "__main__":
    unittest.main()
