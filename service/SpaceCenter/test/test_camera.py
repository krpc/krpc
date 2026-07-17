import unittest

import krpctest


class TestCamera(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit("Kerbin", 1000000)
        cls.camera = cls.connect().space_center.camera
        cls.mode = cls.connect().space_center.CameraMode

    def test_flight_modes(self):
        modes = [
            self.mode.free,
            self.mode.chase,
            self.mode.locked,
            self.mode.orbital,
            self.mode.automatic,
        ]
        self.assertEqual(self.mode.automatic, self.camera.mode)
        for mode in modes:
            self.camera.mode = mode
            self.wait(1)
            self.assertEqual(mode, self.camera.mode)

    def test_iva_mode(self):
        self.assertEqual(self.mode.automatic, self.camera.mode)
        self.camera.mode = self.mode.iva
        self.wait(2)
        self.assertEqual(self.mode.iva, self.camera.mode)
        self.camera.mode = self.mode.automatic
        self.wait(2)
        self.assertEqual(self.mode.automatic, self.camera.mode)

    def test_map_mode(self):
        self.assertEqual(self.mode.automatic, self.camera.mode)
        self.camera.mode = self.mode.map
        self.wait(1)
        self.assertEqual(self.mode.map, self.camera.mode)
        self.camera.mode = self.mode.automatic
        self.wait(1)
        self.assertEqual(self.mode.automatic, self.camera.mode)


class CameraTestBase:
    def test_distance(self):
        self.assertGreater(self.camera.default_distance, self.camera.min_distance)
        self.assertLess(self.camera.default_distance, self.camera.max_distance)
        for distance in self.distances:
            self.assertGreater(distance, self.camera.min_distance)
            self.assertLess(distance, self.camera.max_distance)
            self.camera.distance = distance
            self.wait(0.5)
            self.assertAlmostEqual(distance, self.camera.distance, places=3)

    def test_heading(self):
        self.camera.pitch = 0
        self.camera.distance = self.camera.default_distance
        for heading in self.headings:
            self.camera.heading = heading
            self.wait(0.01)
            self.assertAlmostEqual(heading, self.camera.heading, places=3)

    def test_pitch(self):
        self.camera.heading = 0
        self.camera.distance = self.camera.default_distance
        for pitch in self.pitches:
            self.assertGreater(pitch, self.camera.min_pitch)
            self.assertLess(pitch, self.camera.max_pitch)
            self.camera.pitch = pitch
            self.wait(0.01)
            self.assertAlmostEqual(pitch, self.camera.pitch, places=1)


class TestCameraFlight(krpctest.TestCase, CameraTestBase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit("Kerbin", 1000000)
        space_center = cls.connect().space_center
        cls.camera = space_center.camera
        cls.mode = space_center.CameraMode
        if cls.camera.mode != cls.mode.automatic:
            cls.camera.mode = cls.mode.automatic
        cls.wait(1)
        cls.pitches = range(-90, 90, 5)
        cls.headings = range(0, 360, 5)
        cls.distances = (1, 5, 10, 20)


# TODO: test camera in IVA mode
# class TestCameraIVA(krpctest.TestCase, CameraTestBase):
#     @classmethod
#     def setUpClass(cls):
#         super().setUpClass()
#         cls.camera = cls.space_center.camera
#         cls.mode = cls.space_center.CameraMode
#         cls.camera.mode = cls.mode.iva
#         cls.wait(1)
#         cls.pitches = range(-30, 30, 5)
#         cls.headings = range(-60, 60, 5)
#
#     @classmethod
#     def tearDownClass(cls):
#         cls.camera.mode = cls.mode.automatic
#         cls.wait(1)


class TestCameraMap(krpctest.TestCase, CameraTestBase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit("Kerbin", 1000000)
        space_center = cls.connect().space_center
        cls.camera = space_center.camera
        cls.mode = space_center.CameraMode
        if cls.camera.mode != cls.mode.map:
            cls.camera.mode = cls.mode.map
        cls.wait(1)
        cls.pitches = range(-90, 90, 5)
        cls.headings = range(0, 360, 5)
        cls.distances = (100000, 120000, 200000)

    @classmethod
    def tearDownClass(cls):
        cls.camera.mode = cls.mode.automatic
        cls.wait(1)


class TestCameraDistanceDuringSwitch(krpctest.TestCase):
    # Runs last so its deliberately unsettled camera state does not leak into the
    # other camera test classes (new_save does not reload when already in flight).
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit("Kerbin", 1000000)
        space_center = cls.connect().space_center
        cls.camera = space_center.camera
        cls.mode = space_center.CameraMode

    def test_distance_survives_camera_settle(self):
        # Regression for #318: while the camera is still settling (as it is just
        # after the vessel is placed / the scene loads), switch mode and set the
        # distance with no wait. During the settle the camera drives its distance
        # back to the default, so without deferral the write is lost; it must be
        # re-applied and hold once the camera settles.
        self.set_circular_orbit("Kerbin", 1000000)
        self.camera.mode = self.mode.chase
        self.camera.distance = 15
        self.wait(3)
        self.assertEqual(self.mode.chase, self.camera.mode)
        self.assertAlmostEqual(15, self.camera.distance, places=0)


if __name__ == "__main__":
    unittest.main()
