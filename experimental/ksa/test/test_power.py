import math

import ksatest
from test_parachute import SITE, VEHICLES


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


class TestVehicleCase(ksatest.TestCase):
    """Launches a vehicle from the test vehicles on the launch pad."""

    vehicle_folder = ""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(
            str(VEHICLES / cls.vehicle_folder)
        )
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)

    @classmethod
    def tearDownClass(cls):
        cls.load_save()


class TestSolarPanel(TestVehicleCase):
    vehicle_folder = "SolarPanelTest"

    def test_part_lists(self):
        for name in ("solar_panels", "deployables"):
            with self.subTest(module=name):
                expected = [
                    module
                    for part in self.vehicle.parts
                    for module in getattr(part, name)
                ]
                self.assertEqual(expected, getattr(self.vehicle, name))

    def test_deploy_and_retract(self):
        (deployable,) = self.vehicle.deployables
        panels = self.vehicle.solar_panels
        self.assertEqual(4, len(panels))
        self.assertEqual(deployable.part, self.vehicle.root_part)
        for panel in panels:
            self.assertEqual(deployable, panel.deployable)
        state = self.ksa.DeployableState
        self.assertEqual(state.retracted, deployable.state)
        self.assertFalse(any(panel.active for panel in panels))
        self.assertEqual(0, self.vehicle.net_power)

        deployable.deploy()
        self.assertEqual(state.deploying, deployable.state)
        ksatest.wait_until(lambda: deployable.state == state.deployed, timeout=30)
        ksatest.wait_until(lambda: all(panel.power > 0 for panel in panels))
        for panel in panels:
            self.assertTrue(panel.active)
            self.assertFalse(panel.occluded)
            self.assertGreater(panel.sun_angle, 0)
            self.assertLess(panel.sun_angle, 90)
        self.assertAlmostEqual(
            sum(panel.power for panel in panels), self.vehicle.net_power, delta=1
        )

        deployable.retract()
        self.assertEqual(state.retracting, deployable.state)
        ksatest.wait_until(lambda: deployable.state == state.retracted, timeout=30)
        ksatest.wait_until(lambda: not any(panel.active for panel in panels))
        self.assertEqual(0, self.vehicle.net_power)


class TestMagneticScoop(TestVehicleCase):
    vehicle_folder = "ScoopTest"

    def test_scoop(self):
        (scoop,) = self.vehicle.root_part.magnetic_scoops
        self.assertEqual([scoop], self.vehicle.magnetic_scoops)
        self.assertEqual(self.vehicle.root_part, scoop.part)
        self.assertEqual("Liquid Hydrogen", scoop.substance)
        # No solar wind reaches the launch pad
        self.assertEqual(0, scoop.collection_rate)

    def test_active(self):
        (scoop,) = self.vehicle.magnetic_scoops
        self.assertTrue(scoop.active)
        scoop.active = False
        self.assertFalse(scoop.active)
        scoop.active = True
        self.assertTrue(scoop.active)

    def test_no_scoop(self):
        self.assertEqual([], self.find_vehicle("Gemini7").magnetic_scoops)


class TestRadiation(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_dose_rate(self):
        rate = self.find_vehicle("Gemini7").flight.dose_rate
        self.assertGreater(rate, 0)
        self.assertTrue(math.isfinite(rate))

    def test_orbit_average_dose_rate(self):
        rate = self.find_vehicle("Gemini7").flight.orbit_average_dose_rate
        self.assertGreater(rate, 0)
        self.assertTrue(math.isfinite(rate))

    def test_no_orbit_average_on_the_pad(self):
        name = self.conn.testing_tools.register_vehicle(str(VEHICLES / "ScoopTest"))
        flight = self.ksa.launch_vehicle(name, SITE).flight
        self.assertTrue(math.isnan(flight.orbit_average_dose_rate))
        self.assertGreater(flight.dose_rate, 0)
