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


if __name__ == "__main__":
    unittest.main()
