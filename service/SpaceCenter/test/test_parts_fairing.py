import unittest
import testingtools
import krpc
import time

class TestPartsFairing(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsFairing')
        testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='TestPartsFairing')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_jettison(self):
        print [p.title for p in self.parts.all]
        fairing = next(iter(filter(lambda e: e.part.title == 'AE-FF1 Airstream Protective Shell (1.25m)', self.parts.fairings)))
        self.assertFalse(fairing.jettisoned)
        fairing.jettison()
        self.assertTrue(fairing.jettisoned)
if __name__ == "__main__":
    unittest.main()
