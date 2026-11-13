import unittest

from krpctools.game.ksp import TestCase


class TestPartsLaunchClamp(TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("PartsSolarPanel")
        cls.clamp = cls.vessel.parts.launch_clamps[0]

    def test_launch_clamp(self):
        self.assertEqual(self.vessel, self.clamp.part.vessel)
        self.clamp.release()
        self.wait_until(
            lambda: self.clamp.part.vessel != self.vessel,
            message="the clamp to detach from the vessel",
        )
        self.assertNotIn(self.clamp, self.vessel.parts.launch_clamps)


if __name__ == "__main__":
    unittest.main()
