import unittest
import krpctest
import krpc
import time

class TestPartsFairing(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('PartsFairing')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestPartsFairing')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_jettison(self):
        fairing = next(iter(filter(lambda e: e.part.title == 'AE-FF1 Airstream Protective Shell (1.25m)', self.parts.fairings)))
        self.assertFalse(fairing.jettisoned)
        fairing.jettison()
        self.assertTrue(fairing.jettisoned)
if __name__ == "__main__":
    unittest.main()
