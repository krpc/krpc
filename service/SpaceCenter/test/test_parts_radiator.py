import unittest
import krpctest

class TestPartsRadiator(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsRadiator')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        vessel = cls.conn.space_center.active_vessel
        cls.control = vessel.control
        cls.State = cls.conn.space_center.RadiatorState
        cls.radiator = vessel.parts.with_title('Thermal Control System (medium)')[0].radiator
        cls.radiator_break = vessel.parts.with_title('Thermal Control System (small)')[0].radiator
        cls.fixed_radiator = vessel.parts.with_title('Radiator Panel (small)')[0].radiator

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_fixed_radiator(self):
        self.assertFalse(self.fixed_radiator.deployable)
        self.assertTrue(self.fixed_radiator.deployed)
        self.assertEqual(self.State.extended, self.fixed_radiator.state)

    def test_extendable_radiator(self):
        self.assertTrue(self.radiator.deployable)
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.State.retracted, self.radiator.state)
        self.radiator.deployed = True
        while not self.radiator.deployed:
            pass
        self.assertTrue(self.radiator.deployed)
        self.assertEqual(self.State.extending, self.radiator.state)
        while self.radiator.state == self.State.extending:
            pass
        self.assertTrue(self.radiator.deployed)
        self.assertEqual(self.State.extended, self.radiator.state)
        self.radiator.deployed = False
        while self.radiator.deployed:
            pass
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.State.retracting, self.radiator.state)
        while self.radiator.state == self.State.retracting:
            pass
        self.assertFalse(self.radiator.deployed)
        self.assertEqual(self.State.retracted, self.radiator.state)

    def test_break_radiator(self):
        self.assertEqual(self.State.retracted, self.radiator.state)
        self.radiator.deployed = True
        while self.radiator.state == self.State.extending:
            pass
        self.control.activate_next_stage()
        while self.radiator.state != self.State.broken:
            pass
        self.assertEqual(self.State.broken, self.radiator.state)

if __name__ == '__main__':
    unittest.main()
