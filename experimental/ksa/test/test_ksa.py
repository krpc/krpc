import ksatest


class TestKSA(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    def test_bodies(self):
        bodies = self.ksa.bodies
        self.assertIn("Earth", bodies)
        for name, body in bodies.items():
            self.assertEqual(name, body.name)

    def test_vehicles(self):
        names = [vehicle.name for vehicle in self.ksa.vehicles]
        self.assertIn("Gemini7", names)
        self.assertIn("Rocket", names)
        self.assertEqual(len(names), len(set(names)))

    def test_active_vehicle(self):
        active = self.ksa.active_vehicle
        self.assertIsNotNone(active)
        self.assertIn(active, self.ksa.vehicles)

    def test_ut_advances(self):
        ut = self.ksa.ut
        ksatest.wait_until(lambda: self.ksa.ut > ut + 0.1)

    def test_simulation_speed(self):
        try:
            self.ksa.simulation_speed = 2
            self.assertEqual(2, self.ksa.simulation_speed)
        finally:
            self.ksa.simulation_speed = 1
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_negative_simulation_speed(self):
        with self.assertRaises(ValueError):
            self.ksa.simulation_speed = -1
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_pause(self):
        try:
            self.conn.krpc.paused = True
            self.assertTrue(self.conn.krpc.paused)
            self.assertEqual(0, self.ksa.simulation_speed)
        finally:
            self.conn.krpc.paused = False
        self.assertFalse(self.conn.krpc.paused)
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_warp_to(self):
        ut = self.ksa.ut + 120
        self.ksa.warp_to(ut)
        self.assertAlmostEqual(ut, self.ksa.ut, delta=1)
        self.assertEqual(1, self.ksa.simulation_speed)

    def test_warp_to_past(self):
        ut = self.ksa.ut
        self.ksa.warp_to(ut - 10)
        self.assertLess(self.ksa.ut, ut + 1)
