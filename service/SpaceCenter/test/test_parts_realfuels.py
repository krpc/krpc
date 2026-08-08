import time
import unittest

import krpctest


class TestPartsRealFuels(krpctest.TestCase):
    """Exercise the RealFuels state on the SpaceCenter Engine and Part classes.

    RealFuels rewrites the fuels of the stock parts rather than adding parts of its
    own, and its stock engine configs turn stock engines into RealFuels engines. So
    the engine test craft serves here too: it carries both a pump-fed engine (the
    LV-909 "Terrier") and a pressure-fed one (the O-10 "Puff"), along with the tanks
    that feed them.
    """

    mods = ["RealFuels"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsEngine")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def part_with_name(self, name):
        parts = self.parts.with_name(name)
        self.assertGreater(len(parts), 0, "no part named " + name)
        return parts[0]

    def test_available(self):
        self.assertTrue(self.connect().space_center.real_fuels_available)

    def test_pump_fed_engine(self):
        # LV-909 "Terrier", which RealFuels gives 24 ignitions, each costing a little
        # ElectricCharge, and an ullage-prone hypergolic propellant.
        engine = self.part_with_name("liquidEngine3.v2").engine
        self.assertTrue(engine.has_real_fuels)
        self.assertEqual(24, engine.ignitions)
        self.assertEqual({"ElectricCharge": 0.5}, engine.ignition_resources)
        self.assertTrue(engine.has_ullage)
        self.assertFalse(engine.pressure_fed)
        # An engine that is not pressure fed is never short of feed pressure.
        self.assertTrue(engine.has_feed_pressure)

    def test_pressure_fed_engine(self):
        # O-10 "Puff", which RealFuels feeds from tank pressure rather than a pump.
        engine = self.part_with_name("omsEngine").engine
        self.assertTrue(engine.has_real_fuels)
        self.assertTrue(engine.pressure_fed)

    def test_propellant_stability(self):
        engine = self.part_with_name("liquidEngine3.v2").engine
        stability = engine.propellant_stability
        reliability = engine.propellant_reliability
        self.assertGreaterEqual(stability, 0)
        self.assertLessEqual(stability, 1)
        self.assertGreaterEqual(reliability, 0)
        self.assertLessEqual(reliability, 1)
        # Reliability is the stability scaled towards 1, so it never reads lower.
        self.assertGreaterEqual(reliability, stability - 0.001)

    def test_engine_not_managed_by_real_fuels(self):
        # The IX-6315 "Dawn" is an ion engine, which RealFuels leaves alone.
        engine = self.part_with_name("ionEngine").engine
        self.assertFalse(engine.has_real_fuels)
        for name in (
            "ignitions",
            "ignition_resources",
            "has_ullage",
            "propellant_stability",
            "propellant_reliability",
            "pressure_fed",
            "has_feed_pressure",
        ):
            self.assertRaises(RuntimeError, getattr, engine, name)

    def test_tank(self):
        tanks = [part for part in self.parts.all if part.has_real_fuels_tank]
        self.assertGreater(len(tanks), 0, "no RealFuels tank on the craft")
        # Both pressurized and unpressurized tanks are on the craft: RealFuels pressurizes
        # the monopropellant ones (the pod, the RCS tank) but not the main fuel tanks.
        self.assertTrue(any(tank.highly_pressurized for tank in tanks))
        self.assertTrue(any(not tank.highly_pressurized for tank in tanks))
        for tank in tanks:
            # The craft was saved without RealFuels, so its tanks still hold the stock
            # fuels, none of which RealFuels treats as cryogenic. TestPartsRealFuelsBoiloff
            # covers a tank that does boil off.
            self.assertEqual(0, tank.boiloff_rate)

    def test_part_that_is_not_a_tank(self):
        # An engine holds no propellant of its own, so RealFuels gives it no tank. A
        # command pod would not do here: RealFuels does give one of those a tank, for
        # its monopropellant.
        engine_part = self.part_with_name("liquidEngine3.v2")
        self.assertFalse(engine_part.has_real_fuels_tank)
        self.assertRaises(RuntimeError, getattr, engine_part, "highly_pressurized")
        self.assertRaises(RuntimeError, getattr, engine_part, "boiloff_rate")


class TestPartsRealFuelsEditor(krpctest.TestCase):
    """Exercise the RealFuels state that can be read from the editor.

    RealFuels sets an engine's ignitions, ullage and pressure feeding from its config
    when the part loads, so they read the same in the editor as in flight and are worth
    knowing before launching. What an engine's propellant is currently doing is not:
    RealFuels only builds the ullage set once the engine starts, so the settledness and
    feed pressure properties stay flight-only.
    """

    mods = ["RealFuels"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="PartsEngine")
        cls.parts = cls.connect().space_center.editor.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def part_with_name(self, name):
        parts = self.parts.with_name(name)
        self.assertGreater(len(parts), 0, "no part named " + name)
        return parts[0]

    def test_pump_fed_engine(self):
        engine = self.part_with_name("liquidEngine3.v2").engine
        self.assertTrue(engine.has_real_fuels)
        self.assertEqual(24, engine.ignitions)
        self.assertEqual({"ElectricCharge": 0.5}, engine.ignition_resources)
        self.assertTrue(engine.has_ullage)
        self.assertFalse(engine.pressure_fed)

    def test_pressure_fed_engine(self):
        # The O-10 "Puff" is pressure fed, and RealFuels lets it relight indefinitely.
        engine = self.part_with_name("omsEngine").engine
        self.assertTrue(engine.has_real_fuels)
        self.assertTrue(engine.pressure_fed)
        self.assertEqual(-1, engine.ignitions)

    def test_engine_not_managed_by_real_fuels(self):
        engine = self.part_with_name("ionEngine").engine
        self.assertFalse(engine.has_real_fuels)
        for name in ("ignitions", "ignition_resources", "has_ullage", "pressure_fed"):
            self.assertRaises(RuntimeError, getattr, engine, name)

    def test_tank(self):
        tanks = [part for part in self.parts.all if part.has_real_fuels_tank]
        self.assertGreater(len(tanks), 0, "no RealFuels tank on the craft")
        self.assertTrue(any(tank.highly_pressurized for tank in tanks))
        self.assertTrue(any(not tank.highly_pressurized for tank in tanks))
        for tank in tanks:
            # Nothing boils off in the editor.
            self.assertEqual(0, tank.boiloff_rate)


class TestPartsRealFuelsBoiloff(krpctest.TestCase):
    """Exercise Part.BoiloffRate against a tank that is actually boiling off.

    The other craft fixtures were saved without RealFuels, so their tanks hold the
    stock fuels; RealFuels models none of those as cryogenic, and a tank holding
    them never boils off. PartsRealFuels is written with RealFuels' own contents
    instead - liquid oxygen, which is cryogenic and boils off once the tank is warmer
    than it. The vessel is put in orbit because RealFuels pins the temperature of a
    tank still being fed by a launch clamp and skips its boiloff entirely.
    """

    mods = ["RealFuels"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsRealFuels")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.tank = cls.space_center.active_vessel.parts.with_name("fuelTank")[0]

    def test_boiloff_rate(self):
        self.assertTrue(self.tank.has_real_fuels_tank)
        self.assertGreater(self.tank.boiloff_rate, 0)

    def test_boiloff_rate_matches_the_propellant_lost(self):
        # Boiloff is reported as a rate, so it has to agree with how fast the tank is
        # actually emptying. Comparing the two catches the reported figure being out by
        # the tonnes-to-kilograms conversion or by the length of the update it was
        # measured over. The rate falls as the tank cools towards the boiling point, so
        # the average over the window sits below the rate read at the start of it; an
        # order of magnitude is all this can pin down, and all it needs to.
        density = self.space_center.Resources.density("LqdOxygen")
        start_amount = self.tank.resources.amount("LqdOxygen")
        start_time = self.space_center.ut
        rate = self.tank.boiloff_rate
        while self.space_center.ut - start_time < 5:
            time.sleep(0.5)
        lost = (start_amount - self.tank.resources.amount("LqdOxygen")) * density
        elapsed = self.space_center.ut - start_time
        self.assertGreater(lost, 0)
        # Bracket the reported rate against the measured average rather than the other
        # way round, so that a rate wrong by a constant factor cannot widen the window
        # it is checked against and pass anyway.
        average = lost / elapsed
        self.assertGreater(rate, average / 5)
        self.assertLess(rate, average * 5)

    def test_boiloff_continues_under_time_warp(self):
        # Under rails warp the vessel is no longer under physics simulation, and
        # RealFuels computes boiloff over the much longer steps the game takes instead.
        # It is still reported, and still as a rate per second of in-game time.
        self.space_center.rails_warp_factor = 3
        try:
            start_amount = self.tank.resources.amount("LqdOxygen")
            self.assertGreater(self.tank.boiloff_rate, 0)
            time.sleep(2)
            self.assertLess(self.tank.resources.amount("LqdOxygen"), start_amount)
        finally:
            self.space_center.rails_warp_factor = 0

    def test_boiloff_rate_of_a_tank_holding_nothing_cryogenic(self):
        # The pod's tank holds hydrazine, which is storable rather than cryogenic.
        pod = self.space_center.active_vessel.parts.with_name("mk1pod.v2")[0]
        self.assertTrue(pod.has_real_fuels_tank)
        self.assertEqual(0, pod.boiloff_rate)


if __name__ == "__main__":
    unittest.main()
