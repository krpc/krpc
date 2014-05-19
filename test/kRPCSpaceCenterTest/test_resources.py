import unittest
import testingtools
from testingtools import load_save
import krpc

class TestResources(testingtools.TestCase):

    def test_basic(self):
        load_save('resources')
        ksp = krpc.connect()
        r = ksp.space_center.active_vessel.resources

        self.assertEqual(ksp.space_center.active_vessel.resources, r)

        self.assertEqual(set(['ElectricCharge', 'MonoPropellant', 'LiquidFuel', 'Oxidizer', 'SolidFuel']), set(r.names))

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

        #TODO: remove calls to str(.)

        # Check per-stage amounts (non-cumulative)
        for name in r.names:
            for stage in range(4):
                self.assertClose(expected[stage][name][0], r.amount(str(name), stage=stage, cumulative=False), error=0.5)
                self.assertClose(expected[stage][name][1], r.max(str(name), stage=stage, cumulative=False), error=0.5)

        # Check per-stage amounts (cumulative)
        for name in r.names:
            for stage in range(4):
                expected_amount = sum(expected[x][name][0] for x in range(stage+1))
                expected_max = sum(expected[x][name][1] for x in range(stage+1))
                self.assertClose(expected_amount, r.amount(str(name), stage=stage), error=0.5)
                self.assertClose(expected_max, r.max(str(name), stage=stage), error=0.5)

        # Check total amounts
        for name in r.names:
            expected_amount = sum(expected[stage][name][0] for stage in range(4))
            expected_max = sum(expected[stage][name][1] for stage in range(4))
            self.assertClose(expected_amount, r.amount(str(name)), error=0.5)
            self.assertClose(expected_max, r.max(str(name)), error=0.5)

if __name__ == "__main__":
    unittest.main()
