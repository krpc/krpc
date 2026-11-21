import ksatest

SITE = "CCSFS LC-39A"


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


class TestLaunch(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.earth = cls.ksa.bodies["Earth"]
        cls.vehicle = cls.ksa.launch_vehicle("Rocket", SITE)
        cls.flight = cls.vehicle.flight

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_launch_sites(self):
        self.assertIn(SITE, self.earth.launch_sites)
        self.assertEqual([], self.ksa.bodies["Jupiter"].launch_sites)

    def test_on_the_pad(self):
        self.assertEqual("Rocket_1", self.vehicle.name)
        self.assertEqual(self.vehicle, self.ksa.active_vehicle)
        self.assertEqual(self.earth, self.vehicle.orbit.body)
        self.assertAlmostEqual(28.608, self.flight.latitude, delta=0.01)
        self.assertAlmostEqual(-80.604, self.flight.longitude, delta=0.01)
        self.assertLess(self.flight.surface_altitude, 50)
        self.assertAlmostEqual(90, self.flight.pitch, delta=1)
        self.assertLess(self.flight.speed, 1)

    def test_invalid_launch(self):
        with self.assertRaises(ValueError):
            self.ksa.launch_vehicle("No such vehicle", SITE)
        with self.assertRaises(ValueError):
            self.ksa.launch_vehicle("Rocket", "No such site")


class TestHop(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.launch_vehicle("Rocket", SITE)
        cls.flight = cls.vehicle.flight

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_hop(self):
        # A short burn straight up, and the predicted impact on the way back down
        control = self.vehicle.control
        control.throttle = 1
        control.activate_next_sequence()
        control.engine_on = True
        ksatest.wait_until(lambda: self.flight.surface_altitude > 200, timeout=30)
        control.engine_on = False
        # The vehicle settles onto the pad after launch, and the prediction made while it
        # settled can still say impact, so wait for one in the future
        ksatest.wait_until(lambda: self.flight.time_to_impact > 0, timeout=30)
        self.assertGreater(self.flight.time_to_impact, 0)
        orbit = self.vehicle.orbit
        self.assertEqual(self.ksa.OrbitTransition.impact, orbit.end_transition)
        self.assertAlmostEqual(
            orbit.end_ut - self.ksa.ut, self.flight.time_to_impact, delta=1
        )
