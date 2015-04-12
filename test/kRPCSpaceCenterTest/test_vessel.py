import unittest
import testingtools
import krpc
import time

class TestVessel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        cls.conn = krpc.connect(name='TestVessel')
        cls.vtype = cls.conn.space_center.VesselType
        cls.vsituation = cls.conn.space_center.VesselSituation
        cls.vessel = cls.conn.space_center.active_vessel
        cls.far = cls.conn.space_center.far_available

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_name(self):
        self.assertEqual('Basic', self.vessel.name)
        self.vessel.name = 'Foo Bar Baz';
        self.assertEqual('Foo Bar Baz', self.vessel.name)
        self.vessel.name = 'Basic';

    def test_type(self):
        self.assertEqual(self.vtype.ship, self.vessel.type)
        self.vessel.type = self.vtype.station
        self.assertEqual(self.vtype.station, self.vessel.type)
        self.vessel.type = self.vtype.ship

    def test_situation(self):
        self.assertEqual(self.vsituation.orbiting, self.vessel.situation)

    def test_met(self):
        ut = self.conn.space_center.ut
        met = self.vessel.met
        time.sleep(1)
        self.assertClose(ut+1, self.conn.space_center.ut, error=0.5)
        self.assertClose(met+1, self.vessel.met, error=0.5)
        self.assertGreater(self.conn.space_center.ut, self.vessel.met)

    def test_mass(self):
        # 2645 kg dry mass
        # 10 l of monoprop at 4 kg/l
        # 180 l of LiquidFueld at 5 kg/l
        # 220 l of Oxidizer at 5 kg/l
        dry_mass = 2645
        resource_mass = 10 * 4 + 180 * 5 + 220 * 5
        self.assertEqual(dry_mass + resource_mass, self.vessel.mass)

    def test_dry_mass(self):
        # 2645 kg dry mass
        self.assertEqual(2645, self.vessel.dry_mass)

    def test_cross_sectional_area(self):
        if not self.far:
            # Stock aerodynamic model uses: A = 0.008 . m
            self.assertClose(0.008 * self.vessel.mass, self.vessel.cross_sectional_area)
        else:
            self.assertClose(20.722, self.vessel.cross_sectional_area, 0.01)

    def test_thrust(self):
        self.assertClose(self.vessel.thrust, 0)
        #TODO: more thorough testing

    def test_available_thrust(self):
        self.assertClose(self.vessel.available_thrust, 0)
        #TODO: more thorough testing

    def test_max_thrust(self):
        self.assertClose(self.vessel.max_thrust, 0)
        #TODO: more thorough testing

    def test_specific_impulse(self):
        self.assertClose(self.vessel.specific_impulse, 0)
        #TODO: more thorough testing

    def test_vacuum_specific_impulse(self):
        self.assertClose(self.vessel.vacuum_specific_impulse, 0)
        #TODO: more thorough testing

    def test_kerbin_sea_level_specific_impulse(self):
        self.assertClose(self.vessel.kerbin_sea_level_specific_impulse, 0)
        #TODO: more thorough testing

if __name__ == "__main__":
    unittest.main()
