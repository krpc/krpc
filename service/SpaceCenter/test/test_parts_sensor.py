import unittest
import krpctest


class TestPartsSensor(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        conn = cls.connect()
        active_vessel = conn.space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
            active_vessel = conn.space_center.active_vessel
        conn.paused = False
        # ModuleEnviroSensor readouts update only after the part/vessel is activated
        # (stock PartModule gating). Stage 0 on Parts is launch clamps only.
        if active_vessel.situation.name == "pre_launch":
            active_vessel.control.activate_next_stage()
            cls.wait()
        cls.parts = conn.space_center.active_vessel.parts

    def test_barometer(self):
        sensor = self.parts.with_name("sensorBarometer")[0].sensor
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)
        sensor.active = True
        self.wait_until(
            lambda: sensor.value != "Off",
            timeout=30,
            message="barometer readout to update",
        )
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
        self.wait_until(
            lambda: sensor.value != "Off",
            timeout=30,
            message="gravimeter readout to update",
        )
        self.assertTrue(sensor.active)
        self.assertEqual("09.81m/s^2", sensor.value)
        sensor.active = False
        self.wait()
        self.assertFalse(sensor.active)
        self.assertEqual("Off", sensor.value)


if __name__ == "__main__":
    unittest.main()
