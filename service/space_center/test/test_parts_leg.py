import unittest
import krpctest


class TestPartsLeg(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsLegs':
            cls.launch_vessel_from_vab('PartsLegs')
            cls.remove_other_vessels()
        cls.State = cls.connect().space_center.LegState
        vessel = cls.connect().space_center.active_vessel
        cls.legs = vessel.parts.legs
        cls.leg = vessel.parts.with_title('LT-05 Micro Landing Strut')[0].leg
        cls.control = vessel.control

    def test_deploy_and_retract(self):
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.assertTrue(self.leg.deployable)
        self.assertFalse(self.control.legs)
        self.leg.deployed = True
        self.wait()
        self.assertEqual(self.State.deploying, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.deploying:
            self.wait()
        self.assertEqual(self.State.deployed, self.leg.state)
        self.assertTrue(self.leg.deployed)
        self.assertFalse(self.control.legs)
        self.leg.deployed = False
        self.wait()
        self.assertEqual(self.State.retracting, self.leg.state)
        self.assertFalse(self.leg.deployed)
        while self.leg.state == self.State.retracting:
            self.wait()
        self.assertEqual(self.State.retracted, self.leg.state)
        self.assertFalse(self.leg.deployed)
        self.assertFalse(self.leg.is_grounded)
        self.assertFalse(self.control.legs)

    def test_control_deploy(self):
        self.assertFalse(self.control.legs)
        self.control.legs = True
        while not self.control.legs:
            self.wait()
        self.assertTrue(self.control.legs)
        for leg in self.legs:
            self.assertTrue(leg.deployed)
        self.control.legs = False
        while self.control.legs:
            self.wait()
        self.assertFalse(self.control.legs)
        for leg in self.legs:
            while leg.state == self.State.retracting:
                self.wait()


if __name__ == '__main__':
    unittest.main()
