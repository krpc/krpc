import unittest
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
        self.assertClose(0.04, self.light.power_usage)
        self.light.active = False
        while self.light.active:
            pass
        self.assertFalse(self.light.active)
        self.assertEqual(0, self.light.power_usage)

if __name__ == '__main__':
    unittest.main()
