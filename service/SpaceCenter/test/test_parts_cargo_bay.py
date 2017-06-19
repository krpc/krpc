import unittest
import krpctest


class TestPartsCargoBay(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != 'PartsCargoBay':
            cls.launch_vessel_from_vab('PartsCargoBay')
            cls.remove_other_vessels()
        sc = cls.connect().space_center
        vessel = sc.active_vessel
        cls.control = vessel.control
        cls.parts = vessel.parts
        cls.state = sc.CargoBayState

    def check_open_close(self, bay):
        self.assertEqual(bay.state, self.state.closed)
        self.assertFalse(bay.open)
        self.assertFalse(self.control.cargo_bays)

        bay.open = True
        self.wait()

        self.assertEqual(bay.state, self.state.opening)
        self.assertTrue(bay.open)
        self.assertTrue(self.control.cargo_bays)

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
        self.assertFalse(self.control.cargo_bays)

    def test_cargo_bay(self):
        part = self.parts.with_title('Mk3 Cargo Bay CRG-25')[0]
        self.check_open_close(part.cargo_bay)

    def test_cargo_ramp(self):
        part = self.parts.with_title('Mk3 Cargo Ramp')[0]
        self.check_open_close(part.cargo_bay)

    def test_service_bay(self):
        part = self.parts.with_title('Service Bay (2.5m)')[0]
        self.check_open_close(part.cargo_bay)

    def test_control(self):
        self.assertFalse(self.control.cargo_bays)
        self.control.cargo_bays = True
        while not self.control.cargo_bays:
            self.wait()
        self.assertTrue(self.control.cargo_bays)
        for bay in self.parts.cargo_bays:
            while bay.state != self.state.open:
                self.wait()
            self.assertTrue(bay.open)
        self.control.cargo_bays = False
        while self.control.cargo_bays:
            self.wait()
        self.assertFalse(self.control.cargo_bays)
        for bay in self.parts.cargo_bays:
            while bay.state != self.state.closed:
                self.wait()
            self.assertFalse(bay.open)


if __name__ == '__main__':
    unittest.main()
