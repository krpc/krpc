import unittest
import testingtools
from testingtools import load_save
import krpc

class TestResources(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('resources')
        cls.conn = krpc.connect()
        cls.r = cls.conn.space_center.active_vessel.resources

    density = {
        'MonoPropellant': 4,
        'LiquidFuel':     5,
        'Oxidizer':       5,
        'SolidFuel':      7.5
    }

    expected = {
        0: {
            'ElectricCharge': (150, 150),
            'MonoPropellant': (15, 30),
            'LiquidFuel':     (0, 0),
            'Oxidizer':       (0, 0),
            'SolidFuel':      (0, 0)
        },
        1: {
            'ElectricCharge': (0, 0),
            'MonoPropellant': (0, 0),
            'LiquidFuel':     (720, 1440),
            'Oxidizer':       (1000, 1760),
            'SolidFuel':      (0, 0)
        },
        2: {
            'ElectricCharge': (0, 0),
            'MonoPropellant': (0, 0),
            'LiquidFuel':     (720+720+300, 1440+1440+720),
            'Oxidizer':       (1000+1000+400, 1760+1760+880),
            'SolidFuel':      (13, 15)
        },
        3: {
            'ElectricCharge': (0, 0),
            'MonoPropellant': (0, 0),
            'LiquidFuel':     (0, 0),
            'Oxidizer':       (0, 0),
            'SolidFuel':      (300*4 + 5*8, 850*4 + 8*8)
        },
        4: {
            'ElectricCharge': (0, 0),
            'MonoPropellant': (0, 0),
            'LiquidFuel':     (0, 0),
            'Oxidizer':       (0, 0),
            'SolidFuel':      (0, 0)
        },
    }

    def test_equality(self):
        self.assertEqual(self.conn.space_center.active_vessel.resources, self.r)

    def test_names(self):
        self.assertEqual(set(['ElectricCharge', 'MonoPropellant', 'LiquidFuel', 'Oxidizer', 'SolidFuel']), set(self.r.names))

    #TODO: remove calls to str(.)

    def test_per_stage_amounts(self):
        for name in self.r.names:
            for stage in range(4):
                self.assertClose(self.expected[stage][name][0], self.r.amount(str(name), stage=stage, cumulative=False), error=0.5)
                self.assertClose(self.expected[stage][name][1], self.r.max(str(name), stage=stage, cumulative=False), error=0.5)

    def test_per_stage_amounts_cumulative(self):
        for name in self.r.names:
            for stage in range(4):
                expected_amount = sum(self.expected[x][name][0] for x in range(stage+1))
                expected_max = sum(self.expected[x][name][1] for x in range(stage+1))
                self.assertClose(expected_amount, self.r.amount(str(name), stage=stage), error=0.5)
                self.assertClose(expected_max, self.r.max(str(name), stage=stage), error=0.5)

    def test_total_amounts(self):
        for name in self.r.names:
            expected_amount = sum(self.expected[stage][name][0] for stage in range(4))
            expected_max = sum(self.expected[stage][name][1] for stage in range(4))
            self.assertClose(expected_amount, self.r.amount(str(name)), error=0.5)
            self.assertClose(expected_max, self.r.max(str(name)), error=0.5)

    def test_vessel_mass(self):
        mass = self.conn.space_center.active_vessel.dry_mass
        self.assertEquals(29945, mass)
        for name in self.r.names:
            amount = sum(self.expected[stage][name][0] for stage in range(4))
            if name in self.density:
                mass += amount * self.density[name]
        self.assertEquals(mass, self.conn.space_center.active_vessel.mass)

if __name__ == "__main__":
    unittest.main()
