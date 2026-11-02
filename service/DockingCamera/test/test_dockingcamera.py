import struct
import unittest

import krpctest

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


class TestDockingCamera(krpctest.TestCase):
    mods = ["DockingCamera"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("DockingCamera")
        cls.remove_other_vessels()
        conn = cls.connect()
        cls.dc = conn.docking_camera
        cls.parts = conn.space_center.active_vessel.parts
        cls.part = cls.parts.with_name("OnboardCamera")[0]
        module = next(m for m in cls.part.modules if m.name == "PartCameraModule")
        cls.enabled = next(f for f in module.field_list if f.name == "IsEnabled")

    def setUp(self):
        self.set_enabled(False)

    def set_enabled(self, value):
        self.enabled.bool_value = value
        self.wait_until(lambda: self.enabled.bool_value == value)

    def test_available(self):
        self.assertTrue(self.dc.available)

    def test_camera(self):
        camera = self.dc.camera(self.part)
        self.assertEqual(self.part, camera.part)
        self.assertEqual(camera, self.dc.camera(self.part))

    def test_not_a_camera(self):
        with self.assertRaises(ValueError):
            self.dc.camera(self.parts.root)

    def test_image_while_off(self):
        camera = self.dc.camera(self.part)

        # The mod destroys its cameras in the module's next update
        def is_off():
            try:
                _ = camera.image
            except RuntimeError:
                return True
            return False

        self.wait_until(is_off, message="the camera to switch off")

    def test_image(self):
        camera = self.dc.camera(self.part)
        self.set_enabled(True)

        # The mod creates its cameras in the module's next update
        def try_image():
            try:
                return camera.image
            except RuntimeError:
                return None

        image = self.wait_until(try_image, message="the camera to switch on")
        self.assertEqual(PNG_SIGNATURE, image[:8])
        # The width and height from the IHDR chunk
        self.assertEqual((256, 256), struct.unpack(">II", image[16:24]))


if __name__ == "__main__":
    unittest.main()
