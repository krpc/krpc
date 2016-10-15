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

            self.assertEqual(bay.state, self.state.closed)
            self.assertFalse(bay.open)

            bay.open = True
            self.wait()

            self.assertEqual(bay.state, self.state.opening)
            self.assertTrue(bay.open)

            while bay.state == self.state.opening:
                self.wait()

            self.assertEqual(bay.state, self.state.open)
            self.assertTrue(bay.open)

            bay.open = False
            self.wait()

            self.assertEqual(bay.state, self.state.closing)
            self.assertFalse(bay.open)

            while bay.state == self.state.closing:
                self.wait()

            self.assertEqual(bay.state, self.state.closed)
            self.assertFalse(bay.open)

if __name__ == '__main__':
    unittest.main()
