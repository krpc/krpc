import unittest
import krpctest


class TestPartsSensor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_barometer(self):
        sensor = self.parts.with_name("sensorBarometer")[0].sensor
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        self.assertTrue(sensor.value.startswith("100."))
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)

    def test_gravity(self):
        sensor = self.parts.with_name("sensorGravimeter")[0].sensor
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        self.assertEqual("09.81m/s^2", sensor.value)
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)

    def test_thermometer(self):
        part = self.parts.with_name("sensorThermometer")[0]
        sensor = part.sensor
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        value = sensor.value
        self.assertTrue(value.endswith("K"), value)
        # The readout is the part's own temperature, formatted for display.
        self.assertAlmostEqual(part.temperature, float(value[:-1]), delta=1)
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)

    def test_accelerometer(self):
        part = self.parts.with_name("sensorAccelerometer")[0]
        sensor = part.sensor
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)
        sensor.active = True
        self.wait()
        self.assertTrue(sensor.active)
        value = sensor.value
        self.assertTrue(value.endswith("g"), value)
        # The readout is the vessel's felt g-force, formatted for display.
        flight = self.vessel.flight()
        self.assertAlmostEqual(flight.g_force, float(value[:-1]), delta=0.05)
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)


if __name__ == "__main__":
    unittest.main()
