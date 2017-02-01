import unittest
import krpctest


@unittest.skipIf(not krpctest.TestCase.connect().remote_tech.available,
                 "RemoteTech not installed")
class TestRemoteTech(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.rt = cls.connect().remote_tech

    def test_ground_stations(self):
        self.assertItemsEqual(self.rt.ground_stations, ['Mission Control'])


if __name__ == '__main__':
    unittest.main()
