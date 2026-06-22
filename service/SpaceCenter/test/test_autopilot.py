import math
import time
import unittest
import krpctest
from krpctest.geometry import normalize


class TestAutoPilot(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab("Basic")
        cls.set_orbit("Eve", 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.ap.sas = False
        cls.sas_mode = cls.connect().space_center.SASMode

    def setUp(self):
        self.connect().testing_tools.clear_rotation()

    def tearDown(self):
        # Keep a failing test from leaving the auto-pilot engaged or the
        # reaction wheels disabled, which would break every later test.
        self.ap.disengage()
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_equality(self):
        self.assertEqual(self.ap, self.vessel.auto_pilot)

    def wait_for_autopilot(self, timeout=30, max_speed=0.05):
        self.ap.engage()
        # Match the server-side Wait() criteria (pointed at the target and no
        # longer rotating), but with a timeout so a vessel that never settles
        # fails the test instead of blocking the whole suite forever.
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        deadline = time.time() + timeout
        while True:
            error = self.ap.error
            speed = sum(x * x for x in self.vessel.angular_velocity(ref)) ** 0.5
            if error <= 0.75 and speed <= max_speed:
                break
            if time.time() > deadline:
                self.ap.disengage()
                msg = "Auto-pilot did not settle within %gs (error=%.2f, speed=%.3f)"
                self.fail(msg % (timeout, error, speed))
            self.wait()
        self.ap.disengage()

    def set_rotation(self, pitch, heading, roll=float("nan")):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_pitch = pitch
        self.ap.target_heading = heading
        self.ap.target_roll = roll

    def check_rotation(self, pitch, heading, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(pitch, flight.pitch, delta=1)
        # The auto-pilot only holds the pointing direction (to ~0.75 deg);
        # near-vertical pitch amplifies that into a larger heading error, by a
        # factor of 1/cos(pitch), so scale the heading tolerance to match.
        self.assertAlmostEqual(
            heading, flight.heading, delta=1 / math.cos(math.radians(pitch))
        )
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def set_direction(self, direction, roll=float("nan")):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(direction, flight.direction, delta=0.1)
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def test_sas(self):
        self.ap.sas = True
        self.assertTrue(self.ap.sas)
        self.ap.sas = False
        self.assertFalse(self.ap.sas)

    def test_set_pitch(self):
        for pitch in (-60, -30, 0, 30, 60):
            self.set_rotation(pitch, 90)
            self.wait_for_autopilot()
            self.check_rotation(pitch, 90)

    def test_set_heading(self):
        for heading in (20, 80, 147, 340):
            self.set_rotation(0, heading)
            self.wait_for_autopilot()
            self.check_rotation(0, heading)

    def test_set_roll(self):
        for roll in (-170, -50, 0, 50, 170):
            self.set_rotation(0, 90, roll)
            self.wait_for_autopilot()
            self.check_rotation(0, 90, roll)

    def test_set_rotation(self):
        cases = [
            (50, 90, 90),
            (-50, 90, 90),
            (0, 20, 90),
            (0, 300, 90),
            (0, 90, 160),
            (0, 90, -160),
        ]

        for phr in cases:
            pitch, heading, roll = phr
            self.set_rotation(pitch, heading, roll)
            self.wait_for_autopilot()
            self.check_rotation(pitch, heading, roll)

    def test_set_direction(self):
        cases = [
            (1, 0, 0),
            (0, 1, 0),
            (0, 0, 1),
            (-1, 0, 0),
            (0, -1, 0),
            (0, 0, -1),
            (1, 2, 3),
        ]

        for direction in cases:
            direction = normalize(direction)
            self.set_direction(direction)
            self.wait_for_autopilot()
            self.check_direction(direction)

    def test_set_direction_and_roll(self):
        cases = [
            ((1, 1, 0), 23),
            ((0, 1, 1), -75),
            ((1, 0, 1), 14),
            ((-1, 0, 1), -83),
            ((0, -1, 1), -11),
            ((1, 0, -1), 2),
            ((1, 2, 3), 42),
        ]

        for direction, roll in cases:
            direction = normalize(direction)
            self.set_direction(direction, roll)
            self.wait_for_autopilot()
            self.check_direction(direction, roll)

    def test_orbital_directions(self):
        flight = self.vessel.flight()
        directions = [
            flight.prograde,
            flight.retrograde,
            flight.normal,
            flight.anti_normal,
            flight.radial,
            flight.anti_radial,
        ]
        for direction in directions:
            self.set_direction(direction)
            self.wait_for_autopilot()
            self.check_direction(direction)

    def test_error(self):
        flight = self.vessel.flight()

        self.ap.disengage()
        self.assertRaises(RuntimeError, getattr, self.ap, "error")

        self.set_direction(flight.prograde, roll=0)
        # Settle to a low rotation rate before cutting torque, so the vessel
        # barely drifts while the (wheels-disabled) error readings are taken.
        self.wait_for_autopilot(max_speed=0.02)
        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.target_roll = float("nan")
        self.ap.engage()

        self.ap.target_direction = flight.prograde
        self.assertDegreesAlmostEqual(0, self.ap.error, delta=2)

        self.ap.target_direction = flight.retrograde
        self.assertDegreesAlmostEqual(180, self.ap.error, delta=2)

        self.ap.target_direction = flight.normal
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=2)

        self.ap.target_direction = flight.anti_normal
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=2)

        self.ap.target_direction = flight.radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=2)

        self.ap.target_direction = flight.anti_radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=2)

        self.ap.target_direction = flight.anti_radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=2)

        self.ap.target_direction = flight.prograde
        self.ap.target_roll = 30.0
        self.assertDegreesAlmostEqual(30, self.ap.error, delta=2)

        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_roll_error(self):
        self.ap.disengage()
        self.assertRaises(RuntimeError, getattr, self.ap, "roll_error")

        set_roll = -57
        direction = self.vessel.direction(self.vessel.surface_reference_frame)
        self.set_direction(direction, roll=set_roll)
        self.wait_for_autopilot(max_speed=0.02)
        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.engage()
        for roll in (0, -54, -90, 27, 45, 90):
            self.ap.target_roll = roll
            self.assertAlmostEqual(abs(set_roll - roll), self.ap.roll_error, delta=2)
        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_invalid_reference_frame(self):
        frames = [
            self.vessel.reference_frame,
            self.vessel.parts.all[0].reference_frame,
            self.vessel.parts.all[0].center_of_mass_reference_frame,
            self.vessel.parts.docking_ports[0].reference_frame,
            self.vessel.parts.engines[0].thrusters[0].thrust_reference_frame,
        ]
        for frame in frames:
            self.assertRaises(
                ValueError, setattr, self.ap, "reference_frame", frame
            )

    def test_reset_on_disconnect(self):
        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        conn.close()

        self.wait()

        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.surface_reference_frame, ap.reference_frame)
        # FIXME: tuples returned from server cannot be null
        # self.assertEqual(None, ap.target_direction)
        self.assertIsNaN(ap.target_roll)
        conn.close()

    def test_dont_reset_on_clean_disconnect(self):
        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        self.wait()
        ap.disengage()
        conn.close()

        self.wait()

        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.orbital_reference_frame, ap.reference_frame)
        self.assertIsNotNone(ap.target_direction)
        self.assertEqual(30, ap.target_roll)
        conn.close()


