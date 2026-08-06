import krpctest
from krpctest.geometry import angle_between, norm


class TestEVAOnFoot(krpctest.TestCase):
    """A kerbal standing on open ground, driven by the translation and rotation
    controls."""

    # Flat, empty ground a few kilometers west of the KSC. The kerbal is moved here
    # after climbing out, so that it has room to walk without bumping into the craft
    # it came from or the launch pad structures.
    LATITUDE = -0.05
    LONGITUDE = -74.7

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.connect().testing_tools.go_eva()
        cls.set_landed("Kerbin", cls.LATITUDE, cls.LONGITUDE)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control
        # The rotating frame of the body the kerbal is standing on, so that standing
        # still is zero speed and walking is not.
        cls.reference_frame = cls.vessel.orbit.body.reference_frame

    def setUp(self):
        self.stop()

    @classmethod
    def tearDownClass(cls):
        cls.control.forward = 0
        cls.control.right = 0
        cls.control.yaw = 0

    def stop(self):
        """Zero every input and wait for the kerbal to come to rest."""
        self.control.forward = 0
        self.control.right = 0
        self.control.yaw = 0
        self.wait_until(
            lambda: self.speed() < 0.1, message="the kerbal to stop walking"
        )

    def speed(self):
        return self.vessel.flight(self.reference_frame).speed

    def walk(self, duration=3, **inputs):
        """Hold the given inputs for duration seconds and return the velocity at the
        end of it, before stopping again."""
        for name, value in inputs.items():
            setattr(self.control, name, value)
        self.wait(duration)
        velocity = self.vessel.velocity(self.reference_frame)
        self.stop()
        return velocity

    def test_walk_forward_and_back(self):
        forwards = self.walk(forward=1)
        self.assertGreater(norm(forwards), 0.2)
        backwards = self.walk(forward=-1)
        self.assertGreater(norm(backwards), 0.2)
        # Opposite inputs walk the kerbal in opposite directions.
        self.assertGreater(angle_between(forwards, backwards), 150)

    def test_walk_left_and_right(self):
        right = self.walk(right=1)
        self.assertGreater(norm(right), 0.2)
        left = self.walk(right=-1)
        self.assertGreater(norm(left), 0.2)
        self.assertGreater(angle_between(right, left), 150)

    def test_strafing_is_across_walking(self):
        forwards = self.walk(forward=1)
        right = self.walk(right=1)
        # The kerbal keeps its heading while it strafes, so the two are across each
        # other rather than along the same line.
        angle = angle_between(forwards, right)
        self.assertGreater(angle, 60)
        self.assertLess(angle, 120)

    def test_yaw_steers_while_walking(self):
        self.control.forward = 1
        self.wait(3)
        before = self.vessel.velocity(self.reference_frame)
        self.control.yaw = 0.3
        self.wait(2)
        self.control.yaw = 0
        self.wait(1)
        after = self.vessel.velocity(self.reference_frame)
        self.stop()
        self.assertGreater(angle_between(before, after), 30)

    def test_stands_still_without_input(self):
        self.wait(2)
        self.assertLess(self.speed(), 0.1)

    def test_jetpack_deploys_with_rcs(self):
        self.assertFalse(self.control.rcs)
        self.control.rcs = True
        self.wait_until(lambda: self.control.rcs, message="the jetpack to deploy")
        self.control.rcs = False
        self.wait_until(lambda: not self.control.rcs, message="the jetpack to stow")

    def test_lights(self):
        self.assertFalse(self.control.lights)
        self.control.lights = True
        self.wait()
        self.assertTrue(self.control.lights)
        self.control.lights = False
        self.wait()
        self.assertFalse(self.control.lights)


class TestEVAJetpack(krpctest.TestCase):
    """A kerbal floating in orbit, flown by its jetpack. The kerbal climbs out on the
    ground and is then put into orbit on its own: climbing out in orbit leaves it
    holding the hatch ladder, which is a state it cannot use its jetpack from."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.connect().testing_tools.go_eva()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control
        cls.reference_frame = cls.vessel.orbit.body.non_rotating_reference_frame

    def setUp(self):
        self.control.rcs = True
        self.wait_until(lambda: self.control.rcs, message="the jetpack to deploy")
        self.wait_until(
            lambda: self.rotation_rate() < 0.1, message="the kerbal to stop rotating"
        )

    @classmethod
    def tearDownClass(cls):
        for name in ["forward", "right", "up", "pitch", "yaw", "roll"]:
            setattr(cls.control, name, 0)
        cls.control.rcs = False

    def rotation_rate(self):
        return norm(self.vessel.angular_velocity(self.reference_frame))

    def velocity_change(self, duration=2, **inputs):
        """Hold the given inputs for duration seconds and return how much the kerbal's
        velocity changed over them."""
        for name, value in inputs.items():
            setattr(self.control, name, value)
        before = self.vessel.velocity(self.reference_frame)
        self.wait(duration)
        after = self.vessel.velocity(self.reference_frame)
        for name in inputs:
            setattr(self.control, name, 0)
        return tuple(a - b for a, b in zip(after, before))

    def thrust(self, **inputs):
        """The velocity change from holding the given inputs, over and above the change
        from falling around the body over the same interval."""
        falling = self.velocity_change()
        thrusting = self.velocity_change(**inputs)
        return norm(tuple(a - b for a, b in zip(thrusting, falling)))

    def test_translation_accelerates_the_kerbal(self):
        for axis in ["forward", "right", "up"]:
            self.assertGreater(self.thrust(**{axis: 1}), 2)

    def test_stowed_jetpack_does_not_thrust(self):
        self.control.rcs = False
        self.wait_until(lambda: not self.control.rcs, message="the jetpack to stow")
        self.assertLess(self.thrust(forward=1), 0.5)

    def test_rotation_turns_the_kerbal(self):
        for axis in ["pitch", "yaw", "roll"]:
            setattr(self.control, axis, 0.5)
            self.wait(2)
            rate = self.rotation_rate()
            setattr(self.control, axis, 0)
            # Half input asks for half the kerbal's turn rate.
            self.assertGreater(rate, 1)
            # Releasing the input hands the kerbal back to its attitude controller,
            # which stops it and holds the orientation it ended up at.
            self.wait_until(
                lambda: self.rotation_rate() < 0.1,
                message="the kerbal to stop rotating",
            )
