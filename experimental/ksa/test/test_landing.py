import ksatest


class TestTerrain(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]

    def test_heights(self):
        # Everest, the Mariana Trench and Cape Canaveral
        self.assertGreater(self.earth.bedrock_height(27.988, 86.925), 8000)
        self.assertLess(self.earth.bedrock_height(11.35, 142.2), -10000)
        self.assertEqual(0, self.earth.surface_height(11.35, 142.2))
        self.assertAlmostEqual(17, self.earth.surface_height(28.608, -80.604), delta=10)

    def test_surface_is_terrain_or_sea(self):
        for latitude, longitude in ((0, 0), (45, 90), (-30, -60), (60, 10)):
            self.assertEqual(
                max(self.earth.bedrock_height(latitude, longitude), 0),
                self.earth.surface_height(latitude, longitude),
            )

    def test_body_without_terrain(self):
        self.assertEqual(0, self.ksa.bodies["Jupiter"].bedrock_height(0, 0))

    def test_flight(self):
        flight = self.find_vehicle("Gemini7").flight
        self.assertAlmostEqual(
            self.earth.bedrock_height(flight.latitude, flight.longitude),
            flight.elevation,
            delta=1,
        )
        self.assertAlmostEqual(
            flight.mean_altitude - flight.elevation, flight.bedrock_altitude, delta=1e-3
        )
        self.assertAlmostEqual(
            flight.mean_altitude - max(flight.elevation, 0),
            flight.surface_altitude,
            delta=10,
        )

