import unittest
import krpctest


class TestPartsSensor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts

    def test_barometer(self):
        sensor = self.parts.with_title('PresMat Barometer')[0].sensor
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        self.assertTrue(sensor.value.startswith('100.'))
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)

    def test_gravity(self):
        sensor = self.parts.with_title(
            'GRAVMAX Negative Gravioli Detector')[0].sensor
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        self.assertEqual('09.81m/s^2', sensor.value)
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)


if __name__ == '__main__':
    unittest.main()
