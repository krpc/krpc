import unittest
import time
import krpctest

class TestPartsControlSurface(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsControlSurface':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsControlSurface')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.ctrlsrf = parts.with_title('FAT-455 Aeroplane Control Surface')[0].control_surface
        cls.winglets = [x.control_surface for x in parts.with_title('Delta-Deluxe Winglet')]
        cls.winglet = cls.winglets[0]

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_get_pyr_enabled(self):
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        for winglet in self.winglets:
            self.assertTrue(winglet.pitch_enabled)
            self.assertFalse(winglet.yaw_enabled)
            self.assertTrue(winglet.roll_enabled)

    def test_toggle_pyr_enabled(self):
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.pitch_enabled = True
        time.sleep(0.1)
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.yaw_enabled = False
        time.sleep(0.1)
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.roll_enabled = True
        time.sleep(0.1)
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertTrue(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.pitch_enabled = False
        self.ctrlsrf.yaw_enabled = True
        self.ctrlsrf.roll_enabled = False
        time.sleep(0.1)
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)

    def test_inverted(self):
        self.assertFalse(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = True
        time.sleep(0.1)
        self.assertTrue(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = False
        time.sleep(0.1)
        self.assertFalse(self.ctrlsrf.inverted)

    def test_deployed(self):
        self.assertFalse(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = True
        time.sleep(0.1)
        self.assertTrue(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = False
        time.sleep(0.1)
        self.assertFalse(self.ctrlsrf.deployed)

    def test_surface_area(self):
        self.assertClose(1, self.ctrlsrf.surface_area)
        self.assertClose(0.2, self.winglet.surface_area)

    def test_available_torque(self):
        self.assertClose((0, 0, 0), self.ctrlsrf.available_torque)
        self.assertClose((0, 0, 0), self.winglet.available_torque)

if __name__ == '__main__':
    unittest.main()
