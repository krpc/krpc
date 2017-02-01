import unittest
import krpctest


class TestServoGroup(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('InfernalRobotics', directory='./')
        cls.remove_other_vessels()
        cls.ir = cls.connect().infernal_robotics
        cls.vessel = cls.connect().space_center.active_vessel

    def test_servo_group(self):
        group1 = self.ir.servo_group_with_name(self.vessel, 'Group1')
        group2 = self.ir.servo_group_with_name(self.vessel, 'Group2')
        self.assertEqual('Group1', group1.name)
        self.assertItemsEqual(
            ['Hinge', 'Rail', 'Rotatron'],
            [x.name for x in group1.servos])
        self.assertItemsEqual(
            ['Adjustable Rail', 'IR Rotatron', 'Powered Hinge'],
            [x.title for x in group1.parts])
        self.assertEqual('Group2', group2.name)
        self.assertItemsEqual(
            ['DockingFree', 'DockingRotatron'],
            [x.name for x in group2.servos])
        self.assertItemsEqual(
            ['Docking Washer Standard',
             'Docking Washer Standard (Free Moving)'],
            [x.title for x in group2.parts])

    def test_servo_with_name(self):
        group = self.ir.servo_group_with_name(self.vessel, 'Group1')
        servo = group.servo_with_name('Rotatron')
        self.assertEqual(servo.name, 'Rotatron')
        self.assertIsNone(group.servo_with_name('Foo'))


if __name__ == '__main__':
    unittest.main()