class TestAutoPilotSAS(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab("Basic")
        cls.set_orbit("Eve", 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.sas_mode = cls.connect().space_center.SASMode
        cls.speed_mode = cls.connect().space_center.SpeedMode

    def setUp(self):
        self.connect().testing_tools.clear_rotation()

    def tearDown(self):
        # Keep a failing test from leaving the auto-pilot engaged or the
        # reaction wheels disabled, which would break every later test.
        self.ap.disengage()
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def wait_for_autopilot(self, timeout=30, max_speed=0.05):
        self.ap.engage()
        # Match the server-side Wait() criteria (pointed at the target and no
        # longer rotating), but with a timeout so a vessel that never settles
        # fails the test instead of blocking the whole suite forever.
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        deadline = time.time() + timeout
        while True:
            error = self.ap.error
            speed = sum(x * x for x in self.vessel.angular_velocity(ref)) ** 0.5
            if error <= 0.75 and speed <= max_speed:
                break
            if time.time() > deadline:
                self.ap.disengage()
                msg = "Auto-pilot did not settle within %gs (error=%.2f, speed=%.3f)"
                self.fail(msg % (timeout, error, speed))
            self.wait()
        self.ap.disengage()

    def set_direction(self, direction, roll=float("nan")):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(direction, flight.direction, delta=0.1)
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def check_sas_error(self, mode, expected):
        # Setting the SAS mode takes a physics frame to apply; give it one
        # before reading the error or it may still report stability assist.
        self.ap.sas_mode = mode
        self.wait()
        self.assertAlmostEqual(expected, self.ap.error, delta=2)

    def test_sas_error(self):
        flight = self.vessel.flight()
        self.set_direction(flight.prograde, roll=27)
        self.wait_for_autopilot(max_speed=0.02)
        self.check_direction(flight.prograde, roll=27)

        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.speed_mode = self.speed_mode.orbit

        self.check_sas_error(self.sas_mode.prograde, 0)
        self.check_sas_error(self.sas_mode.retrograde, 180)
        self.check_sas_error(self.sas_mode.normal, 90)
        self.check_sas_error(self.sas_mode.anti_normal, 90)
        self.check_sas_error(self.sas_mode.radial, 90)
        self.check_sas_error(self.sas_mode.anti_radial, 90)

        self.ap.sas = False
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True


class TestAutoPilotOtherVessel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_orbit("Eve", 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        space_center = cls.connect().space_center
        next(iter(space_center.active_vessel.parts.docking_ports)).undock()
        cls.vessel = space_center.active_vessel
        cls.other_vessel = next(v for v in space_center.vessels if v != cls.vessel)

    def test_autopilot(self):
        ap = self.other_vessel.auto_pilot
        ap.target_pitch_and_heading(0, 0)
        ap.target_roll = 0
        ap.engage()
        ap.wait()
        ap.disengage()


if __name__ == "__main__":
    unittest.main()
