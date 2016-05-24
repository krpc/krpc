import unittest
import krpctest

class TestInfernalRobotics(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('InfernalRobotics')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.ir = cls.conn.infernal_robotics

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_servo_groups(self):
        groups = self.ir.servo_groups
        self.assertEqual(['Group1', 'Group2'], sorted(g.name for g in groups))

    def test_servo_group_with_name(self):
        group1 = self.ir.servo_group_with_name('Group1')
        group2 = self.ir.servo_group_with_name('Group2')
        group3 = self.ir.servo_group_with_name('Group3')
        self.assertEqual('Group1', group1.name)
        self.assertEqual('Group2', group2.name)
        self.assertIsNone(group3)

    def test_servo_with_name(self):
        servo1 = self.ir.servo_with_name('Rail')
        servo2 = self.ir.servo_with_name('Rotatron')
        servo3 = self.ir.servo_with_name('Foo')
        self.assertEqual('Rail', servo1.name)
        self.assertEqual('Rotatron', servo2.name)
        self.assertIsNone(servo3)

if __name__ == '__main__':
    unittest.main()
