import unittest
import time
import krpctest

class TestPartsSensor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.parts = cls.conn.space_center.active_vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_barometer(self):
        sensor = self.parts.with_title('PresMat Barometer')[0].sensor
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)
        sensor.active = True
        time.sleep(0.1)
        self.assertTrue(sensor.active)
        self.assertTrue(sensor.value.startswith('99.8'))
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)
        sensor.active = False
        time.sleep(0.1)
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)

    def test_gravity(self):
        sensor = self.parts.with_title('GRAVMAX Negative Gravioli Detector')[0].sensor
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)
        sensor.active = True
        time.sleep(0.1)
        self.assertTrue(sensor.active)
        self.assertEqual('09.81m/s^2', sensor.value)
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)
        sensor.active = False
        time.sleep(0.1)
        self.assertFalse(sensor.active)
        self.assertEqual('Off', sensor.value)
        self.assertClose(0.0075, sensor.power_usage, error=0.0001)

if __name__ == '__main__':
    unittest.main()
