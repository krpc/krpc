import math

import ksatest


class TestAutoPilot(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.auto_pilot = cls.vehicle.auto_pilot
        cls.flight = cls.vehicle.flight
        cls.modes = cls.ksa.AutoPilotMode

    def tearDown(self):
        self.auto_pilot.disengage()

    def test_engage(self):
        self.auto_pilot.engage()
        self.assertTrue(self.auto_pilot.engaged)
        self.assertTrue(self.vehicle.control.stabilization)
        self.auto_pilot.disengage()
        self.assertFalse(self.auto_pilot.engaged)
        self.assertTrue(math.isnan(self.auto_pilot.error))
        self.assertTrue(math.isnan(self.auto_pilot.roll_error))

    def test_modes(self):
        for mode in (
            self.modes.hold,
            self.modes.prograde,
            self.modes.retrograde,
            self.modes.normal,
            self.modes.anti_normal,
            self.modes.radial_out,
            self.modes.radial_in,
            self.modes.surface_prograde,
            self.modes.surface_retrograde,
            self.modes.attitude,
        ):
            self.auto_pilot.mode = mode
            self.assertEqual(mode, self.auto_pilot.mode)
        with self.assertRaises(ValueError):
            self.auto_pilot.mode = self.modes.other

    def test_attitude_mode_targets_current_attitude(self):
        self.auto_pilot.mode = self.modes.hold
        self.assertTrue(math.isnan(self.auto_pilot.target_pitch))
        self.auto_pilot.mode = self.modes.attitude
        self.assertAlmostEqual(self.flight.pitch, self.auto_pilot.target_pitch, delta=1)

    def test_targets(self):
        self.auto_pilot.target_pitch_and_heading(10, 270)
        self.assertEqual(self.modes.attitude, self.auto_pilot.mode)
        self.assertAlmostEqual(10, self.auto_pilot.target_pitch)
        self.assertAlmostEqual(270, self.auto_pilot.target_heading)
        self.auto_pilot.target_pitch = -20
        self.auto_pilot.target_heading = 45
        self.assertAlmostEqual(-20, self.auto_pilot.target_pitch)
        self.assertAlmostEqual(45, self.auto_pilot.target_heading)
        self.auto_pilot.target_roll = 30
        self.assertAlmostEqual(30, self.auto_pilot.target_roll)
        self.auto_pilot.target_roll = math.nan
        self.assertTrue(math.isnan(self.auto_pilot.target_roll))

    def test_invalid_targets(self):
        with self.assertRaises(ValueError):
            self.auto_pilot.target_pitch = 91
        with self.assertRaises(ValueError):
            self.auto_pilot.target_pitch_and_heading(0, math.inf)

    def test_wait_disengaged(self):
        with self.assertRaises(RuntimeError):
            self.auto_pilot.wait()

    def test_point(self):
        # Checks the sign conventions of the targets against Flight
        for pitch, heading in ((30, 90), (-10, 200)):
            self.auto_pilot.target_pitch_and_heading(pitch, heading)
            self.auto_pilot.target_roll = math.nan
            self.auto_pilot.engage()
            self.auto_pilot.wait()
            self.assertLess(self.auto_pilot.error, 2)
            self.assertAlmostEqual(pitch, self.flight.pitch, delta=2)
            # A heading difference spans an angle scaled by the cosine of the pitch
            heading_error = (self.flight.heading - heading + 180) % 360 - 180
            self.assertAlmostEqual(
                0, heading_error * math.cos(math.radians(pitch)), delta=2
            )

    def test_track_target_attitudes(self):
        # Checks the signs of Flight against the game's own track targets. In a nearly
        # circular orbit, prograde is horizontal and normal is 90 degrees to its left.
        self.assertLess(self.vehicle.orbit.eccentricity, 0.05)
        attitudes = {}
        for mode in (self.modes.prograde, self.modes.normal, self.modes.radial_out):
            self.auto_pilot.mode = mode
            self.auto_pilot.engage()
            self.auto_pilot.wait()
            attitudes[mode] = (self.flight.pitch, self.flight.heading)
        prograde_pitch, prograde_heading = attitudes[self.modes.prograde]
        normal_pitch, normal_heading = attitudes[self.modes.normal]
        self.assertAlmostEqual(0, prograde_pitch, delta=5)
        self.assertAlmostEqual(0, normal_pitch, delta=5)
        self.assertGreater(attitudes[self.modes.radial_out][0], 85)
        heading_error = (normal_heading - (prograde_heading - 90) + 180) % 360 - 180
        self.assertAlmostEqual(0, heading_error, delta=5)
        # The orbit is eastward, so prograde heads east of north, and its heading and the
        # latitude give the inclination
        self.assertLess(self.vehicle.orbit.inclination, math.pi / 2)
        self.assertTrue(0 < prograde_heading < 180)
        latitude = math.radians(self.flight.latitude)
        self.assertAlmostEqual(
            math.cos(self.vehicle.orbit.inclination),
            math.cos(latitude) * math.sin(math.radians(prograde_heading)),
            delta=0.05,
        )

    def test_point_prograde(self):
        self.auto_pilot.mode = self.modes.prograde
        self.auto_pilot.engage()
        self.auto_pilot.wait()
        self.assertLess(self.auto_pilot.error, 2)

    def test_target_direction(self):
        frame = self.vehicle.orbital_reference_frame
        self.auto_pilot.target_direction((0, 1, 0), frame)
        self.assertEqual(self.modes.attitude, self.auto_pilot.mode)
        self.auto_pilot.engage()
        self.auto_pilot.wait()
        direction = self.vehicle.direction(frame)
        self.assertGreater(direction[1], math.cos(math.radians(3)))

    def test_invalid_direction(self):
        with self.assertRaises(ValueError):
            self.auto_pilot.target_direction((0, 0, 0), self.vehicle.reference_frame)

    def surface_velocity_pitch_and_heading(self):
        body_frame = self.vehicle.orbit.body.reference_frame
        velocity = self.ksa.transform_direction(
            self.vehicle.velocity(body_frame),
            body_frame,
            self.vehicle.surface_reference_frame,
        )
        speed = math.sqrt(sum(x * x for x in velocity))
        pitch = math.degrees(math.asin(velocity[2] / speed))
        heading = math.degrees(math.atan2(velocity[0], velocity[1])) % 360
        return pitch, heading

    def test_angle_of_attack_and_sideslip(self):
        # Pitching above and turning right of the velocity, with the vehicle level
        pitch, heading = self.surface_velocity_pitch_and_heading()
        for offset_pitch, offset_heading in ((10, 0), (-10, 0), (0, 10)):
            self.auto_pilot.target_pitch_and_heading(
                pitch + offset_pitch, (heading + offset_heading) % 360
            )
            self.auto_pilot.target_roll = 0
            self.auto_pilot.engage()
            self.auto_pilot.wait()
            self.assertAlmostEqual(offset_pitch, self.flight.angle_of_attack, delta=3)
            self.assertAlmostEqual(offset_heading, self.flight.sideslip_angle, delta=3)
