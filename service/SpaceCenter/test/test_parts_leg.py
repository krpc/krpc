import unittest
import krpctest


class TestPartsLeg(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        cls.State = cls.connect().space_center.LegState
        cls.leg = cls.connect().space_center.active_vessel.parts.legs[0]

    def test_deploy_and_retract(self):
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.leg.deployed = True
        self.wait()
        self.assertEqual(self.State.deploying, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.deploying:
            self.wait()
        self.assertEqual(self.State.deployed, self.leg.state)
        self.assertTrue(self.leg.deployed)
        self.leg.deployed = False
        self.wait()
        self.assertEqual(self.State.retracting, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.retracting:
            self.wait()
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.assertFalse(self.leg.is_grounded)


if __name__ == '__main__':
    unittest.main()
