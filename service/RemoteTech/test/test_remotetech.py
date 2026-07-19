import unittest
import krpctest


class TestRemoteTech(krpctest.TestCase):
    mods = ["RemoteTech"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.rt = cls.connect().remote_tech

    def test_available(self):
        self.assertTrue(self.rt.available)

    def test_ground_stations(self):
        self.assertCountEqual(self.rt.ground_stations, ["Mission Control"])


if __name__ == "__main__":
    unittest.main()
