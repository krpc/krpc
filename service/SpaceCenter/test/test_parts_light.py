import unittest
import time
import krpctest

class TestPartsLight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'Parts':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('Parts')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.light = parts.with_title('Illuminator Mk1')[0].light

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_light(self):
        self.assertFalse(self.light.active)
        self.assertEqual(0, self.light.power_usage)
        self.light.active = True
        while not self.light.active:
            pass
        self.assertTrue(self.light.active)
        time.sleep(0.2)
        self.assertClose(0.04, self.light.power_usage)
        self.light.active = False
        while self.light.active:
            pass
        self.assertFalse(self.light.active)
        self.assertEqual(0, self.light.power_usage)

    def test_color(self):
        self.assertEqual((1, 1, 1), self.light.color)
        self.light.active = True
        while not self.light.active:
            pass
        self.light.color = (1, 0, 0)
        self.assertClose((1, 0, 0), self.light.color)
        time.sleep(0.2)
        self.light.color = (0, 1, 0)
        self.assertClose((0, 1, 0), self.light.color)
        time.sleep(0.2)
        self.light.color = (0, 0, 1)
        self.assertClose((0, 0, 1), self.light.color)
        time.sleep(0.2)
        self.light.color = (1, 1, 1)
        self.assertEqual((1, 1, 1), self.light.color)
        self.light.active = False
        while self.light.active:
            pass

if __name__ == '__main__':
    unittest.main()
