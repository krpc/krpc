import unittest

from krpctools.game.ksp import TestCase


class TestRemoteTech(TestCase):
    mods = ["RemoteTech"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.rt = cls.connect().remote_tech

    def test_ground_stations(self):
        self.assertCountEqual(self.rt.ground_stations, ["Mission Control"])


class TestRemoteTechNotInstalled(TestCase):
    def test_not_installed(self):
        conn = self.connect()
        with self.assertRaisesRegex(
            RuntimeError, "RemoteTech is not available: not installed"
        ):
            _ = conn.remote_tech.ground_stations


if __name__ == "__main__":
    unittest.main()
