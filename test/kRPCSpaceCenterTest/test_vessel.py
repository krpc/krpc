import unittest
import testingtools
from testingtools import load_save
import krpc
import time

class TestVessel(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('basic')
        cls.conn = krpc.connect()
        cls.vtype = cls.conn.space_center.VesselType
        cls.vsituation = cls.conn.space_center.VesselSituation
        cls.vessel = cls.conn.space_center.active_vessel

    def test_name(self):
        self.assertEqual('Test', self.vessel.name)
        self.vessel.name = 'Foo Bar Baz';
        self.assertEqual('Foo Bar Baz', self.vessel.name)

    def test_type(self):
        self.assertEqual(self.vtype.ship, self.vessel.type)
        self.vessel.type = self.vtype.station
        self.assertEqual(self.vtype.station, self.vessel.type)

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
        # 0.8 t dry mass
        # 10 l of monoprop at 4 kg/l
        self.assertEqual(0.8 * 1000 + 10 * 4, self.vessel.mass)

    def test_dry_mass(self):
        # 0.8 t dry mass
        self.assertEqual(0.8 * 1000, self.vessel.dry_mass)

    def test_cross_sectional_area(self):
        self.assertClose(0.008 * (0.84 * 1000), self.vessel.cross_sectional_area)

    def test_drag_coefficient(self):
        self.assertClose((0.84 * 0.2) / 0.84, self.vessel.drag_coefficient)

if __name__ == "__main__":
    unittest.main()
