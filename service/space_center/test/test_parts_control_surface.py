import unittest
import krpctest


class TestPartsControlSurface(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if cls.connect().space_center.active_vessel.name \
           != 'PartsControlSurface':
            cls.new_save()
            cls.launch_vessel_from_vab('PartsControlSurface')
            cls.remove_other_vessels()
            # TODO: wait needed to let available torque calculations settle
            cls.wait(3)
        parts = cls.connect().space_center.active_vessel.parts
        cls.ctrlsrf = parts.with_title(
            'FAT-455 Aeroplane Control Surface')[0].control_surface
        cls.winglets = [x.control_surface for x in
                        parts.with_title('Delta-Deluxe Winglet')]
        cls.winglet = cls.winglets[0]

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
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.yaw_enabled = False
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.roll_enabled = True
        self.wait()
        self.assertTrue(self.ctrlsrf.pitch_enabled)
        self.assertFalse(self.ctrlsrf.yaw_enabled)
        self.assertTrue(self.ctrlsrf.roll_enabled)
        self.ctrlsrf.pitch_enabled = False
        self.ctrlsrf.yaw_enabled = True
        self.ctrlsrf.roll_enabled = False
        self.wait()
        self.assertFalse(self.ctrlsrf.pitch_enabled)
        self.assertTrue(self.ctrlsrf.yaw_enabled)
        self.assertFalse(self.ctrlsrf.roll_enabled)

    def test_authority_limiter(self):
        self.assertEqual(100, self.ctrlsrf.authority_limiter)
        self.ctrlsrf.authority_limiter = 50
        self.assertEqual(50, self.ctrlsrf.authority_limiter)
        self.ctrlsrf.authority_limiter = 100
        self.assertEqual(100, self.ctrlsrf.authority_limiter)

    def test_inverted(self):
        self.assertFalse(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = True
        self.wait()
        self.assertTrue(self.ctrlsrf.inverted)
        self.ctrlsrf.inverted = False
        self.wait()
        self.assertFalse(self.ctrlsrf.inverted)

    def test_deployed(self):
        self.assertFalse(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = True
        self.wait()
        self.assertTrue(self.ctrlsrf.deployed)
        self.ctrlsrf.deployed = False
        self.wait()
        self.assertFalse(self.ctrlsrf.deployed)

    def test_surface_area(self):
        self.assertAlmostEqual(1, self.ctrlsrf.surface_area)
        self.assertAlmostEqual(0.2, self.winglet.surface_area)

    def test_available_torque(self):
        self.assertAlmostEqual(
            (0, 0, 0), self.ctrlsrf.available_torque[0], places=3)
        self.assertAlmostEqual(
            (0, 0, 0), self.winglet.available_torque[1], places=3)


if __name__ == '__main__':
    unittest.main()
