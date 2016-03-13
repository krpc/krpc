import unittest
import testingtools
import krpc
import time

class TestPartsCargoBay(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if testingtools.connect().space_center.active_vessel.name != 'PartsCargoBay':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('PartsCargoBay')
            testingtools.remove_other_vessels()
        cls.conn = testingtools.connect(name='PartsCargoBay')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.CargoBayState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_open_close(self):
        for bay in self.parts.cargo_bays:

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closed)

            bay.open = True
            time.sleep(0.1)

            self.assertTrue(bay.open)
            self.assertEqual(bay.state, self.state.opening)

            while bay.state == self.state.opening:
                pass
            time.sleep(0.1)

            self.assertTrue(bay.open)
            self.assertEqual(bay.state, self.state.open)

            bay.open = False
            time.sleep(0.1)

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closing)

            while bay.state == self.state.closing:
                pass
            time.sleep(0.1)

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closed)

if __name__ == "__main__":
    unittest.main()
