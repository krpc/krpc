import ksatest


class TestControl(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.control = cls.vehicle.control

    def test_throttle(self):
        throttle = self.control.throttle
        try:
            self.control.throttle = 1
            self.assertEqual(1, self.control.throttle)
            self.control.throttle = 2
            self.assertEqual(1, self.control.throttle)
        finally:
            self.control.throttle = throttle
        self.assertAlmostEqual(throttle, self.control.throttle)

    def test_engine_off(self):
        self.assertFalse(self.control.engine_on)

    def test_stabilization(self):
        stabilization = self.control.stabilization
        try:
            self.control.stabilization = not stabilization
            self.assertEqual(not stabilization, self.control.stabilization)
        finally:
            self.control.stabilization = stabilization
        self.assertEqual(stabilization, self.control.stabilization)

    def test_rcs(self):
        rcs = self.control.rcs
        try:
            self.control.rcs = not rcs
            self.assertEqual(not rcs, self.control.rcs)
        finally:
            self.control.rcs = rcs
        self.assertEqual(rcs, self.control.rcs)

    def test_active_sequence(self):
        self.assertGreaterEqual(self.control.active_sequence, 0)

    def test_activate_next_sequence_inactive_vehicle(self):
        other = next(v for v in self.ksa.vehicles if v != self.vehicle)
        with self.assertRaises(RuntimeError):
            other.control.activate_next_sequence()

    def test_burn(self):
        throttle = self.control.throttle
        # The performance of the engines is known before they ignite
        self.assertGreater(self.vehicle.available_thrust, 0)
        self.assertGreater(self.vehicle.specific_impulse, 0)
        try:
            self.control.throttle = 1
            self.control.engine_on = True
            ksatest.wait_until(lambda: self.vehicle.thrust > 0, timeout=5)
            thrust = self.vehicle.thrust
            self.assertAlmostEqual(
                self.vehicle.available_thrust, thrust, delta=thrust * 0.01
            )
            # The measured acceleration excludes gravity, so is the thrust alone
            g_force = thrust / self.vehicle.mass / 9.80665
            self.assertAlmostEqual(
                g_force, self.vehicle.flight.g_force, delta=g_force * 0.05
            )
        finally:
            self.control.engine_on = False
            self.control.throttle = throttle
        ksatest.wait_until(lambda: self.vehicle.thrust == 0, timeout=5)
