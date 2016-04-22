import unittest
import testingtools
import krpc
import time

class TestPartsLandingLeg(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsLandingLeg')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.LandingLegState
        cls.leg = cls.parts.landing_legs[0]

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_deploy_and_retract(self):
        self.assertEqual(self.state.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.leg.deployed = True
        self.assertEqual(self.state.deploying, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.state.deploying:
            pass
        self.assertEqual(self.state.deployed, self.leg.state)
        self.assertTrue(self.leg.deployed)
        time.sleep(0.1)
        self.leg.deployed = False
        self.assertEqual(self.state.retracting, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.state.retracting:
            pass
        self.assertEqual(self.state.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)

if __name__ == '__main__':
    unittest.main()
