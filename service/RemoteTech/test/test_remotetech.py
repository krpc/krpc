import unittest
import krpctest

class TestRemoteTech(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.rt = cls.conn.remote_tech

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_ground_stations(self):
        self.assertEquals(['Mission Control'], self.rt.ground_stations)

if __name__ == '__main__':
    unittest.main()
