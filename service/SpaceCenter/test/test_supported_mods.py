import unittest

from krpctools.game.ksp import TestCase


class TestSupportedModsNotInstalled(TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic")

    def test_far(self):
        with self.assertRaisesRegex(
            RuntimeError, "FAR is not available: not installed"
        ):
            _ = self.vessel.flight().reynolds_number

    def test_real_antennas(self):
        antenna = self.vessel.parts.antennas[0]
        with self.assertRaisesRegex(
            RuntimeError, "RealAntennas is not available: not installed"
        ):
            _ = antenna.gain

    def test_real_fuels(self):
        engine = self.vessel.parts.engines[0]
        with self.assertRaisesRegex(
            RuntimeError, "RealFuels is not available: not installed"
        ):
            _ = engine.ignitions


if __name__ == "__main__":
    unittest.main()
