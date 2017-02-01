import unittest
import krpctest


class RCSTestBase(object):

    rcs_data = {
        'Place-Anywhere 7 Linear RCS Port': {
            'propellants': {'MonoPropellant': 1},
            'max_vac_thrust': 2000,
            'msl_isp': 100,
            'vac_isp': 240,
            'thrusters': 1
        },
        'RV-105 RCS Thruster Block': {
            'propellants': {'MonoPropellant': 1},
            'max_vac_thrust': 1000,
            'msl_isp': 100,
            'vac_isp': 240,
            'thrusters': 4
        },
        'Vernor Engine': {
            'propellants': {'LiquidFuel': 9./11., 'Oxidizer': 1},
            'max_vac_thrust': 12000,
            'msl_isp': 140,
            'vac_isp': 260,
            'thrusters': 1
        }
    }

    @classmethod
    def add_rcs_data(cls, title, data):
        for k, v in data.items():
            cls.rcs_data[title][k] = v

    def get_rcs(self, title):
        return self.parts.with_title(title)[0].rcs

    def set_fuel_enabled(self, value):
        for r in self.vessel.resources.all:
            r.enabled = value
        self.wait()


class RCSTest(RCSTestBase):

    def check_properties(self, rcs):
        data = self.rcs_data[rcs.part.title]
        self.control.rcs = True
        self.wait()
        self.assertTrue(rcs.active)
        self.assertTrue(rcs.pitch_enabled)
        self.assertTrue(rcs.yaw_enabled)
        self.assertTrue(rcs.roll_enabled)
        self.assertTrue(rcs.forward_enabled)
        self.assertTrue(rcs.up_enabled)
        self.assertTrue(rcs.right_enabled)
        self.assertAlmostEqual(
            data['pos_torque'], rcs.available_torque[0], delta=10)
        self.assertAlmostEqual(
            data['neg_torque'], rcs.available_torque[1], delta=10)
        self.assertAlmostEqual(data['max_thrust'], rcs.max_thrust, delta=1)
        self.assertEqual(data['max_vac_thrust'], rcs.max_vacuum_thrust)
        self.assertEqual(data['thrusters'], len(rcs.thrusters))
        self.assertAlmostEqual(data['isp'], rcs.specific_impulse, places=1)
        self.assertEqual(
            data['vac_isp'], rcs.vacuum_specific_impulse)
        self.assertEqual(
            data['msl_isp'], rcs.kerbin_sea_level_specific_impulse)
        self.assertItemsEqual(data['propellants'].keys(), rcs.propellants)
        self.assertAlmostEqual(
            data['propellants'], rcs.propellant_ratios, places=3)
        self.assertTrue(rcs.has_fuel)
        self.control.rcs = False
        self.wait()

    def test_rcs_single(self):
        rcs = self.get_rcs('Place-Anywhere 7 Linear RCS Port')
        self.check_properties(rcs)

    def test_rcs_block(self):
        rcs = self.get_rcs('RV-105 RCS Thruster Block')
        self.check_properties(rcs)

    def test_vernor_engine(self):
        rcs = self.get_rcs('Vernor Engine')
        self.check_properties(rcs)


class TestPartsRCS(krpctest.TestCase, RCSTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsRCS':
            cls.launch_vessel_from_vab('PartsRCS')
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts

    def test_active_and_enabled(self):
        rcs = self.get_rcs('RV-105 RCS Thruster Block')
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
        rcs = self.get_rcs('RV-105 RCS Thruster Block')
        props = (
            'enabled', 'pitch_enabled', 'yaw_enabled', 'roll_enabled',
            'forward_enabled', 'up_enabled', 'right_enabled'
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

    def test_has_fuel(self):
        rcs = self.get_rcs('RV-105 RCS Thruster Block')
        self.assertTrue(rcs.has_fuel)

    def test_has_no_fuel(self):
        rcs = self.get_rcs('RV-105 RCS Thruster Block')
        self.set_fuel_enabled(False)
        self.assertFalse(rcs.has_fuel)
        self.set_fuel_enabled(True)


class TestPartsRCSMSL(krpctest.TestCase, RCSTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsRCS')
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts
        cls.add_rcs_data(
            'Place-Anywhere 7 Linear RCS Port',
            {'max_thrust': 842,
             'isp': 101,
             'pos_torque': (1260, 360, 2460),
             'neg_torque': (-1260, -360, -2460)})
        cls.add_rcs_data(
            'RV-105 RCS Thruster Block',
            {'max_thrust': 420,
             'isp': 101,
             'pos_torque': (1020, 470, 805),
             'neg_torque': (-1020, -470, -805)})
        cls.add_rcs_data(
            'Vernor Engine',
            {'max_thrust': 6503,
             'isp': 140.9,
             'pos_torque': (7400, 320, 7570),
             'neg_torque': (-7400, -320, -7570)})


class TestPartsRCSVacuum(krpctest.TestCase, RCSTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsRCS')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 250000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.parts = cls.vessel.parts
        cls.add_rcs_data(
            'Place-Anywhere 7 Linear RCS Port',
            {'max_thrust': 2000,
             'isp': 240,
             'pos_torque': (1210, 325, 2360),
             'neg_torque': (-1210, -325, -2360)})
        cls.add_rcs_data(
            'RV-105 RCS Thruster Block',
            {'max_thrust': 1000,
             'isp': 240,
             'pos_torque': (960, 510, 820),
             'neg_torque': (-960, -510, -820)})
        cls.add_rcs_data(
            'Vernor Engine',
            {'max_thrust': 12000,
             'isp': 260,
             'pos_torque': (6900, 1, 6900),
             'neg_torque': (-6900, -1, -6900)})


if __name__ == '__main__':
    unittest.main()
