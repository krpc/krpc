import unittest
import krpctest

class TestPartsCargoBay(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsCargoBay':
            cls.launch_vessel_from_vab('PartsCargoBay')
            cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts
        cls.state = cls.connect().space_center.CargoBayState

    def check_open_close(self, bay):
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

    def test_cargo_bay(self):
        part = self.parts.with_title('Mk3 Cargo Bay CRG-25')[0]
        self.check_open_close(part.cargo_bay)

    def test_cargo_ramp(self):
        part = self.parts.with_title('Mk3 Cargo Ramp')[0]
        self.check_open_close(part.cargo_bay)

    def test_service_bay(self):
        part = self.parts.with_title('Service Bay (2.5m)')[0]
        self.check_open_close(part.cargo_bay)

if __name__ == '__main__':
    unittest.main()
