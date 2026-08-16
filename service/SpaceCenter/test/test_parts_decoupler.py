import unittest
import krpctest


class TestPartsDecoupler(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsDecoupler")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        # Look parts up by language-independent internal name (part.name), not the
        # localized title. Decoupler.1 = "TD-12 Decoupler" (was "TR-18A Stack
        # Decoupler"), Separator.1 = "TS-12 Stack Separator" (was "TR-18D Stack
        # Separator"); both were renamed by KSP's parts revamp.
        cls.stack_decoupler = cls.vessel.parts.with_name("Decoupler.1")[0].decoupler
        cls.radial_decoupler = cls.vessel.parts.with_name("radialDecoupler2")[
            0
        ].decoupler
        cls.disabled_decoupler = cls.vessel.parts.with_name("Separator.1")[0].decoupler

    def test_a_decoupler_survives_a_quickload(self):
        decoupler = self.stack_decoupler
        part = decoupler.part
        impulse = decoupler.impulse
        self.assertFalse(decoupler.decoupled)
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # The game rebuilt the part's modules, and the object names the decoupler of
        # its part rather than the module it was made from, so it reads the new one.
        self.assertEqual(impulse, decoupler.impulse)
        self.assertFalse(decoupler.decoupled)
        # And asking the part again gives that object back rather than a second one.
        self.assertEqual(decoupler, part.decoupler)

    def test_asking_twice_gives_the_same_decoupler(self):
        # Two objects for one decoupler are equal, so the server hands the object it
        # already has back rather than adding another one for every call.
        part = self.stack_decoupler.part
        self.assertEqual(self.stack_decoupler, part.decoupler)
        self.assertEqual(part.decoupler, part.decoupler)

    def test_stack_decoupler(self):
        # impulse = ejectionForce (kN) * 10. TD-12 Decoupler has ejectionForce
        # = 100 in its part cfg (was 250 for the pre-revamp TR-18A, impulse 2500).
        self.assertEqual(1000, self.stack_decoupler.impulse)
        self.assertFalse(self.stack_decoupler.decoupled)
        self.assertTrue(self.stack_decoupler.staged)
        self.assertEqual(self.vessel, self.stack_decoupler.part.vessel)
        new_vessel = self.stack_decoupler.decouple()
        self.assertTrue(self.stack_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.stack_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertCountEqual(
            ["fuelTank", "Decoupler.1"],
            [part.name for part in new_vessel.parts.all],
        )

    def test_radial_decoupler(self):
        self.assertEqual(2600, self.radial_decoupler.impulse)
        self.assertFalse(self.radial_decoupler.decoupled)
        self.assertTrue(self.radial_decoupler.staged)
        self.assertEqual(self.vessel, self.radial_decoupler.part.vessel)
        new_vessel = self.radial_decoupler.decouple()
        self.assertTrue(self.radial_decoupler.decoupled)
        self.assertNotEqual(self.vessel, self.radial_decoupler.part.vessel)
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertCountEqual(
            ["fuelTank", "fuelTank", "radialDecoupler2"],
            [part.name for part in new_vessel.parts.all],
        )

    def test_disabled_decoupler(self):
        self.assertFalse(self.disabled_decoupler.staged)


class TestPartsOmniDecoupler(krpctest.TestCase):
    """An omni-decoupler detaches at every node at once, so firing it produces two new
    vessels: the stack it separated, and the decoupler left on its own. Decouple returns
    the vessel that was separated. This needs a class of its own because firing the
    separator takes the rest of the craft with it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsDecoupler")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 250000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.separator = cls.vessel.parts.with_name("Separator.1")[0].decoupler

    def test_decouple_returns_the_separated_vessel(self):
        self.assertTrue(self.separator.is_omni_decoupler)
        new_vessel = self.separator.decouple()
        self.assertTrue(self.separator.decoupled)

        # The separator is left on a vessel of its own, which is not the one returned.
        self.assertNotEqual(new_vessel, self.separator.part.vessel)
        self.assertEqual(
            ["Separator.1"],
            [part.name for part in self.separator.part.vessel.parts.all],
        )

        # The vessel returned is the stack that was separated, below the separator.
        self.assertNotEqual(self.vessel, new_vessel)
        self.assertCountEqual(
            [
                "fuelTank",
                "radialDecoupler2",
                "fuelTank",
                "fuelTank",
                "Decoupler.1",
                "fuelTank",
            ],
            [part.name for part in new_vessel.parts.all],
        )


if __name__ == "__main__":
    unittest.main()
