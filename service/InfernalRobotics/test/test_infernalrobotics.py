import unittest
import krpctest


class TestInfernalRobotics(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('InfernalRobotics', directory='./')
        cls.remove_other_vessels()
        cls.ir = cls.connect().infernal_robotics
        cls.vessel = cls.connect().space_center.active_vessel

    def test_servo_groups(self):
        groups = self.ir.servo_groups(self.vessel)
        self.assertItemsEqual(['Group1', 'Group2'], [g.name for g in groups])

    def test_servo_group_with_name(self):
        group1 = self.ir.servo_group_with_name(self.vessel, 'Group1')
        group2 = self.ir.servo_group_with_name(self.vessel, 'Group2')
        group3 = self.ir.servo_group_with_name(self.vessel, 'Group3')
        self.assertEqual('Group1', group1.name)
        self.assertEqual('Group2', group2.name)
        self.assertIsNone(group3)

    def test_servo_with_name(self):
        servo1 = self.ir.servo_with_name(self.vessel, 'Rail')
        servo2 = self.ir.servo_with_name(self.vessel, 'Rotatron')
        servo3 = self.ir.servo_with_name(self.vessel, 'Foo')
        self.assertEqual('Rail', servo1.name)
        self.assertEqual('Rotatron', servo2.name)
        self.assertIsNone(servo3)


if __name__ == '__main__':
    unittest.main()
