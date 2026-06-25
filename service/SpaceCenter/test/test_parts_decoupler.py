import unittest
import krpctest


class TestPartsDecoupler(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsDecoupler")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        # Look parts up by language-independent internal name (part.name), not the
        # localized title. Decoupler.1 = "TD-12 Decoupler" (was "TR-18A Stack
        # Decoupler"), Separator.1 = "TS-12 Stack Separator" (was "TR-18D Stack
        # Separator"); both were renamed by KSP's parts revamp.
        cls.stack_decoupler = cls.vessel.parts.with_name("Decoupler.1")[0].decoupler
        cls.radial_decoupler = cls.vessel.parts.with_name("radialDecoupler2")[
            0
        ].decoupler
        cls.disabled_decoupler = cls.vessel.parts.with_name("Separator.1")[0].decoupler

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


if __name__ == "__main__":
    unittest.main()
