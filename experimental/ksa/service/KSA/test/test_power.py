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


class TestProbeCore(ksatest.TestCase):
    """Launches a small probe core, with its own 2500 J battery and a 3 W draw."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(str(VEHICLES / "ProbeCoreTest"))
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)
        cls.part = cls.vehicle.root_part
        cls.core = cls.part.power_consumers[0]

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def tearDown(self):
        self.core.mode = self.ksa.PowerMode.auto
        self.core.standby_in_storms = False
        ksatest.wait_until(
            lambda: self.vehicle.control_state == self.ksa.ControlState.controllable
        )

    def test_control_state(self):
        self.assertEqual(self.ksa.ControlState.controllable, self.vehicle.control_state)
        self.assertTrue(self.vehicle.controllable)

    def test_power_switch(self):
        self.assertTrue(self.core.has_power_switch)
        self.assertEqual(self.ksa.PowerMode.auto, self.core.mode)
        self.assertEqual(self.ksa.PowerStatus.on, self.core.status)
        self.core.mode = self.ksa.PowerMode.off
        self.assertEqual(self.ksa.PowerMode.off, self.core.mode)
        ksatest.wait_until(
            lambda: self.vehicle.control_state == self.ksa.ControlState.probe_core_off
        )
        self.assertEqual(self.ksa.PowerStatus.off, self.core.status)
        self.assertFalse(self.vehicle.controllable)
        self.core.mode = self.ksa.PowerMode.on
        self.assertEqual(self.ksa.PowerMode.on, self.core.mode)
        ksatest.wait_until(
            lambda: self.vehicle.control_state == self.ksa.ControlState.controllable
        )
        self.assertEqual(self.ksa.PowerStatus.on, self.core.status)

    def test_standby_in_storms(self):
        self.assertFalse(self.core.standby_in_storms)
        self.core.standby_in_storms = True
        self.assertTrue(self.core.standby_in_storms)

    def test_power_forecast(self):
        forecast = self.part.power_forecast
        self.assertTrue(forecast.has_battery)
        self.assertAlmostEqual(3, forecast.draw, places=3)
        self.assertEqual(0, forecast.supply)
        self.assertGreater(forecast.charge, 2400)
        self.assertLessEqual(forecast.charge, 2500)
        self.assertAlmostEqual(
            forecast.charge / forecast.draw, forecast.time_left, delta=1
        )
        # Switched off, the core draws nothing
        self.core.mode = self.ksa.PowerMode.off
        ksatest.wait_until(lambda: forecast.draw == 0)
        self.assertEqual(math.inf, forecast.time_left)

    def test_avionics(self):
        avionics = self.part.avionics
        self.assertIsNotNone(avionics)
        self.assertEqual(self.part, avionics.part)
        self.assertEqual([avionics], self.vehicle.avionics)
        self.assertEqual(self.ksa.AvionicsHealth.ok, avionics.health)
        self.assertEqual(100, avionics.tolerance)
        self.assertGreaterEqual(avionics.dose, 0)
        self.assertLess(avionics.dose, 1)
        self.assertFalse(avionics.rebooting)
        if math.isnan(avionics.dose_rate):
            self.assertEqual(math.inf, avionics.time_left)
        else:
            self.assertGreater(avionics.time_left, 0)


class TestProbeCoreUnpowered(ksatest.TestCase):
    """Runs the battery of a small probe core flat, in about 14 minutes."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(str(VEHICLES / "ProbeCoreTest"))
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)
        cls.part = cls.vehicle.root_part
        cls.core = cls.part.power_consumers[0]
        forecast = cls.part.power_forecast
        ut = cls.ksa.ut + forecast.time_left + 60
        # The game stops the warp for its low power alerts
        for _ in range(20):
            if cls.ksa.ut >= ut - 1:
                break
            cls.ksa.warp_to(ut)

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_unpowered(self):
        self.assertEqual(
            self.ksa.ControlState.probe_core_unpowered, self.vehicle.control_state
        )
        self.assertEqual(self.ksa.PowerStatus.out, self.core.status)
        forecast = self.part.power_forecast
        self.assertTrue(forecast.has_battery)
        self.assertEqual(0, forecast.charge)
        self.assertEqual(0, forecast.time_left)


class TestNoPowerSwitch(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")

    def test_crewed_control_state(self):
        self.assertEqual(self.ksa.ControlState.controllable, self.vehicle.control_state)
        for vehicle in self.ksa.vehicles:
            self.assertEqual(
                vehicle.controllable,
                vehicle.control_state == self.ksa.ControlState.controllable,
            )

    def test_no_power_switch(self):
        consumers = [c for c in self.vehicle.power_consumers if not c.has_power_switch]
        self.assertTrue(consumers)
        consumer = consumers[0]
        with self.assertRaises(RuntimeError):
            _ = consumer.mode
        with self.assertRaises(RuntimeError):
            _ = consumer.status
        with self.assertRaises(RuntimeError):
            consumer.standby_in_storms = True
        self.assertEqual([], self.vehicle.avionics)

    def test_power_forecast(self):
        forecast = self.vehicle.root_part.power_forecast
        self.assertTrue(forecast.has_battery)
        self.assertGreater(forecast.charge, 0)
        self.assertGreaterEqual(forecast.draw, 0)
        self.assertGreaterEqual(forecast.supply, 0)
        self.assertGreater(forecast.time_left, 0)
