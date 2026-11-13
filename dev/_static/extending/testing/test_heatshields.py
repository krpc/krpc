from krpctools.game.ksp import TestCase


class TestHeatshields(TestCase):
    mods = ["Heatshields", "FAR"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Capsule")

    def test_heatshield(self):
        parts = self.vessel.parts.with_name("heatshield.large")
        self.assertEqual(1, len(parts))
