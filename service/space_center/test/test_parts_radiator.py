import unittest
import krpctest


class TestPartsRadiator(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsRadiator')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.RadiatorState
        cls.radiators = parts.radiators
        cls.radiator = parts.with_title(
            'Thermal Control System (medium)')[0].radiator
        cls.radiator_break = parts.with_title(
            'Thermal Control System (small)')[0].radiator
        cls.fixed_radiator = parts.with_title(
            'Radiator Panel (small)')[0].radiator

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

    def test_control_radiators(self):
        self.assertFalse(self.control.radiators)
        self.control.radiators = True
        for radiator in self.radiators:
            if radiator.deployable:
                while radiator.state == self.state.extending:
                    self.wait()
            self.assertTrue(radiator.deployed)
        self.assertTrue(self.control.radiators)
        self.control.radiators = False
        for radiator in self.radiators:
            if radiator.deployable:
                while radiator.state != self.state.retracted:
                    self.wait()
                self.assertFalse(radiator.deployed)
        self.assertFalse(self.control.radiators)


class TestPartsRadiatorBreak(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('PartsRadiator')
        cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.state = cls.connect().space_center.RadiatorState
        cls.radiator = parts.with_title(
            'Thermal Control System (small)')[0].radiator

    def test_break_radiator(self):
        self.assertEqual(self.state.retracted, self.radiator.state)
        self.radiator.deployed = True
        while self.radiator.state == self.state.extending:
            self.wait()
        self.control.activate_next_stage()
        while self.radiator.state != self.state.broken:
            self.wait()
        self.assertEqual(self.state.broken, self.radiator.state)


if __name__ == '__main__':
    unittest.main()
