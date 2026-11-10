import unittest

from krpctools.game.ksp import TestCase


class TestPartsFairing(TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsFairing")
        cls.remove_other_vessels()
        parts = cls.connect().space_center.active_vessel.parts
        cls.fairing = parts.with_name("fairingSize1")[0].fairing

    def test_jettison(self):
        self.assertFalse(self.fairing.jettisoned)
        self.fairing.jettison()
        self.assertTrue(self.fairing.jettisoned)


class TestPartsFairingProceduralFairings(TestCase):
    mods = ["ProceduralFairings"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsFairingProceduralFairings")
        cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts

    def test_fairing_parts(self):
        sides = self.parts.with_name("KzProcFairingSide1")
        self.assertEqual(2, len(sides))
        for side in sides:
            self.assertIsNotNone(side.fairing)
            self.assertEqual(side, side.fairing.part)
        base = self.parts.with_name("KzResizableFairingBase")[0]
        self.assertIsNone(base.fairing)

    def test_jettison(self):
        for side in self.parts.with_name("KzProcFairingSide1"):
            fairing = side.fairing
            self.assertFalse(fairing.jettisoned)
            # The mod decouples the side on a later frame
            fairing.jettison()
            self.wait_until(lambda f=fairing: f.jettisoned, timeout=5)
            fairing.jettison()
            self.assertTrue(fairing.jettisoned)


if __name__ == "__main__":
    unittest.main()
