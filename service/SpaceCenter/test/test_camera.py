import unittest
import krpctest


class TestCamera(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit('Kerbin', 1000000)
        cls.camera = cls.connect().space_center.camera
        cls.mode = cls.connect().space_center.CameraMode

    def test_flight_modes(self):
        modes = [
            self.mode.free,
            self.mode.chase,
            self.mode.locked,
            self.mode.orbital,
            self.mode.automatic
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


class CameraTestBase(object):

    def test_distance(self):
        # TODO: not supported in IVA mode
        if self.camera.mode == self.mode.iva:
            return
        self.assertGreater(
            self.camera.default_distance, self.camera.min_distance)
        self.assertLess(
            self.camera.default_distance, self.camera.max_distance)
        for distance in self.distances:
            self.assertGreater(distance, self.camera.min_distance)
            self.assertLess(distance, self.camera.max_distance)
            self.camera.distance = distance
            self.wait(0.5)
            self.assertAlmostEqual(distance, self.camera.distance, places=3)

    def test_heading(self):
        # TODO: not supported in IVA mode
        if self.camera.mode == self.mode.iva:
            return
        self.camera.pitch = 0
        self.camera.distance = self.camera.default_distance
        for heading in self.headings:
            self.camera.heading = heading
            self.wait(0.01)
            self.assertAlmostEqual(heading, self.camera.heading, places=3)

    def test_pitch(self):
        # TODO: not supported in IVA mode
        if self.camera.mode == self.mode.iva:
            return
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
        cls.set_circular_orbit('Kerbin', 1000000)
        space_center = cls.connect().space_center
        cls.camera = space_center.camera
        cls.mode = space_center.CameraMode
        if cls.camera.mode != cls.mode.automatic:
            cls.camera.mode = cls.mode.automatic
        cls.wait(1)
        cls.pitches = range(-90, 90, 5)
        cls.headings = range(0, 360, 5)
        cls.distances = (1, 5, 10, 20)


class TestCameraIVA(krpctest.TestCase, CameraTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        space_center = cls.connect().space_center
        cls.camera = space_center.camera
        cls.mode = space_center.CameraMode
        if cls.camera.mode != cls.mode.iva:
            cls.camera.mode = cls.mode.iva
        cls.wait(1)
        cls.pitches = range(-30, 30, 5)
        cls.headings = range(-60, 60, 5)

    @classmethod
    def tearDownClass(cls):
        cls.camera.mode = cls.mode.automatic
        cls.wait(1)


class TestCameraMap(krpctest.TestCase, CameraTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit('Kerbin', 1000000)
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


if __name__ == '__main__':
    unittest.main()
