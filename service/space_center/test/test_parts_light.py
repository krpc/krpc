import unittest
import krpctest


class TestPartsLight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'Parts':
            cls.launch_vessel_from_vab('Parts')
            cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.light = parts.with_title('Illuminator Mk1')[0].light

    def test_light(self):
        self.assertFalse(self.light.active)
        self.assertEqual(0, self.light.power_usage)
        self.light.active = True
        while not self.light.active:
            self.wait()
        self.assertTrue(self.light.active)
        self.wait(0.2)
        self.assertAlmostEqual(0.04, self.light.power_usage)
        self.light.active = False
        while self.light.active:
            self.wait()
        self.assertFalse(self.light.active)
        self.assertEqual(0, self.light.power_usage)

    def test_color(self):
        self.assertEqual((1, 1, 1), self.light.color)
        self.light.active = True
        while not self.light.active:
            self.wait()
        self.light.color = (1, 0, 0)
        self.assertAlmostEqual((1, 0, 0), self.light.color)
        self.wait(0.2)
        self.light.color = (0, 1, 0)
        self.assertAlmostEqual((0, 1, 0), self.light.color)
        self.wait(0.2)
        self.light.color = (0, 0, 1)
        self.assertAlmostEqual((0, 0, 1), self.light.color)
        self.wait(0.2)
        self.light.color = (1, 1, 1)
        self.assertEqual((1, 1, 1), self.light.color)
        self.light.active = False
        while self.light.active:
            self.wait()


if __name__ == '__main__':
    unittest.main()
