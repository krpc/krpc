import unittest
import time
import krpctest

class TestCamera(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 1000000)
        cls.conn = krpctest.connect(cls)
        cls.camera = cls.conn.space_center.camera
        cls.mode = cls.conn.space_center.CameraMode

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

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
            time.sleep(1)
            self.assertEqual(mode, self.camera.mode)

    def test_iva_mode(self):
        self.assertEqual(self.mode.automatic, self.camera.mode)
        self.camera.mode = self.mode.iva
        time.sleep(1)
        self.assertEqual(self.mode.iva, self.camera.mode)
        self.camera.mode = self.mode.automatic
        time.sleep(1)
        self.assertEqual(self.mode.automatic, self.camera.mode)

    def test_map_mode(self):
        self.assertEqual(self.mode.automatic, self.camera.mode)
        self.camera.mode = self.mode.map
        time.sleep(1)
        self.assertEqual(self.mode.map, self.camera.mode)
        self.camera.mode = self.mode.automatic
        time.sleep(1)
        self.assertEqual(self.mode.automatic, self.camera.mode)

class CameraTestBase(object):

    def test_distance(self):
        #TODO: distance not supported in IVA mode
        if self.camera.mode == self.mode.iva:
            return
        self.assertGreater(self.camera.default_distance, self.camera.min_distance)
        self.assertLess(self.camera.default_distance, self.camera.max_distance)
        for distance in self.distances:
            self.assertGreater(distance, self.camera.min_distance)
            self.assertLess(distance, self.camera.max_distance)
            self.camera.distance = distance
            time.sleep(0.5)
            self.assertClose(distance, self.camera.distance)

    def test_heading(self):
        self.camera.pitch = 0
        #TODO: distance not supported in IVA mode
        if self.camera.mode != self.mode.iva:
            self.camera.distance = self.camera.default_distance
        for heading in self.headings:
            self.camera.heading = heading
            time.sleep(0.01)
            self.assertClose(heading, self.camera.heading)

    def test_pitch(self):
        self.camera.heading = 0
        #TODO: distance not supported in IVA mode
        if self.camera.mode != self.mode.iva:
            self.camera.distance = self.camera.default_distance
        for pitch in self.pitches:
            self.assertGreater(pitch, self.camera.min_pitch)
            self.assertLess(pitch, self.camera.max_pitch)
            self.camera.pitch = pitch
            time.sleep(0.01)
            self.assertClose(pitch, self.camera.pitch, 0.1)

class TestCameraFlight(krpctest.TestCase, CameraTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 1000000)
        cls.conn = krpctest.connect(cls)
        cls.camera = cls.conn.space_center.camera
        cls.mode = cls.conn.space_center.CameraMode
        if cls.camera.mode != cls.mode.automatic:
            cls.camera.mode = cls.mode.automatic
        time.sleep(5)
        cls.pitches = range(-90, 90, 5)
        cls.headings = range(0, 360, 5)
        cls.distances = (1, 5, 10, 20)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

class TestCameraIVA(krpctest.TestCase, CameraTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.camera = cls.conn.space_center.camera
        cls.mode = cls.conn.space_center.CameraMode
        if cls.camera.mode != cls.mode.iva:
            cls.camera.mode = cls.mode.iva
        time.sleep(5)
        cls.pitches = range(-30, 30, 5)
        cls.headings = range(-60, 60, 5)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

class TestCameraMap(krpctest.TestCase, CameraTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 1000000)
        cls.conn = krpctest.connect(cls)
        cls.camera = cls.conn.space_center.camera
        cls.mode = cls.conn.space_center.CameraMode
        if cls.camera.mode != cls.mode.map:
            cls.camera.mode = cls.mode.map
        time.sleep(5)
        cls.pitches = range(-90, 90, 5)
        cls.headings = range(0, 360, 5)
        cls.distances = (100, 1000, 10000)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

if __name__ == '__main__':
    unittest.main()
