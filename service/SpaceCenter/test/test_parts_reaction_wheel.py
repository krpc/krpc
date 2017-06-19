import unittest
import krpctest


class TestPartsReactionWheel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        name = cls.connect().space_center.active_vessel.name
        if name != 'PartsReactionWheel':
            cls.launch_vessel_from_vab('PartsReactionWheel')
            cls.remove_other_vessels()
        vessel = cls.connect().space_center.active_vessel
        parts = vessel.parts
        cls.control = vessel.control
        cls.wheels = parts.wheels
        cls.wheel = parts.with_title(
            'Advanced Reaction Wheel Module, Large')[0].reaction_wheel

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

    def test_control(self):
        self.assertTrue(self.control.reaction_wheels)
        self.control.reaction_wheels = False
        self.wait()
        self.assertFalse(self.control.reaction_wheels)
        for wheel in self.wheels:
            self.assertFalse(wheel.active)
        self.control.reaction_wheels = True
        self.wait()
        self.assertTrue(self.control.reaction_wheels)
        for wheel in self.wheels:
            self.assertTrue(wheel.active)


if __name__ == '__main__':
    unittest.main()
