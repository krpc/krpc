import unittest
import krpctest


class TestAlarm(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        cls.alarm_manager = cls.space_center.alarm_manager
        for alarm in list(cls.alarm_manager.alarms):
            alarm.remove()

    def setUp(self):
        for alarm in list(self.alarm_manager.alarms):
            alarm.remove()

    def test_remove_alarm(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_remove_alarm", "to be removed")
        self.assertIn(alarm, self.alarm_manager.alarms)
        alarm.remove()
        self.assertNotIn(alarm, self.alarm_manager.alarms)

    def test_access_after_remove_raises(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_access_after_remove", "")
        alarm.remove()
        with self.assertRaises(RuntimeError):
            _ = alarm.title

    def test_remove_twice_raises(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_remove_twice", "")
        alarm.remove()
        with self.assertRaises(RuntimeError):
            alarm.remove()

    def test_alarm_with_name_after_remove(self):
        alarm = self.alarm_manager.add_alarm(
            3600, "test_alarm_with_name_after_remove", ""
        )
        self.assertIsNotNone(
            self.alarm_manager.alarm_with_name("test_alarm_with_name_after_remove")
        )
        alarm.remove()
        self.assertIsNone(
            self.alarm_manager.alarm_with_name("test_alarm_with_name_after_remove")
        )

    def test_alarms_list_stable_after_add_and_remove(self):
        a1 = self.alarm_manager.add_alarm(3600, "regression_a1", "")
        a2 = self.alarm_manager.add_alarm(3600, "regression_a2", "")
        for _ in range(3):
            self.assertEqual(2, len(self.alarm_manager.alarms))
        a1.remove()
        for _ in range(3):
            names = [a.title for a in self.alarm_manager.alarms]
            self.assertIn("regression_a2", names)
        a2.remove()

    def test_trigger_state_initially_false(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_trigger_state", "")
        self.assertFalse(alarm.triggered)
        self.assertFalse(alarm.actioned)


if __name__ == "__main__":
    unittest.main()
