import unittest
import krpctest

# TODO: expand the KAC tests

@unittest.skipIf(not krpctest.TestCase.connect().kerbal_alarm_clock.available, "KerbalAlarmClock is not installed")
class TestKerbalAlarmClock(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.kac = cls.connect().kerbal_alarm_clock

    def test_alarms(self):
        self.assertItemsEqual([], self.kac.alarms)

    def test_alarm_with_name(self):
        self.assertEqual(None, self.kac.alarm_with_name('foo'))

    def test_alarms_with_type(self):
        self.assertItemsEqual([], self.kac.alarms_with_type(self.kac.AlarmType.raw))

if __name__ == '__main__':
    unittest.main()
