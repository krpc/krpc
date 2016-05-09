import unittest
import krpctest
import krpc
import time

class TestPartsSensor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsSensor')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_barometer(self):
        sensor = next(iter(filter(lambda e: e.part.title == 'PresMat Barometer', self.parts.sensors)))
        self.assertFalse(sensor.active)
        self.assertEqual(sensor.value, 'Off')
        self.assertClose(sensor.power_usage, 0)
        sensor.active = True
        time.sleep(0.1)
        self.assertTrue(sensor.active)
        self.assertTrue(sensor.value.startswith('99.8'))
        self.assertClose(sensor.power_usage, 0)
        sensor.active = False
        time.sleep(0.1)
        self.assertFalse(sensor.active)
        self.assertEqual(sensor.value, 'Off')
        self.assertClose(sensor.power_usage, 0)

    def test_gravity(self):
        sensor = next(iter(filter(lambda e: e.part.title == 'GRAVMAX Negative Gravioli Detector', self.parts.sensors)))
        self.assertFalse(sensor.active)
        self.assertEqual(sensor.value, 'Off')
        self.assertClose(sensor.power_usage, 0)
        sensor.active = True
        time.sleep(0.1)
        self.assertTrue(sensor.active)
        self.assertEqual(sensor.value, '09.81m/s^2')
        self.assertClose(sensor.power_usage, 0)
        sensor.active = False
        time.sleep(0.1)
        self.assertFalse(sensor.active)
        self.assertEqual(sensor.value, 'Off')
        self.assertClose(sensor.power_usage, 0)

if __name__ == "__main__":
    unittest.main()
