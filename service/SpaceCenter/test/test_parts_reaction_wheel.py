import unittest
import krpctest

class TestPartsReactionWheel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.wheel = parts.with_title('Advanced Reaction Wheel Module, Large')[0].reaction_wheel

    def test_reaction_wheel(self):
        pos_torque = (30000, 30000, 30000)
        neg_torque = tuple(-x for x in pos_torque)
        self.assertFalse(self.wheel.broken)
        self.assertEqual(pos_torque, self.wheel.max_torque[0])
        self.assertEqual(pos_torque, self.wheel.available_torque[0])
        self.assertEqual(neg_torque, self.wheel.max_torque[1])
        self.assertEqual(neg_torque, self.wheel.available_torque[1])
        self.assertTrue(self.wheel.active)
        self.wheel.active = False
        self.assertFalse(self.wheel.active)
        self.assertEqual(pos_torque, self.wheel.max_torque[0])
        self.assertEqual(neg_torque, self.wheel.max_torque[1])
        self.assertEqual((0, 0, 0), self.wheel.available_torque[0])
        self.assertEqual((0, 0, 0), self.wheel.available_torque[1])
        self.wheel.active = True
        self.assertTrue(self.wheel.active)
        self.assertEqual(pos_torque, self.wheel.max_torque[0])
        self.assertEqual(pos_torque, self.wheel.available_torque[0])
        self.assertEqual(neg_torque, self.wheel.max_torque[1])
        self.assertEqual(neg_torque, self.wheel.available_torque[1])

if __name__ == '__main__':
    unittest.main()
