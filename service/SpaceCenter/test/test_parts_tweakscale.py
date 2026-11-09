import unittest

from krpctools.game.ksp import Mixin, TestCase


class TweakScaleTests(Mixin):
    """The SpaceCenter part classes on parts scaled by TweakScale Rescaled.

    The test craft has each part twice, once at its default size and once at twice that:
    an FL-T400 tank with an LV-T45 engine below it, and a Small Inline Reaction Wheel.
    Each scaled stat is compared with the same stat on the unscaled part, by the exponent
    TweakScale scales it with. Most stats scale with the cube of the scale, and an
    engine's thrust, mass and cost with its 2.5th power.
    """

    craft = "PartsTweakScale"
    scale = 2
    # The engine properties available in the scene
    thrust_properties = ("max_vacuum_thrust",)

    @classmethod
    def pair(cls, parts, name):
        """The unscaled and scaled parts with the given name."""
        found = sorted(parts.with_name(name), key=lambda part: part.mass)
        assert len(found) == 2, "expected two parts named " + name
        return found

    @classmethod
    def find_parts(cls, parts):
        cls.parts = parts
        cls.tanks = cls.pair(parts, "fuelTank")
        cls.engines = cls.pair(parts, "liquidEngine2.v2")
        cls.wheels = cls.pair(parts, "sasModule")

    def assert_scaled(self, unscaled, scaled, exponent):
        expected = unscaled * self.scale**exponent
        self.assertAlmostEqual(expected, scaled, delta=abs(expected) * 1e-4)

    def test_tank_resources(self):
        unscaled, scaled = self.tanks
        for name in ("LiquidFuel", "Oxidizer"):
            self.assert_scaled(
                unscaled.resources.max(name), scaled.resources.max(name), 3
            )
            self.assert_scaled(
                unscaled.resources.amount(name), scaled.resources.amount(name), 3
            )

    def test_tank_mass(self):
        unscaled, scaled = self.tanks
        self.assert_scaled(unscaled.dry_mass, scaled.dry_mass, 3)
        self.assert_scaled(unscaled.mass, scaled.mass, 3)

    def test_engine_mass(self):
        unscaled, scaled = self.engines
        self.assert_scaled(unscaled.mass, scaled.mass, 2.5)

    def test_engine_thrust(self):
        unscaled, scaled = (part.engine for part in self.engines)
        for name in self.thrust_properties:
            self.assert_scaled(getattr(unscaled, name), getattr(scaled, name), 2.5)

    def test_engine_specific_impulse(self):
        unscaled, scaled = (part.engine for part in self.engines)
        self.assertAlmostEqual(
            unscaled.vacuum_specific_impulse, scaled.vacuum_specific_impulse
        )

    def test_reaction_wheel_torque(self):
        unscaled, scaled = (part.reaction_wheel for part in self.wheels)
        for unscaled_range, scaled_range in zip(unscaled.max_torque, scaled.max_torque):
            for unscaled_torque, scaled_torque in zip(unscaled_range, scaled_range):
                self.assert_scaled(unscaled_torque, scaled_torque, 3)

    def test_cost(self):
        for (unscaled, scaled), exponent in (
            (self.tanks, 3),
            (self.wheels, 3),
            (self.engines, 2.5),
        ):
            self.assert_scaled(unscaled.cost, scaled.cost, exponent)


class TestPartsTweakScaleRescaledEditor(TweakScaleTests, TestCase):
    mods = ["TweakScaleRescaled"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft=cls.craft)
        cls.vessel = cls.connect().space_center.editor.vessel
        cls.find_parts(cls.vessel.parts)

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def test_vessel_cost(self):
        # The craft's tanks are full, so the part costs add up to the game's ship cost
        total = sum(part.cost for part in self.parts.all)
        self.assertAlmostEqual(self.vessel.cost, total, delta=0.01)

    def test_vessel_mass(self):
        total = sum(part.mass for part in self.parts.all)
        self.assertAlmostEqual(self.vessel.mass, total, delta=0.1)


class TestPartsTweakScaleRescaledFlight(TweakScaleTests, TestCase):
    mods = ["TweakScaleRescaled"]
    thrust_properties = ("max_thrust", "max_vacuum_thrust", "available_thrust")

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab(cls.craft)
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.find_parts(cls.vessel.parts)


if __name__ == "__main__":
    unittest.main()
