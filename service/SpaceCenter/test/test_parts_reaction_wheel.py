import unittest
import krpctest

class TestPartsReactionWheel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.wheel = parts.with_title('Advanced Reaction Wheel Module, Large')[0].reaction_wheel

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_reaction_wheel(self):
        torque = (30000, 30000, 30000)
        self.assertFalse(self.wheel.broken)
        self.assertEqual(torque, self.wheel.max_torque)
        self.assertEqual(torque, self.wheel.available_torque)
        self.assertTrue(self.wheel.active)
        self.wheel.active = False
        self.assertFalse(self.wheel.active)
        self.assertEqual(torque, self.wheel.max_torque)
        self.assertEqual((0, 0, 0), self.wheel.available_torque)
        self.wheel.active = True
        self.assertTrue(self.wheel.active)
        self.assertEqual(torque, self.wheel.max_torque)
        self.assertEqual(torque, self.wheel.available_torque)

if __name__ == '__main__':
    unittest.main()
