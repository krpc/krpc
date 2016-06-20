import unittest
import krpctest

class TestPartsLandingGear(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsLandingGear':
            cls.launch_vessel_from_vab('PartsLandingGear')
            cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.gear = parts.with_title('LY-99 Extra Large Landing Gear')[0].landing_gear
        cls.fixed_gear = parts.with_title('LY-01 Fixed Landing Gear')[0].landing_gear
        cls.State = cls.connect().space_center.LandingGearState

    def test_deploy_and_retract(self):
        self.assertTrue(self.gear.deployable)
        self.assertEqual(self.State.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)
        self.gear.deployed = False
        self.assertEqual(self.State.retracting, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.State.retracting:
            self.wait()
        self.assertEqual(self.State.retracted, self.gear.state)
        self.assertFalse(self.gear.deployed)
        self.gear.deployed = True
        self.assertEqual(self.State.deploying, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.State.deploying:
            self.wait()
        self.assertEqual(self.State.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)

    def test_fixed_gear(self):
        self.assertFalse(self.fixed_gear.deployable)
        self.assertEqual(self.State.deployed, self.fixed_gear.state)
        self.assertTrue(self.fixed_gear.deployed)

if __name__ == '__main__':
    unittest.main()
