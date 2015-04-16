import unittest
import testingtools
import krpc
import time

class TestPartsSensor(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpc.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestPartsSensor')
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
        self.assertEqual(sensor.value, '0.9809')
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
