import math
import unittest
from typing import Any

from krpctools.game.ksp import TestCase

# The ROTest craft: a satellite bus, a reaction wheel, a 1.5 m by 2 m procedural service
# module tank, an AJ10-137 engine and four nitrogen RCS blocks
TANK = "RO-RFTank-SM"
ENGINE = "LiquidEngineRE-J10"
RCS = "RCSBlock.413"
DIAMETER = 1.5
LENGTH = 2
# The share of a service module tank's volume that holds propellant, in percent
UTILIZATION = 50
# The tank's contents in liters: nitrogen for the RCS, then the AJ10-137 mixture
RESOURCES = {
    "Nitrogen": 35342.92,
    "Aerozine50": 755.11,
    "MON1": 758.14,
    "Helium": 15435.21,
}


class RealismOverhaulTestCase(TestCase):
    """Realism Overhaul at real scale, with a craft built from its parts and a Procedural
    Parts tank. RO's parts do not load the stock test saves, so the tests use their own."""

    mods = ["RealismOverhaul", "RealSolarSystem", "ProceduralParts"]
    save = "krpctest_ro"

    @staticmethod
    def field(part, module_name, name):
        module = next(m for m in part.modules if m.name == module_name)
        return next(f for f in module.field_list if f.name == name)

    def check_tank(self, tank, scale):
        volume = math.pi * (DIAMETER / 2) ** 2 * LENGTH * scale * 1000
        self.assertAlmostEqual(
            volume * UTILIZATION / 100,
            float(self.field(tank, "ModuleFuelTanks", "volume").value),
            delta=0.01,
        )
        resources = tank.resources
        self.assertCountEqual(RESOURCES.keys(), resources.names)
        wet_mass = tank.dry_mass
        for name, amount in RESOURCES.items():
            self.assertAlmostEqual(amount * scale, resources.max(name), delta=0.01)
            self.assertAlmostEqual(amount * scale, resources.amount(name), delta=0.01)
            wet_mass += resources.amount(name) * resources.density(name)
        self.assertAlmostEqual(wet_mass, tank.mass, delta=0.1)


class TestRealismOverhaul(RealismOverhaulTestCase):
    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("ROTest", save=cls.save)
        cls.parts = cls.vessel.parts

    def part(self, name):
        parts = self.parts.with_name(name)
        self.assertGreater(len(parts), 0, "no part named " + name)
        return parts[0]

    def test_engine(self):
        # The AJ10-137 config: pressure-fed hypergolics, with 50 ignitions that each take a
        # little ElectricCharge
        engine = self.part(ENGINE).engine
        self.assertTrue(engine.has_real_fuels)
        self.assertEqual(50, engine.ignitions)
        self.assertEqual({"ElectricCharge": 0.05}, engine.ignition_resources)
        self.assertTrue(engine.has_ullage)
        self.assertTrue(engine.pressure_fed)
        self.assertTrue(engine.has_feed_pressure)
        self.assertCountEqual(["Aerozine50", "MON1", "Helium"], engine.propellant_names)
        self.assertTrue(engine.gimballed)
        self.assertAlmostEqual(4.5, engine.gimbal_range, places=3)
        self.assertAlmostEqual(97416, engine.max_vacuum_thrust, delta=1)
        self.assertAlmostEqual(314, engine.vacuum_specific_impulse, delta=0.1)

    def test_tank(self):
        tank = self.part(TANK)
        self.assertTrue(tank.has_real_fuels_tank)
        self.assertTrue(tank.highly_pressurized)
        self.assertAlmostEqual(
            DIAMETER,
            self.field(tank, "ProceduralShapeCylinder", "diameter").float_value,
            places=5,
        )
        self.assertAlmostEqual(
            LENGTH,
            self.field(tank, "ProceduralShapeCylinder", "length").float_value,
            places=5,
        )
        self.check_tank(tank, 1)

    def test_rcs(self):
        blocks = self.parts.with_name(RCS)
        self.assertEqual(4, len(blocks))
        for part in blocks:
            self.assertEqual(["Nitrogen"], part.rcs.propellants)
            self.assertTrue(part.rcs.has_fuel)

    def test_vessel_mass(self):
        self.assertAlmostEqual(
            sum(part.mass for part in self.parts.all), self.vessel.mass, delta=0.1
        )
        self.assertAlmostEqual(
            sum(part.dry_mass for part in self.parts.all),
            self.vessel.dry_mass,
            delta=0.1,
        )


class TestRealismOverhaulEditor(RealismOverhaulTestCase):
    tank: Any

    @classmethod
    def setUpClass(cls):
        cls.new_save(cls.save)
        cls.enter_editor("VAB", craft="ROTest")

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def setUp(self):
        editor = self.connect().space_center.editor
        editor.load_vessel("VAB", "ROTest")
        self.wait()
        self.tank = editor.vessel.parts.with_name(TANK)[0]

    def test_saved_tank(self):
        self.check_tank(self.tank, 1)

    def test_resize_tank(self):
        # Procedural Parts rescales the tank's volume and its contents with the shape
        length = self.field(self.tank, "ProceduralShapeCylinder", "length")
        dry_mass = self.tank.dry_mass
        length.float_value = LENGTH * 1.5
        self.wait()
        self.check_tank(self.tank, 1.5)
        self.assertGreater(self.tank.dry_mass, dry_mass)


class TestRealismOverhaulOrbit(RealismOverhaulTestCase):
    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("ROTest", save=cls.save)
        cls.set_circular_orbit("Earth", 300000)
        cls.engine = cls.vessel.parts.with_name(ENGINE)[0].engine

    def tearDown(self):
        control = self.vessel.control
        control.throttle = 0
        self.vessel.auto_pilot.engaged = False

    def test_autopilot(self):
        # The reaction wheel turns the craft, with no help from the engine
        ap = self.vessel.auto_pilot
        ap.reference_frame = self.vessel.orbital_reference_frame
        for direction in ((0, 1, 0), (1, 0, 0), (0, 0, 1)):
            ap.target_direction = direction
            ap.engaged = True
            ap.wait(120)
            self.assertLess(ap.error, 1)
            ap.engaged = False

    def test_ignition(self):
        engine = self.engine
        ignitions = engine.ignitions
        # The highly pressurized tank keeps the propellant settled whatever the craft's motion
        self.assertGreater(engine.propellant_stability, 0.99)
        engine.active = True
        self.vessel.control.throttle = 1
        self.wait_until(lambda: engine.thrust > 0, message="the engine to ignite")
        self.assertEqual(ignitions - 1, engine.ignitions)
        # RealFuels gives each engine a flow and specific impulse up to 1.5% and 1% from
        # the config, and varies the thrust by a few percent from one frame to the next
        available = engine.available_thrust
        self.wait_until(
            lambda: engine.thrust > available * 0.9, message="the engine to spool up"
        )
        samples = []
        for _ in range(30):
            self.wait(0.1)
            samples.append(engine.thrust)
        for thrust in samples:
            self.assertAlmostEqual(available, thrust, delta=available * 0.06)
        self.assertAlmostEqual(
            available, sum(samples) / len(samples), delta=available * 0.03
        )
        self.vessel.control.throttle = 0
        self.wait_until(lambda: engine.thrust == 0, message="the engine to shut down")


if __name__ == "__main__":
    unittest.main()
