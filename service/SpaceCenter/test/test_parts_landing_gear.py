import unittest
import krpctest
import krpc
import time

class TestPartsLandingGear(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsLandingGear':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsLandingGear')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsLandingGear')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.LandingGearState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_deploy_and_retract(self):
        gear = next(iter(filter(lambda e: e.part.title == 'LY-99 Extra Large Landing Gear', self.parts.landing_gear)))
        self.assertTrue(gear.deployable)
        self.assertEqual(self.state.deployed, gear.state)
        self.assertTrue(gear.deployed)
        gear.deployed = False
        self.assertEqual(self.state.retracting, gear.state)
        self.assertFalse(gear.deployed)
        while gear.state == self.state.retracting:
            pass
        self.assertEqual(self.state.retracted, gear.state)
        self.assertFalse(gear.deployed)
        time.sleep(0.1)
        gear.deployed = True
        self.assertEqual(self.state.deploying, gear.state)
        self.assertFalse(gear.deployed)
        while gear.state == self.state.deploying:
            pass
        self.assertEqual(self.state.deployed, gear.state)
        self.assertTrue(gear.deployed)

    def test_fixed_gear(self):
        gear = next(iter(filter(lambda e: e.part.title == 'LY-01 Fixed Landing Gear', self.parts.landing_gear)))
        self.assertFalse(gear.deployable)
        self.assertEqual(self.state.deployed, gear.state)
        self.assertTrue(gear.deployed)

if __name__ == "__main__":
    unittest.main()
