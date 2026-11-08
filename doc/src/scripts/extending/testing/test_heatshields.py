from krpctools.game.ksp import TestCase


class TestHeatshields(TestCase):
    mods = ["Heatshields", "FAR"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Capsule")
        cls.vessel = cls.connect().space_center.active_vessel

    def test_heatshield(self):
        parts = self.vessel.parts.with_name("heatshield.large")
        self.assertEqual(1, len(parts))
