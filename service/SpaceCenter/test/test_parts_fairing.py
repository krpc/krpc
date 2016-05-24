import unittest
import krpctest

class TestPartsFairing(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsFairing')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        parts = cls.conn.space_center.active_vessel.parts
        cls.fairing = parts.with_title('AE-FF1 Airstream Protective Shell (1.25m)')[0].fairing

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_jettison(self):
        self.assertFalse(self.fairing.jettisoned)
        self.fairing.jettison()
        self.assertTrue(self.fairing.jettisoned)

if __name__ == '__main__':
    unittest.main()
