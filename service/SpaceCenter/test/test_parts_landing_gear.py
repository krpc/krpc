import unittest
import krpctest


class TestPartsLandingGear(krpctest.TestCase):

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab('PartsLandingGear')
        self.remove_other_vessels()
        self.parts = self.connect().space_center.active_vessel.parts
        self.gear = self.parts.with_title(
            'LY-99 Extra Large Landing Gear')[0].landing_gear
        self.fixed_gear = self.parts.with_title(
            'LY-01 Fixed Landing Gear')[0].landing_gear
        self.state = self.connect().space_center.LandingGearState

    def test_deploy_and_retract(self):
        self.assertTrue(self.gear.deployable)
        self.assertEqual(self.state.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)
        self.gear.deployed = False
        self.wait()
        self.assertEqual(self.state.retracting, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.state.retracting:
            self.wait()
        self.assertEqual(self.state.retracted, self.gear.state)
        self.assertFalse(self.gear.deployed)
        self.gear.deployed = True
        self.wait()
        self.assertEqual(self.state.deploying, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.state.deploying:
            self.wait()
        self.assertEqual(self.state.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)

    def test_fixed_gear(self):
        self.assertFalse(self.fixed_gear.deployable)
        self.assertEqual(self.state.deployed, self.fixed_gear.state)
        self.assertTrue(self.fixed_gear.deployed)

    def test_grounded(self):
        self.assertTrue(self.gear.deployed)
        self.assertFalse(self.gear.is_grounded)
        self.parts.launch_clamps[0].release()
        self.wait(1)
        self.assertTrue(self.gear.is_grounded)


if __name__ == '__main__':
    unittest.main()
