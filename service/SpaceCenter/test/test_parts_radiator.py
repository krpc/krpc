import unittest
import krpctest

class TestPartsRadiator(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsRadiator')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        cls.control = vessel.control
        cls.state = cls.connect().space_center.RadiatorState
        cls.radiator = vessel.parts.with_title('Thermal Control System (medium)')[0].radiator
        cls.radiator_break = vessel.parts.with_title('Thermal Control System (small)')[0].radiator
        cls.fixed_radiator = vessel.parts.with_title('Radiator Panel (small)')[0].radiator

    def test_fixed_radiator(self):
        self.assertFalse(self.fixed_radiator.deployable)
        self.assertTrue(self.fixed_radiator.deployed)
        self.assertEqual(self.state.extended, self.fixed_radiator.state)

    def test_extendable_radiator(self):
        self.assertTrue(self.radiator.deployable)
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.state.retracted, self.radiator.state)
        self.radiator.deployed = True
        while not self.radiator.deployed:
            self.wait()
        self.assertTrue(self.radiator.deployed)
        self.assertEqual(self.state.extending, self.radiator.state)
        while self.radiator.state == self.state.extending:
            self.wait()
        self.assertTrue(self.radiator.deployed)
        self.assertEqual(self.state.extended, self.radiator.state)
        self.radiator.deployed = False
        while self.radiator.deployed:
            self.wait()
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.state.retracting, self.radiator.state)
        while self.radiator.state == self.state.retracting:
            self.wait()
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.state.retracted, self.radiator.state)

    def test_break_radiator(self):
        self.assertEqual(self.state.retracted, self.radiator_break.state)
        self.radiator_break.deployed = True
        while self.radiator_break.state == self.state.extending:
            self.wait()
        self.control.activate_next_stage()
        while self.radiator_break.state != self.state.broken:
            self.wait()
        self.assertEqual(self.state.broken, self.radiator_break.state)

if __name__ == '__main__':
    unittest.main()
