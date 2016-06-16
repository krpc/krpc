import unittest
import krpctest

class TestRemoteTech(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.rt = cls.connect().remote_tech

    def test_ground_stations(self):
        self.assertEqual(['Mission Control'], self.rt.ground_stations)

if __name__ == '__main__':
    unittest.main()
