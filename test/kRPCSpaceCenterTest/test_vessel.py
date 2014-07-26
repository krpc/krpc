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
        cls.conn = krpc.connect()
        cls.vtype = cls.conn.space_center.VesselType
        cls.vsituation = cls.conn.space_center.VesselSituation
        cls.vessel = cls.conn.space_center.active_vessel

    def test_name(self):
        self.assertEqual('Basic', self.vessel.name)
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
        # 2625 kg dry mass
        # 10 l of monoprop at 4 kg/l
        # 180 l of LiquidFueld at 5 kg/l
        # 220 l of Oxidizer at 5 kg/l
        dry_mass = 2625
        resource_mass = 10 * 4 + 180 * 5 + 220 * 5
        self.assertEqual(dry_mass + resource_mass, self.vessel.mass)

    def test_dry_mass(self):
        # 2625 kg dry mass
        self.assertEqual(2625, self.vessel.dry_mass)

    def test_cross_sectional_area(self):
        # Stock aerodynamic model uses: A = 0.008 . m
        self.assertClose(0.008 * self.vessel.mass, self.vessel.cross_sectional_area)

    def test_drag_coefficient(self):
        # Using stock aerodynamic model
        parts = {
            'mk1pod': {'n': 1, 'mass': 0.8, 'drag': 0.2},
            'fuelTank': {'n': 1, 'mass': 0.125, 'drag': 0.2},
            'batteryPack': {'n': 2, 'mass': 0.01, 'drag': 0.2},
            'solarPanels1': {'n': 3, 'mass': 0.02, 'drag': 0.25},
            'liquidEngine2': {'n': 1, 'mass': 1.5, 'drag': 0.2}
        }
        total_mass = sum(x['mass']*x['n'] for x in parts.values())
        mass_drag_products = sum(x['mass']*x['drag']*x['n'] for x in parts.values())
        drag_coefficient = mass_drag_products / total_mass
        self.assertClose(drag_coefficient, self.vessel.drag_coefficient)

if __name__ == "__main__":
    unittest.main()
