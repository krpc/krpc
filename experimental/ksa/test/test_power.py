import ksatest


class TestPower(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.control = cls.vehicle.control

    def setUp(self):
        self.control.lights = False

    def tearDown(self):
        self.control.lights = False

    def test_batteries(self):
        batteries = self.vehicle.batteries
        self.assertTrue(batteries)
        for battery in batteries:
            self.assertIn(battery.part, self.vehicle.parts)
            self.assertGreater(battery.energy_capacity, 0)
            self.assertGreaterEqual(battery.stored_energy, 0)
            self.assertLessEqual(battery.stored_energy, battery.energy_capacity)
        self.assertAlmostEqual(
            sum(battery.stored_energy for battery in batteries),
            self.vehicle.stored_energy,
        )
        self.assertAlmostEqual(
            sum(battery.energy_capacity for battery in batteries),
            self.vehicle.energy_capacity,
        )

    def test_generator_engines(self):
        generators = [
            generator
            for vehicle in self.ksa.vehicles
            for generator in vehicle.generators
        ]
        if not generators:
            self.skipTest("No stock part has a generator")
        for generator in generators:
            engine = generator.engine
            if engine is not None:
                self.assertEqual(generator.part, engine.part)

    def test_part_lists(self):
        for name in (
            "batteries",
            "solar_panels",
            "generators",
            "power_consumers",
            "lights",
        ):
            with self.subTest(module=name):
                expected = [
                    module
                    for part in self.vehicle.parts
                    for module in getattr(part, name)
                ]
                self.assertEqual(expected, getattr(self.vehicle, name))

    def test_lights(self):
        self.assertFalse(self.control.lights)
        # Cabin lights are always on
        lights = [light for light in self.vehicle.lights if not light.active]
        self.assertTrue(lights)
        self.control.lights = True
        self.assertTrue(self.control.lights)
        ksatest.wait_until(lambda: all(light.active for light in lights))
        self.control.lights = False
        self.assertFalse(self.control.lights)
        ksatest.wait_until(lambda: not any(light.active for light in lights))

    def test_lights_use_power(self):
        consumers = self.vehicle.power_consumers
        self.assertTrue(consumers)
        ksatest.wait_until(lambda: not any(consumer.active for consumer in consumers))
        self.assertEqual(0, self.vehicle.net_power)
        self.control.lights = True
        ksatest.wait_until(lambda: any(consumer.active for consumer in consumers))
        power = sum(consumer.power for consumer in consumers)
        self.assertGreater(power, 0)
        self.assertAlmostEqual(-power, self.vehicle.net_power)
        # The batteries drain at that rate
        energy = self.vehicle.stored_energy
        ut = self.ksa.ut
        ksatest.wait_until(lambda: self.ksa.ut > ut + 2)
        drained = energy - self.vehicle.stored_energy
        elapsed = self.ksa.ut - ut
        self.assertAlmostEqual(power * elapsed, drained, delta=power * 0.5)
