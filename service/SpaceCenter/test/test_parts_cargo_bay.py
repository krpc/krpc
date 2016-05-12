import unittest
import time
import krpctest

class TestPartsCargoBay(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpctest.connect().space_center.active_vessel.name != 'PartsCargoBay':
            krpctest.new_save()
            krpctest.launch_vessel_from_vab('PartsCargoBay')
            krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.cargo_bays = cls.conn.space_center.active_vessel.parts.cargo_bays
        cls.state = cls.conn.space_center.CargoBayState

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_open_close(self):
        for bay in self.cargo_bays:

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

if __name__ == '__main__':
    unittest.main()
