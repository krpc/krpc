import unittest
import krpctest


class TestPartsCargoBay(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsCargoBay")
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

        self.wait_while(
            lambda: bay.state == self.state.opening,
            message="cargo bay to finish opening",
        )

        self.assertEqual(bay.state, self.state.open)
        self.assertTrue(bay.open)

        bay.open = False
        self.wait()

        self.assertEqual(bay.state, self.state.closing)
        self.assertFalse(bay.open)

        # After the close animation stops, ModuleCargoBay can take an extra
        # frame or two to register as closed-and-locked; until it does, the
        # state reads back as open (not moving, not yet locked). Wait for the
        # terminal closed state rather than just "no longer closing", so the
        # assertion doesn't race that lock-lag (seen on mk3CargoBayS).
        self.wait_while(
            lambda: bay.state in (self.state.closing, self.state.open),
            message="cargo bay to close and lock",
        )

        self.assertEqual(bay.state, self.state.closed)
        self.assertFalse(bay.open)
        self.assertFalse(self.control.cargo_bays)

    def test_cargo_bay(self):
        part = self.parts.with_name("mk3CargoBayS")[0]
        self.check_open_close(part.cargo_bay)

    def test_cargo_ramp(self):
        part = self.parts.with_name("mk3CargoRamp")[0]
        self.check_open_close(part.cargo_bay)

    def test_service_bay(self):
        part = self.parts.with_name("ServiceBay.250.v2")[0]
        self.check_open_close(part.cargo_bay)

    def test_control(self):
        self.assertFalse(self.control.cargo_bays)
        self.control.cargo_bays = True
        self.wait_while(
            lambda: not self.control.cargo_bays,
            message="control to report cargo bays open",
        )
        self.assertTrue(self.control.cargo_bays)
        for bay in self.parts.cargo_bays:
            self.wait_while(
                lambda bay=bay: bay.state != self.state.open,
                message="cargo bay to open",
            )
            self.assertTrue(bay.open)
        self.control.cargo_bays = False
        self.wait_while(
            lambda: self.control.cargo_bays,
            message="control to report cargo bays closed",
        )
        self.assertFalse(self.control.cargo_bays)
        for bay in self.parts.cargo_bays:
            self.wait_while(
                lambda bay=bay: bay.state != self.state.closed,
                message="cargo bay to close",
            )
            self.assertFalse(bay.open)


if __name__ == "__main__":
    unittest.main()
