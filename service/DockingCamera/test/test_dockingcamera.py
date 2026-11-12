import functools
import unittest

from krpc.services.stdlib import ImageFormat
from krpctools.game.images import assert_jpeg, assert_png
from krpctools.game.ksp import TestCase

# The part of each kind of camera on the test craft, and its module
CAMERAS = [
    ("OnboardCamera", "PartCameraModule"),
    ("dockingPort3", "DockingCameraModule"),
]


class TestDockingCamera(TestCase):
    mods = ["DockingCamera"]

    @classmethod
    def setUpClass(cls):
        cls.parts = cls.launch("DockingCamera").parts
        cls.dc = cls.connect().docking_camera
        cls.cameras = []
        for part_name, module_name in CAMERAS:
            part = cls.parts.with_name(part_name)[0]
            module = next(m for m in part.modules if m.name == module_name)
            enabled = next(f for f in module.field_list if f.name == "IsEnabled")
            cls.cameras.append((part, enabled))

    def setUp(self):
        for _, enabled in self.cameras:
            self.set_enabled(enabled, False)

    def set_enabled(self, enabled, value):
        enabled.bool_value = value
        self.wait_until(lambda: enabled.bool_value == value)

    @staticmethod
    def image(camera):
        """The camera's image, or None while the camera is off."""
        try:
            return camera.image()
        except RuntimeError:
            return None

    @classmethod
    def is_off(cls, camera):
        return cls.image(camera) is None

    def test_camera(self):
        for part, _ in self.cameras:
            with self.subTest(part=part.name):
                camera = self.dc.camera(part)
                self.assertEqual(part, camera.part)
                self.assertEqual(camera, self.dc.camera(part))

    def test_not_a_camera(self):
        with self.assertRaises(ValueError):
            self.dc.camera(self.parts.root)

    def test_image_while_off(self):
        for part, _ in self.cameras:
            with self.subTest(part=part.name):
                camera = self.dc.camera(part)
                # The mod destroys its cameras in the module's next update
                self.wait_until(
                    functools.partial(self.is_off, camera),
                    message="the camera to switch off",
                )

    def test_image(self):
        for part, enabled in self.cameras:
            with self.subTest(part=part.name):
                camera = self.dc.camera(part)
                self.set_enabled(enabled, True)
                # The mod creates its cameras in the module's next update
                image = self.wait_until(
                    functools.partial(self.image, camera),
                    message="the camera to switch on",
                )
                assert_png(image, (256, 256))
                assert_jpeg(camera.image(ImageFormat.jpeg))
                with self.assertRaises(ValueError):
                    camera.image(ImageFormat.jpeg, 0)
                # PNG ignores the quality
                assert_png(camera.image(quality=0))
                self.set_enabled(enabled, False)


if __name__ == "__main__":
    unittest.main()
