import unittest
import krpctest

class TestPartsLandingGear(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsLandingGear':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsLandingGear')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.gear = parts.with_title('LY-99 Extra Large Landing Gear')[0].landing_gear
        cls.fixed_gear = parts.with_title('LY-01 Fixed Landing Gear')[0].landing_gear
        cls.State = cls.conn.space_center.LandingGearState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_deploy_and_retract(self):
        self.assertTrue(self.gear.deployable)
        self.assertEqual(self.State.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)
        self.gear.deployed = False
        self.assertEqual(self.State.retracting, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.State.retracting:
            pass
        self.assertEqual(self.State.retracted, self.gear.state)
        self.assertFalse(self.gear.deployed)
        self.gear.deployed = True
        self.assertEqual(self.State.deploying, self.gear.state)
        self.assertFalse(self.gear.deployed)
        while self.gear.state == self.State.deploying:
            pass
        self.assertEqual(self.State.deployed, self.gear.state)
        self.assertTrue(self.gear.deployed)

    def test_fixed_gear(self):
        self.assertFalse(self.fixed_gear.deployable)
        self.assertEqual(self.State.deployed, self.fixed_gear.state)
        self.assertTrue(self.fixed_gear.deployed)

if __name__ == '__main__':
    unittest.main()
