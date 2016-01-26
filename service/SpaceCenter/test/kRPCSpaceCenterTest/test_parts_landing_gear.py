import unittest
import testingtools
import krpc
import time

class TestPartsLandingGear(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsLandingGear')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.LandingGearState
        cls.gear = cls.parts.landing_gear[0]

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_deploy_and_retract(self):
        self.assertEqual(self.state.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)
        self.gear.deployed = False
        self.assertEqual(self.state.retracting, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.state.retracting:
            pass
        self.assertEqual(self.state.retracted, self.gear.state)
        self.assertFalse(self.gear.deployed)
        time.sleep(0.1)
        self.gear.deployed = True
        self.assertEqual(self.state.deploying, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.state.deploying:
            pass
        self.assertEqual(self.state.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)

if __name__ == "__main__":
    unittest.main()
