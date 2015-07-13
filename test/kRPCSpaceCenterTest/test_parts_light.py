import unittest
import testingtools
import krpc
import time

class TestPartsLight(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpc.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestPartsLight')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_light(self):
        light = next(iter(filter(lambda e: e.part.title == 'Illuminator Mk1', self.parts.lights)))
        self.assertFalse(light.active)
        self.assertEqual(light.power_usage, 0)
        light.active = True
        time.sleep(1)
        self.assertTrue(light.active)
        self.assertClose(light.power_usage, 0.04)
        light.active = False
        time.sleep(1)
        self.assertFalse(light.active)
        self.assertEqual(light.power_usage, 0)

if __name__ == "__main__":
    unittest.main()
