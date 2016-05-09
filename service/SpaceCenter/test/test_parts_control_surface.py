import unittest
import krpctest
import krpc
import time

class TestPartsControlSurface(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsControlSurface':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsControlSurface')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsControlSurface')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_get_pyr_enabled(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertFalse(ctrlsrf.pitch_enabled)
        self.assertTrue(ctrlsrf.yaw_enabled)
        self.assertFalse(ctrlsrf.roll_enabled)
        wings = filter(lambda e: e.part.title == 'Delta-Deluxe Winglet', self.parts.control_surfaces)
        for wing in wings:
            self.assertTrue(wing.pitch_enabled)
            self.assertFalse(wing.yaw_enabled)
            self.assertTrue(wing.roll_enabled)

    def test_toggle_pyr_enabled(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertFalse(ctrlsrf.pitch_enabled)
        self.assertTrue(ctrlsrf.yaw_enabled)
        self.assertFalse(ctrlsrf.roll_enabled)
        ctrlsrf.pitch_enabled = True
        time.sleep(0.1)
        self.assertTrue(ctrlsrf.pitch_enabled)
        self.assertTrue(ctrlsrf.yaw_enabled)
        self.assertFalse(ctrlsrf.roll_enabled)
        ctrlsrf.yaw_enabled = False
        time.sleep(0.1)
        self.assertTrue(ctrlsrf.pitch_enabled)
        self.assertFalse(ctrlsrf.yaw_enabled)
        self.assertFalse(ctrlsrf.roll_enabled)
        ctrlsrf.roll_enabled = True
        time.sleep(0.1)
        self.assertTrue(ctrlsrf.pitch_enabled)
        self.assertFalse(ctrlsrf.yaw_enabled)
        self.assertTrue(ctrlsrf.roll_enabled)
        ctrlsrf.pitch_enabled = False
        ctrlsrf.yaw_enabled = True
        ctrlsrf.roll_enabled = False
        time.sleep(0.1)
        self.assertFalse(ctrlsrf.pitch_enabled)
        self.assertTrue(ctrlsrf.yaw_enabled)
        self.assertFalse(ctrlsrf.roll_enabled)

    def test_inverted(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertFalse(ctrlsrf.inverted)
        ctrlsrf.inverted = True
        time.sleep(0.1)
        self.assertTrue(ctrlsrf.inverted)
        ctrlsrf.inverted = False
        time.sleep(0.1)
        self.assertFalse(ctrlsrf.inverted)

    def test_deployed(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertFalse(ctrlsrf.deployed)
        ctrlsrf.deployed = True
        time.sleep(0.1)
        self.assertTrue(ctrlsrf.deployed)
        ctrlsrf.deployed = False
        time.sleep(0.1)
        self.assertFalse(ctrlsrf.deployed)

    def test_surface_area(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertClose(1, ctrlsrf.surface_area)
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'Delta-Deluxe Winglet', self.parts.control_surfaces)))
        self.assertClose(0.2, ctrlsrf.surface_area)

    def test_available_torque(self):
        ctrlsrf = next(iter(filter(lambda e: e.part.title == 'FAT-455 Aeroplane Control Surface', self.parts.control_surfaces)))
        self.assertClose((0,0,0), ctrlsrf.available_torque)

if __name__ == "__main__":
    unittest.main()
