import ksatest


class TestVehicle(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")

    def test_equality(self):
        self.assertEqual(self.vehicle, self.find_vehicle("Gemini7"))
        self.assertNotEqual(self.vehicle, self.find_vehicle("Rocket"))

    def test_properties(self):
        self.assertEqual("Gemini7", self.vehicle.name)
        self.assertEqual(self.ksa.Situation.freefall, self.vehicle.situation)
        self.assertTrue(self.vehicle.has_launched)
        self.assertLessEqual(self.vehicle.launch_time, self.ksa.ut)
        self.assertGreater(self.vehicle.mass, 0)
        self.assertGreaterEqual(self.vehicle.propellant_mass, 0)
        self.assertLess(self.vehicle.propellant_mass, self.vehicle.mass)
        self.assertGreaterEqual(self.vehicle.available_thrust, 0)
        self.assertEqual(0, self.vehicle.thrust)
        self.assertGreaterEqual(self.vehicle.specific_impulse, 0)
        self.assertEqual("Earth", self.vehicle.orbit.body.name)

    def test_rename(self):
        try:
            self.vehicle.name = "Gemini7Renamed"
            self.assertEqual("Gemini7Renamed", self.vehicle.name)
            names = [vehicle.name for vehicle in self.ksa.vehicles]
            self.assertIn("Gemini7Renamed", names)
            self.assertNotIn("Gemini7", names)
        finally:
            self.vehicle.name = "Gemini7"
        self.assertEqual("Gemini7", self.vehicle.name)

    def test_rename_in_use(self):
        with self.assertRaises(ValueError):
            self.vehicle.name = "Rocket"
        self.assertEqual("Gemini7", self.vehicle.name)

    def test_removed_by_load(self):
        vehicle = self.find_vehicle("Rocket")
        self.load_save()
        with self.assertRaises(RuntimeError):
            _ = vehicle.name
        type(self).vehicle = self.find_vehicle("Gemini7")
