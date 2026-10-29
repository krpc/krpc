import ksatest


class TestCamera(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.camera = cls.ksa.camera
        cls.mode = cls.ksa.CameraMode

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def setUp(self):
        self.vehicle = self.ksa.active_vehicle
        self.camera.mode = self.mode.orbit
        self.camera.focused_vehicle = self.vehicle

    def test_camera(self):
        self.assertEqual(self.camera, self.ksa.camera)

    def test_mode(self):
        for mode in (self.mode.free, self.mode.map, self.mode.iva, self.mode.orbit):
            with self.subTest(mode=mode):
                self.camera.mode = mode
                self.assertEqual(mode, self.camera.mode)

    def test_orbit_view(self):
        self.camera.azimuth = 30
        self.camera.elevation = -20
        self.camera.distance = 50
        self.assertAlmostEqual(30, self.camera.azimuth)
        self.assertAlmostEqual(-20, self.camera.elevation)
        self.assertAlmostEqual(50, self.camera.distance)

    def test_map_view(self):
        self.camera.mode = self.mode.map
        self.camera.azimuth = 45
        self.camera.elevation = 60
        self.camera.distance = 2e7
        self.assertAlmostEqual(45, self.camera.azimuth)
        self.assertAlmostEqual(60, self.camera.elevation)
        self.assertAlmostEqual(2e7, self.camera.distance)

    def test_minimum_distance(self):
        self.camera.distance = 0.001
        self.assertGreater(self.camera.distance, 0.001)

    def test_invalid_view(self):
        with self.assertRaises(ValueError):
            self.camera.elevation = 91
        with self.assertRaises(ValueError):
            self.camera.distance = 0

    def test_view_in_free_mode(self):
        self.camera.mode = self.mode.free
        with self.assertRaises(RuntimeError):
            self.camera.azimuth
        with self.assertRaises(RuntimeError):
            self.camera.distance = 10

    def test_field_of_view(self):
        fov = self.camera.field_of_view
        try:
            self.camera.field_of_view = 40
            self.assertAlmostEqual(40, self.camera.field_of_view, places=4)
            with self.assertRaises(ValueError):
                self.camera.field_of_view = 10
        finally:
            self.camera.field_of_view = fov

    def test_focus_vehicle(self):
        self.assertEqual(self.vehicle, self.camera.focused_vehicle)
        self.assertIsNone(self.camera.focused_body)
        self.assertIsNone(self.camera.focused_kitten)
        (other,) = [
            v
            for v in self.ksa.vehicles
            if v.name in ("Gemini7", "Rocket") and v != self.vehicle
        ]
        self.camera.focused_vehicle = other
        self.assertEqual(other, self.camera.focused_vehicle)
        self.assertEqual(self.vehicle, self.ksa.active_vehicle)

    def test_focus_body(self):
        earth = self.ksa.bodies["Earth"]
        self.camera.focused_body = earth
        self.assertEqual(earth, self.camera.focused_body)
        self.assertIsNone(self.camera.focused_vehicle)
        self.camera.distance = 2 * earth.radius
        self.assertAlmostEqual(
            2 * earth.radius, self.camera.distance, delta=0.01 * earth.radius
        )

    def test_focus_kitten(self):
        kitten = self.ksa.kittens["Hunter"]
        self.camera.focused_kitten = kitten
        self.assertEqual(kitten, self.camera.focused_kitten)
        self.assertIsNone(self.camera.focused_vehicle)
        self.assertIsNone(self.ksa.active_kitten)

    def test_focus_kitten_not_on_eva(self):
        (kitten,) = self.find_vehicle("Gemini7").crew
        with self.assertRaises(RuntimeError):
            self.camera.focused_kitten = kitten
