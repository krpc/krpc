import unittest
import krpctest

class TestPartsLandingLeg(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.State = cls.conn.space_center.LandingLegState
        cls.leg = cls.conn.space_center.active_vessel.parts.landing_legs[0]

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_deploy_and_retract(self):
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.leg.deployed = True
        self.assertEqual(self.State.deploying, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.deploying:
            pass
        self.assertEqual(self.State.deployed, self.leg.state)
        self.assertTrue(self.leg.deployed)
        self.leg.deployed = False
        self.assertEqual(self.State.retracting, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.retracting:
            pass
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)

if __name__ == '__main__':
    unittest.main()
