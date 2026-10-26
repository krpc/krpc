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

    def test_inputs(self):
        for axis in ("pitch", "yaw", "roll", "forward", "right", "up"):
            with self.subTest(axis=axis):
                try:
                    for value in (1, -1, 0):
                        setattr(self.control, axis, value)
                        self.assertEqual(value, getattr(self.control, axis))
                    with self.assertRaises(ValueError):
                        setattr(self.control, axis, 2)
                    self.assertEqual(0, getattr(self.control, axis))
                finally:
                    setattr(self.control, axis, 0)

    def test_pulse(self):
        pulse = self.control.pulse
        try:
            self.control.pulse = not pulse
            self.assertEqual(not pulse, self.control.pulse)
        finally:
            self.control.pulse = pulse
        self.assertEqual(pulse, self.control.pulse)


class TestControlInputs(ksatest.TestCase):
    """Each test starts from the save, so the vehicle starts at rest."""

    def setUp(self):
        self.load_save()
        self.vehicle = self.ksa.active_vehicle
        self.control = self.vehicle.control
        self.control.stabilization = False
        self.frame = self.vehicle.reference_frame

    def tearDown(self):
        for axis in ("pitch", "yaw", "roll", "forward", "right", "up"):
            setattr(self.control, axis, 0)

    def angular_velocity(self):
        # The vehicle's own frame rotates with it, so measure in a fixed frame
        fixed = self.vehicle.orbit.body.non_rotating_reference_frame
        velocity = self.vehicle.angular_velocity(fixed)
        return self.ksa.transform_direction(velocity, fixed, self.frame)

    def check_rotation(self, axis, index):
        setattr(self.control, axis, 1)
        try:
            # Positive inputs turn the vehicle about the positive axis of its frame
            ksatest.wait_until(
                lambda: abs(self.angular_velocity()[index]) > 0.01, timeout=5
            )
            velocity = self.angular_velocity()
            self.assertGreater(velocity[index], 0, velocity)
        finally:
            setattr(self.control, axis, 0)

    def test_roll(self):
        self.check_rotation("roll", 0)

    def test_pitch(self):
        self.check_rotation("pitch", 1)

    def test_yaw(self):
        self.check_rotation("yaw", 2)

    def test_translation(self):
        for axis in ("forward", "right", "up"):
            with self.subTest(axis=axis):
                self.assertEqual(0, self.vehicle.flight.g_force)
                setattr(self.control, axis, 1)
                try:
                    ksatest.wait_until(
                        lambda: self.vehicle.flight.g_force > 0, timeout=5
                    )
                finally:
                    setattr(self.control, axis, 0)
                ksatest.wait_until(lambda: self.vehicle.flight.g_force == 0, timeout=5)

    def test_switch_vehicle_releases_inputs(self):
        other = next(v for v in self.ksa.vehicles if v != self.vehicle)
        self.control.pitch = 1
        self.control.forward = 1
        try:
            self.ksa.active_vehicle = other
            self.assertEqual(0, self.control.pitch)
            self.assertEqual(0, self.control.forward)
        finally:
            self.ksa.active_vehicle = self.vehicle

    def test_overrides_auto_pilot(self):
        auto_pilot = self.vehicle.auto_pilot
        auto_pilot.target_pitch_and_heading(0, 90)
        auto_pilot.target_roll = 0
        auto_pilot.engage()
        try:
            auto_pilot.wait()
            roll = self.vehicle.flight.roll
            self.control.roll = 1
            # The auto-pilot ignores its roll error while the input is held
            ksatest.wait_until(lambda: self.vehicle.flight.roll > roll + 10, timeout=10)
            self.assertEqual(0, auto_pilot.roll_error)
            self.control.roll = 0
            ksatest.wait_until(lambda: auto_pilot.roll_error > 10, timeout=5)
        finally:
            self.control.roll = 0
            auto_pilot.disengage()
