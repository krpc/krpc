import unittest
import krpctest

class TestPartsCargoBay(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsCargoBay':
            cls.launch_vessel_from_vab('PartsCargoBay')
            cls.remove_other_vessels()
        cls.cargo_bays = cls.connect().space_center.active_vessel.parts.cargo_bays
        cls.state = cls.connect().space_center.CargoBayState

    def test_open_close(self):
        for bay in self.cargo_bays:

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closed)

            bay.open = True
            self.wait()

            self.assertTrue(bay.open)
            self.assertEqual(bay.state, self.state.opening)

            while bay.state == self.state.opening:
                self.wait()

            self.assertTrue(bay.open)
            self.assertEqual(bay.state, self.state.open)

            bay.open = False
            self.wait()

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closing)

            while bay.state == self.state.closing:
                self.wait()

            self.assertFalse(bay.open)
            self.assertEqual(bay.state, self.state.closed)

if __name__ == '__main__':
    unittest.main()
