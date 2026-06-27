# pylint: disable=too-many-lines
import math
import time
import unittest

import krpctest
from krpctest import diagnostics
from krpctest.geometry import cross, dot, normalize


# pylint: disable=too-many-statements,too-many-arguments,too-many-positional-arguments,too-many-locals
def _make_autopilot_test_class(
    vessel_name,
    angle_error,
    direction_error,
    winding_limit=0.75,
    path_deviation_limit=0.25,
    nudge_rate=0.3,
    recover_timeout=30,
    rebound_limit=5.0,
    cross_axis_slack=8.0,
    roll_isolation_limit=4.0,
    control_spike_limit=3,
    saturation_limit=6.0,
    overshoot_limit=0.3,
    roll_control_limit=0.5,
    gain_jump_limit=0.15,
    hold_chatter_limit=0.1,
    slew_chatter_limit=0.2,
    hold_chatter_floor=0.3,
):
    class TestAutoPilot(krpctest.TestCase):
        @classmethod
        def setUpClass(cls):
            cls.new_save()
            cls.remove_other_vessels()
            cls.launch_vessel_from_vab(vessel_name)
            cls.set_orbit("Eve", 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
            cls.vessel = cls.connect().space_center.active_vessel
            cls.ap = cls.vessel.auto_pilot
            cls.ap.reset()
            cls.ap.sas = False
            cls.vessel.control.rcs = True
            cls.sas_mode = cls.connect().space_center.SASMode
            cls.angle_error = angle_error
            cls.direction_error = direction_error
            cls.winding_limit = winding_limit
            cls.path_deviation_limit = path_deviation_limit
            cls.nudge_rate = nudge_rate
            cls.recover_timeout = recover_timeout
            cls.rebound_limit = rebound_limit
            cls.cross_axis_slack = cross_axis_slack
            cls.roll_isolation_limit = roll_isolation_limit
            cls.control_spike_limit = control_spike_limit
            cls.saturation_limit = saturation_limit
            cls.overshoot_limit = overshoot_limit
            cls.roll_control_limit = roll_control_limit
            cls.gain_jump_limit = gain_jump_limit
            cls.hold_chatter_limit = hold_chatter_limit
            cls.slew_chatter_limit = slew_chatter_limit
            cls.hold_chatter_floor = hold_chatter_floor

        def setUp(self):
            self.connect().testing_tools.clear_rotation()
            self.apply_craft_tuning()

        def apply_craft_tuning(self):
            # Re-apply the per-craft tuning. reset() (called in tearDown and by some tests)
            # restores controller defaults, so this must run before every test -- otherwise the
            # Wobbly fixture keeps its soft tuning only for the first test and then runs the rest
            # with the stiff defaults that excite the bending mode it is meant to test against.
            if vessel_name == "AutoPilotWobbly":
                self.ap.decel_lag_correction = False
                self.ap.time_to_peak = (100, 100, 100)

        def tearDown(self):
            self.ap.reset()
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = True

        def test_equality(self):
            self.assertEqual(self.ap, self.vessel.auto_pilot)

        def wait_for_autopilot(self, timeout=60):
            self.ap.engaged = True
            self.ap.wait(timeout)
            self.ap.engaged = False

        def set_rotation(self, pitch, heading, roll=float("nan")):
            self.ap.reference_frame = self.vessel.surface_reference_frame
            self.ap.target_pitch = pitch
            self.ap.target_heading = heading
            self.ap.target_roll = roll

        def check_rotation(self, pitch, heading, roll=None):
            flight = self.vessel.flight()
            self.assertAlmostEqual(pitch, flight.pitch, delta=self.angle_error)
            # When near-vertical heading is amplified that into a larger error, by a
            # factor of 1/cos(pitch), so scale the heading tolerance to match
            self.assertAlmostEqual(
                heading,
                flight.heading,
                delta=self.angle_error / math.cos(math.radians(pitch)),
            )
            if roll is not None:
                self.assertAlmostEqual(roll, flight.roll, delta=self.angle_error)

        def set_direction(self, direction, roll=float("nan")):
            self.ap.reference_frame = self.vessel.surface_reference_frame
            self.ap.target_direction = direction
            self.ap.target_roll = roll

        def check_direction(self, direction, roll=None):
            flight = self.vessel.flight()
            self.assertAlmostEqual(
                direction, flight.direction, delta=self.direction_error
            )
            if roll is not None:
                self.assertAlmostEqual(roll, flight.roll, delta=self.angle_error)

        ######################## General autopilot tests #######################################

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

            self.ap.engaged = False
            self.assertRaises(RuntimeError, getattr, self.ap, "error")

            self.set_direction(flight.prograde, roll=0)
            # Settle to a low rotation rate before cutting torque, so the vessel
            # barely drifts while the (wheels-disabled) error readings are taken.
            self.ap.stopping_velocity_threshold = 0.02
            self.wait_for_autopilot()
            self.ap.sas = True
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = False

            self.ap.target_roll = float("nan")
            self.ap.engaged = True

            self.ap.target_direction = flight.prograde
            self.assertDegreesAlmostEqual(0, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.retrograde
            self.assertDegreesAlmostEqual(180, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.normal
            self.assertDegreesAlmostEqual(90, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.anti_normal
            self.assertDegreesAlmostEqual(90, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.radial
            self.assertDegreesAlmostEqual(90, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.anti_radial
            self.assertDegreesAlmostEqual(90, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.anti_radial
            self.assertDegreesAlmostEqual(90, self.ap.error, delta=self.angle_error)

            self.ap.target_direction = flight.prograde
            self.ap.target_roll = 30.0
            self.assertDegreesAlmostEqual(30, self.ap.error, delta=self.angle_error)

        def test_pitch_and_heading_error(self):
            # The per-axis pitch_error/heading_error getters (untested elsewhere -- test_error
            # only reads error). Settle on a known attitude, cut torque so the vessel holds
            # still, then offset each target axis by a known amount and read the error back
            # promptly (mirrors the test_error pattern).
            self.ap.engaged = False
            self.assertRaises(RuntimeError, getattr, self.ap, "pitch_error")
            self.assertRaises(RuntimeError, getattr, self.ap, "heading_error")

            self.set_rotation(0, 90, 0)
            self.ap.stopping_velocity_threshold = 0.02
            self.wait_for_autopilot()
            self.ap.sas = True
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = False
            self.ap.engaged = True

            flight = self.vessel.flight()
            base_pitch = flight.pitch
            base_heading = flight.heading

            self.ap.target_pitch = base_pitch + 10
            self.ap.target_heading = base_heading
            self.assertAlmostEqual(10, self.ap.pitch_error, delta=self.angle_error)

            self.ap.target_pitch = base_pitch
            self.ap.target_heading = base_heading + 12
            self.assertAlmostEqual(12, self.ap.heading_error, delta=self.angle_error)

        def test_roll_error(self):
            self.ap.engaged = False
            self.assertRaises(RuntimeError, getattr, self.ap, "roll_error")

            set_roll = -57
            direction = self.vessel.direction(self.vessel.surface_reference_frame)
            self.set_direction(direction, roll=set_roll)
            self.ap.stopping_velocity_threshold = 0.02
            self.wait_for_autopilot()
            self.ap.sas = True
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = False

            self.ap.engaged = True
            for roll in (0, -54, -90, 27, 45, 90):
                self.ap.target_roll = roll
                self.assertAlmostEqual(
                    abs(set_roll - roll), self.ap.roll_error, delta=self.angle_error
                )

        ######################## Tests for corner cases #######################################

        def settle_on(self, direction):
            self.set_direction(direction, roll=0)
            self.ap.stopping_velocity_threshold = 0.02
            self.wait_for_autopilot()

        def perpendicular_axis(self, direction):
            # A unit vector perpendicular to direction (in the surface frame): a spin about it
            # moves the nose sideways, i.e. is tangential when the pointing error is ~0.
            direction = normalize(direction)
            reference = (0.0, 0.0, 1.0)
            if abs(dot(direction, reference)) > 0.9:
                reference = (0.0, 1.0, 0.0)
            return normalize(cross(direction, reference))

        def capture_recovery(self, max_duration, perturb=None, perturb_after=0.0):
            # Engage the autopilot with diagnostic logging until it has held the target for a
            # second, or max_duration elapses, then return the parsed per-tick samples. Uses a
            # fixed poll loop rather than ap.wait (which raises on timeout) so the full trace is
            # captured even when the vessel never settles (e.g. a sustained orbit). If perturb is
            # given it is called once, perturb_after seconds in (e.g. to nudge or step the target
            # mid-slew); the settle test only begins after the perturbation has been applied.
            self.ap.diagnostic_logging = True
            self.ap.engaged = True
            start = time.time()
            deadline = start + max_duration
            settled_since = None
            perturbed = perturb is None
            while time.time() < deadline:
                self.wait(0.1)
                if not perturbed and time.time() - start >= perturb_after:
                    perturb()
                    perturbed = True
                    settled_since = None
                try:
                    error = self.ap.error
                except RuntimeError:
                    error = None
                if perturbed and error is not None and error < self.angle_error:
                    if settled_since is None:
                        settled_since = time.time()
                    elif time.time() - settled_since > 1.0:
                        break
                else:
                    settled_since = None
            self.ap.engaged = False
            self.ap.diagnostic_logging = False
            return diagnostics.parse_log(self.ap.diagnostic_log)

        def capture_for(self, duration):
            # Engage with diagnostic logging for a fixed wall-clock duration and return the
            # parsed samples (used to observe a steady hold rather than a transient recovery).
            self.ap.diagnostic_logging = True
            self.ap.engaged = True
            self.wait(duration)
            self.ap.engaged = False
            self.ap.diagnostic_logging = False
            return diagnostics.parse_log(self.ap.diagnostic_log)

        def test_precession_when_nudged(self):
            # Settle on target, inject a spin perpendicular to the nose (tangential, since the
            # pointing error is ~0), and confirm the nose spirals back in rather than settling
            # into a sustained orbit around the target -- the precession / limit-cycle corner
            # case.
            # NOTE: winding_limit is an initial estimate and may need in-game calibration.
            direction = self.vessel.flight().prograde
            axis = self.perpendicular_axis(direction)
            tools = self.connect().testing_tools
            tools.clear_rotation()
            self.settle_on(direction)
            angular_velocity = tuple(self.nudge_rate * value for value in axis)
            tools.apply_angular_velocity(
                angular_velocity, self.vessel.surface_reference_frame, self.vessel
            )
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            self.assertLess(
                diagnostics.total_winding(samples),
                self.winding_limit,
                "nose orbited the target",
            )
            self.assertIsNotNone(
                diagnostics.settling_time(samples, angle_threshold=self.angle_error),
                "did not settle after the nudge",
            )

        def test_great_circle_path(self):
            # A combined pitch+yaw slew should follow a straight great-circle path in the
            # roll-invariant error plane rather than curving.
            # NOTE: path_deviation_limit is an initial estimate; calibrate in-game.
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(40, 20)
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            self.assertLess(
                diagnostics.path_deviation(samples),
                self.path_deviation_limit,
                "slew path curved",
            )
            self.assertLess(
                diagnostics.overshoot(samples),
                self.overshoot_limit,
                "slew overshot the target",
            )
            self.check_rotation(40, 20)

        def test_residual_hold(self):
            # After settling, the controller should hold without a sustained limit cycle: the
            # error stays small and does not keep growing tick-to-tick.
            self.settle_on(self.vessel.flight().prograde)
            samples = self.capture_for(8)
            self.assertGreater(len(samples), 0)
            tail = samples[len(samples) // 2 :]
            self.assertLess(max(sample.err for sample in tail), 2 * self.angle_error)
            self.assertLess(diagnostics.max_radius_increase(tail), self.angle_error)

        def test_anisotropic_authority(self):
            # With an asymmetric pitch/yaw velocity cap the joint law must still converge
            # without diverging (exercises the constraint-ellipse projection). The path may
            # legitimately curve, so only convergence is asserted.
            self.ap.max_angular_velocity = (1.0, 1.0, 0.3)
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(35, 35)
            self.wait_for_autopilot()
            self.check_rotation(35, 35)

        def apply_nudge(self, axis, rate):
            angular_velocity = tuple(rate * value for value in axis)
            self.connect().testing_tools.apply_angular_velocity(
                angular_velocity, self.vessel.surface_reference_frame, self.vessel
            )

        def surface_direction(self):
            return self.vessel.direction(self.vessel.surface_reference_frame)

        def test_oblique_nudge(self):
            # A small pointing error plus an injected tangential spin: the angular velocity has
            # both a radial and a tangential component during the approach. The law should lead
            # the turn and converge with bounded winding rather than orbiting (P3).
            # NOTE: thresholds are initial estimates; calibrate in-game.
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            axis = self.perpendicular_axis(self.surface_direction())
            self.set_rotation(15, 90)
            samples = self.capture_recovery(
                self.recover_timeout,
                perturb=lambda: self.apply_nudge(axis, self.nudge_rate),
            )
            self.assertGreater(len(samples), 0)
            self.assertLess(
                diagnostics.total_winding(samples),
                self.winding_limit,
                "nose orbited the target",
            )
            self.assertIsNotNone(
                diagnostics.settling_time(samples, angle_threshold=self.angle_error),
                "did not settle after the oblique nudge",
            )
            self.check_rotation(15, 90)

        def test_nudge_mid_slew(self):
            # Inject a tangential spin partway through a large slew. The great-circle path
            # should re-converge without an S-curve or runaway overshoot (P4).
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            axis = self.perpendicular_axis(self.surface_direction())
            self.set_rotation(60, 90)
            samples = self.capture_recovery(
                self.recover_timeout,
                perturb=lambda: self.apply_nudge(axis, self.nudge_rate),
                perturb_after=1.5,
            )
            self.assertGreater(len(samples), 0)
            self.assertIsNotNone(
                diagnostics.settling_time(samples, angle_threshold=self.angle_error),
                "did not settle after the mid-slew nudge",
            )
            self.assertLess(
                diagnostics.radius_rebound(samples),
                self.rebound_limit,
                "slew rebounded outwards",
            )
            self.check_rotation(60, 90)

        def test_gyroscopic_cross_axis(self):
            # During a fast single-axis slew the omega x I*omega term induces cross-axis coupling.
            # Measure the cross-axis excursion -- the deviation perpendicular to the commanded slew
            # direction in the roll-invariant error plane -- with gyroscopic compensation off and
            # then on. A fixed-axis measure is not valid here: the roll-invariant decomposition can
            # put the *primary* motion of the slew on either the pitch or the yaw axis (it depends
            # on the FromToRotation gauge for the pointing direction), so "max yaw_error" can be the
            # slew itself rather than the coupling. The roll-invariant frame also rotates under the
            # per-tick error vector during a large slew, adding a baseline excursion; that baseline
            # is common to the off and on runs, so comparing on against off cancels it and isolates
            # the gyroscopic effect. Compensation must not make the coupling worse (and reduces it
            # on asymmetric-inertia craft). On the near-symmetric stock craft both runs are similar,
            # so this is a "not worse" guard there -- it bites on an asymmetric-MoI fixture.
            def excursion(gyro):
                self.ap.gyroscopic_compensation = gyro
                self.set_rotation(0, 90)
                self.wait_for_autopilot()
                self.set_rotation(70, 90)
                samples = self.capture_recovery(self.recover_timeout)
                self.assertGreater(len(samples), 0)
                self.check_rotation(70, 90)
                return diagnostics.cross_axis_excursion(samples)

            off = excursion(False)
            on = excursion(True)
            self.assertLessEqual(
                on,
                off + self.cross_axis_slack,
                "gyroscopic compensation increased cross-axis coupling",
            )

        def test_roll_nudge_isolation(self):
            # A pure roll (nose-axis) spin must bleed off without contaminating the pitch/yaw
            # path: the roll-invariant frame should keep roll isolated from pointing.
            self.set_rotation(0, 90, 0)
            self.wait_for_autopilot()
            nose = normalize(self.surface_direction())
            samples = self.capture_recovery(
                self.recover_timeout,
                perturb=lambda: self.apply_nudge(nose, self.nudge_rate),
            )
            self.assertGreater(len(samples), 0)
            pointing = max(
                math.hypot(sample.pitch_error, sample.yaw_error) for sample in samples
            )
            self.assertLess(
                pointing,
                self.roll_isolation_limit,
                "roll nudge contaminated pitch/yaw",
            )

        def test_antipodal_flip(self):
            # A 180-degree flip is the singularity of the error-axis construction. The vessel
            # must still pick a geodesic and converge rather than stalling at the antipode. The
            # flip uses recover_timeout (not the default 30 s) because a low-authority craft needs
            # well over 30 s to traverse 180 degrees at full torque.
            self.set_rotation(0, 90)
            self.wait_for_autopilot(self.recover_timeout)
            self.set_rotation(0, 270)
            self.wait_for_autopilot(self.recover_timeout)
            self.check_rotation(0, 270)

        def test_target_step_mid_slew(self):
            # Stepping the target mid-slew is a setpoint discontinuity. The pre-clamp acceleration
            # feedforward legitimately impulses at the step (it differentiates the velocity
            # setpoint), but the [-1, 1] clamp absorbs that impulse, so asserting on the raw
            # feedforward flags a harmless transient. Assert instead on the *post-clamp* control
            # output: the discontinuity must produce only the one clean swing onto the new heading,
            # not a burst of single-tick actuator jerks (control chatter).
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(50, 90)
            samples = self.capture_recovery(
                self.recover_timeout,
                perturb=lambda: self.set_rotation(50, 150),
                perturb_after=1.0,
            )
            self.assertGreater(len(samples), 0)
            self.assertLessEqual(
                diagnostics.control_spikes(samples),
                self.control_spike_limit,
                "control chattered at the target step",
            )
            self.check_rotation(50, 150)

        def test_torque_dropout_recovery(self):
            # Cut all torque (wheels + RCS) mid-slew, restore it, and confirm recovery without
            # an integral-windup kick (RunAxis clears the integral on a zero-torque axis).
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(40, 90)
            self.ap.engaged = True
            self.wait(0.5)
            self.vessel.control.rcs = False
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = False
            self.wait(1.0)
            self.vessel.control.rcs = True
            for wheel in self.vessel.parts.reaction_wheels:
                wheel.active = True
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            self.assertLess(
                diagnostics.radius_rebound(samples),
                self.rebound_limit,
                "windup kick on torque restore",
            )
            self.check_rotation(40, 90)

        def test_partial_torque_smoothing(self):
            # Disable the reaction wheels mid-slew but keep RCS, so available torque drops
            # sharply while staying > 0. The one-sided torque smoothing should keep the
            # autotuned gains from spiking on the drop (which would jerk the gimbal); without it
            # Kp jumps as moi/torque grows. Asserts the per-tick fractional Kp rise stays small.
            # NOTE: gain_jump_limit is an initial estimate; calibrate in-game.
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(45, 90)

            def cut_wheels():
                for wheel in self.vessel.parts.reaction_wheels:
                    wheel.active = False

            samples = self.capture_recovery(
                self.recover_timeout, perturb=cut_wheels, perturb_after=1.0
            )
            self.assertGreater(len(samples), 0)
            self.assertLess(
                diagnostics.max_gain_jump(samples),
                self.gain_jump_limit,
                "autotuned gains spiked when torque dropped",
            )
            self.check_rotation(45, 90)

        def test_reference_frame_switch_while_engaged(self):
            # All other dynamic tests hold a single (surface) reference frame. Engage on a
            # surface target, then switch to the orbital frame and re-point while still engaged;
            # the controller must re-converge in the new frame rather than glitch. Covers both
            # the switch-while-engaged path and a dynamic slew in a non-surface frame.
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            frame = self.vessel.orbital_reference_frame
            target = (0.0, 1.0, 0.0)
            self.ap.engaged = True
            self.ap.reference_frame = frame
            self.ap.target_direction = target
            self.ap.target_roll = float("nan")
            self.ap.wait(self.recover_timeout)
            self.ap.engaged = False
            self.assertAlmostEqual(
                self.vessel.direction(frame), target, delta=self.direction_error
            )

        def test_flexible_mode_slew(self):
            # A large slew must not drive the structure into a saturated limit cycle. Bounded
            # control saturation is a proxy for not fighting the bending mode. Most meaningful
            # on the flexible craft. Two guards: control saturation stays bounded, and the
            # far-field of the slew (pointing error still large, before the bang-bang brake) is
            # free of the sign-flipping chatter that the self-excitation produced -- this is
            # the slew counterpart of test_sustained_hold_chatter (the feedforward-decoupling fix
            # rather than the bandwidth-reduction fix). The near-target approach phase is excluded
            # because it legitimately carries the documented adaptive re-clamp transient.
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(50, 90)
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            self.assertLess(diagnostics.saturation_time(samples), self.saturation_limit)
            far = [sample for sample in samples if sample.err > 25]
            self.assertGreater(len(far), 0, "no far-field samples in the slew")
            self.assertLess(
                diagnostics.control_reversal_rate(far),
                self.slew_chatter_limit,
                "actuators chattered during the slew (bending-mode self-excitation)",
            )
            self.check_rotation(50, 90)

        def test_sustained_hold_chatter(self):
            # After slewing to a target the controller must hold it without a sustained
            # actuator limit cycle. On a structurally flexible craft the bending mode
            # (sampled at the root part) used to self-excite a control chatter that
            # saturated the actuators almost every tick and never decayed -- the net
            # torque averaged to ~0 and the craft rang indefinitely. The adaptive
            # chatter detector suppresses it; this asserts the settled-hold control
            # reversal rate stays low. Rigid craft pass trivially (their settled control
            # never approaches full deflection). Most meaningful on the flexible
            # (vibrating) craft.
            self.set_rotation(0, 90)
            self.wait_for_autopilot(self.recover_timeout)
            self.set_rotation(45, 90)
            self.wait_for_autopilot(self.recover_timeout)
            self.check_rotation(45, 90)
            # Observe the steady hold (the slew is over) and assert the actuators are not in a
            # sustained sign-flipping limit cycle over its second half.
            samples = self.capture_for(8)
            self.assertGreater(len(samples), 0)
            tail = samples[len(samples) // 2 :]
            self.assertLess(
                diagnostics.control_reversal_rate(tail, floor=self.hold_chatter_floor),
                self.hold_chatter_limit,
                "actuators chattered while holding (bending-mode limit cycle)",
            )

        def test_max_angular_velocity_cap(self):
            # The max_angular_velocity cap must bound the commanded target rate: slew under a
            # low uniform cap and assert no per-axis target angular velocity exceeds it (the
            # profile's Math.Min against the cap / constraint ellipse).
            # NOTE: the margin is an initial estimate; calibrate in-game.
            cap = 0.2
            self.ap.max_angular_velocity = (cap, cap, cap)
            self.set_rotation(0, 90)
            self.wait_for_autopilot()
            self.set_rotation(60, 90)
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            peak_target = max(
                max(abs(value) for value in sample.tgt_omega_ri) for sample in samples
            )
            self.assertLessEqual(
                peak_target,
                cap * 1.05,
                "target rate exceeded the cap",
            )
            self.check_rotation(60, 90)

        def test_roll_blending(self):
            # Roll is suppressed while the pointing error exceeds roll_start_angle and blends in
            # below roll_engage_angle (RollWeight), so a large slew produces no roll kick.
            # Exercises the roll_start_angle/roll_engage_angle setters. Gyroscopic compensation
            # is disabled so the roll control output reflects only the roll law, not the gyro
            # feedforward. NOTE: roll_control_limit is an initial estimate; calibrate in-game.
            self.ap.gyroscopic_compensation = False
            self.ap.roll_start_angle = 25
            self.ap.roll_engage_angle = 10
            roll_start = 25
            self.set_rotation(0, 90, 0)
            self.wait_for_autopilot()
            # Large direction change (50 deg) plus a large roll change: while the pointing error
            # is above roll_start_angle the roll axis must stay essentially un-actuated.
            self.set_rotation(50, 90, 120)
            samples = self.capture_recovery(self.recover_timeout)
            self.assertGreater(len(samples), 0)
            far = [sample for sample in samples if sample.err > roll_start]
            self.assertGreater(len(far), 0, "no samples with a large pointing error")
            peak_roll_ctrl = max(abs(sample.ctrl[1]) for sample in far)
            self.assertLess(
                peak_roll_ctrl,
                self.roll_control_limit,
                "roll actuated while the pointing error was large",
            )
            self.check_rotation(50, 90, 120)

        def test_pid_gains(self):
            # The manual pitch/roll/yaw_pid_gains get/set API (untested elsewhere) and its
            # documented auto_tune interaction: a manual set persists while auto_tune is off and
            # is overwritten by the autotuner when it is on (which also zeroes Kd).
            self.ap.auto_tune = False
            gains = (1.5, 0.25, 0.05)
            self.ap.pitch_pid_gains = gains
            self.ap.roll_pid_gains = gains
            self.ap.yaw_pid_gains = gains
            for axis_gains in (
                self.ap.pitch_pid_gains,
                self.ap.roll_pid_gains,
                self.ap.yaw_pid_gains,
            ):
                for actual, expected in zip(axis_gains, gains):
                    self.assertAlmostEqual(actual, expected, delta=1e-6)

            # Engaging with auto_tune off must not retune (overwrite) the manual gains.
            self.set_rotation(0, 90)
            self.ap.engaged = True
            self.wait(0.5)
            self.ap.engaged = False
            for actual, expected in zip(self.ap.pitch_pid_gains, gains):
                self.assertAlmostEqual(actual, expected, delta=1e-6)

            # With auto_tune on, the next engage retunes: Kp changes and Kd is driven to zero.
            self.ap.auto_tune = True
            self.ap.engaged = True
            self.wait(0.5)
            self.ap.engaged = False
            tuned = self.ap.pitch_pid_gains
            self.assertNotAlmostEqual(tuned[0], gains[0], delta=1e-6)
            self.assertAlmostEqual(tuned[2], 0, delta=1e-6)

        def test_target_rotation(self):
            # The target_rotation quaternion get/set round-trips and drives the vessel via the
            # SetTargetRotation path (which the pitch/heading/roll properties do not exercise).
            self.set_rotation(30, 120, 45)
            rotation = self.ap.target_rotation

            self.ap.reset()
            self.apply_craft_tuning()
            self.ap.reference_frame = self.vessel.surface_reference_frame
            self.ap.target_rotation = rotation
            # Quaternions q and -q denote the same rotation, so compare via |dot| ~= 1.
            readback = self.ap.target_rotation
            dot4 = sum(lhs * rhs for lhs, rhs in zip(rotation, readback))
            self.assertAlmostEqual(abs(dot4), 1.0, delta=1e-3)

            self.wait_for_autopilot()
            self.check_rotation(30, 120, 45)

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
            ap.engaged = True
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
            ap.engaged = True
            self.wait()
            ap.engaged = False
            conn.close()

            self.wait()

            conn = self.connect(use_cached=False)
            vessel = conn.space_center.active_vessel
            ap = vessel.auto_pilot
            self.assertEqual(vessel.orbital_reference_frame, ap.reference_frame)
            self.assertIsNotNone(ap.target_direction)
            self.assertEqual(30, ap.target_roll)
            conn.close()

    TestAutoPilot.__name__ = f"TestAutoPilot_{vessel_name}"
    TestAutoPilot.__qualname__ = f"TestAutoPilot_{vessel_name}"
    return TestAutoPilot


TestAutoPilotNormal = _make_autopilot_test_class(
    "AutoPilotNormal",
    angle_error=2,
    direction_error=0.1,
    # Calibrated in-game against the Normal craft.  winding_limit (0.75) and
    # path_deviation_limit (0.25) are left at the defaults: the worst observed values
    # (0.21 winding, 0.17 path deviation) already sit ~3.5x / 1.4x under them.
    overshoot_limit=0.10,
    roll_isolation_limit=2.7,
    rebound_limit=1.0,
    gain_jump_limit=0.12,
    saturation_limit=0.6,
    roll_control_limit=0.1,
    cross_axis_slack=4.0,
)
TestAutoPilotSlow = _make_autopilot_test_class(
    "AutoPilotSlow",
    angle_error=2,
    direction_error=0.1,
    # Calibrated in-game against the Slow craft (the Normal airframe with RCS thrust and
    # reaction-wheel authority both cut to 20%). It is rigid and chatter-free. Before
    # the available-torque limiter fix the over-estimated authority made it heavily
    # underdamped (~30-40% overshoot); with the fix it now settles monotonically with
    # *zero* overshoot (a 90 deg slew converges in ~12 s, error decreasing
    # monotonically), so these bounds are tightened to ~3x the worst observed (floors
    # for the near-zero metrics), matching the Normal/Nimble calibration
    # policy. recover_timeout is left high because the torque-cut tests (partial_torque
    # on 20% RCS only, full dropout recovery) still settle slowly even though the
    # unperturbed slew is fast.
    winding_limit=0.8,
    path_deviation_limit=0.25,
    nudge_rate=0.2,
    recover_timeout=90,
    rebound_limit=2.0,
    cross_axis_slack=4.0,
    roll_isolation_limit=2.5,
    control_spike_limit=3,
    saturation_limit=1.0,
    overshoot_limit=0.1,
    roll_control_limit=0.1,
    gain_jump_limit=0.12,
)
TestAutoPilotNimble = _make_autopilot_test_class(
    "AutoPilotNimble",
    angle_error=2,
    direction_error=0.1,
    # Calibrated in-game against the Nimble craft (reaction-wheel dominated, high authority;
    # a small RCS was added so test_partial_torque_smoothing keeps non-zero torque when the
    # wheels are cut). path_deviation_limit (0.25) is left at the default: observed 0.17.
    # High authority means faster slews -- more winding on recovery, harder bang-bang braking
    # reversals at a setpoint step, and a bigger rebound on a full-torque dropout.
    winding_limit=1.25,
    overshoot_limit=0.1,
    rebound_limit=9.0,
    cross_axis_slack=10.0,
    roll_isolation_limit=2.5,
    control_spike_limit=10,
    saturation_limit=0.6,
    gain_jump_limit=0.12,
    roll_control_limit=0.1,
)
TestAutoPilotWobbly = _make_autopilot_test_class(
    "AutoPilotWobbly",
    angle_error=4,
    direction_error=0.2,
    # Flexible: looser bounds, a gentler nudge to avoid exciting the bending mode, more time.
    winding_limit=1.5,
    path_deviation_limit=0.4,
    nudge_rate=0.15,
    recover_timeout=60,
    rebound_limit=12.0,
    roll_isolation_limit=8.0,
    cross_axis_slack=12.0,
    control_spike_limit=6,
    saturation_limit=15.0,
    overshoot_limit=0.6,
    roll_control_limit=0.7,
    gain_jump_limit=0.25,
)
TestAutoPilotVibrating = _make_autopilot_test_class(
    "AutoPilotSlowVibrating",
    angle_error=3,
    direction_error=0.2,
    # A long, low-authority rocket with its reaction wheels at the *far end* -- the wheels-at-the-
    # tip fixture that self-excited the bending-mode limit cycle (it used to overshoot ~60 deg
    # per swing and ring indefinitely). It runs on DEFAULT parameters (no apply_craft_tuning): the
    # adaptive chatter detector engages the rate filter, feedforward decoupling and bandwidth
    # reduction by itself. The slew far-field and the settled hold are clean (test_sustained_hold_-
    # chatter is the headline guard); the residual is an approach-phase transient as the
    # detector re-clamps at the bang-bang brake, so the spike/saturation/rebound bounds are loose
    # like the flexible Wobbly craft. All thresholds are initial estimates pending in-game
    # calibration.
    # Bounds calibrated in-game against the full dynamic suite (worst observed in parentheses):
    # winding 0.38, path 0.12, rebound 0.0, saturation 1.4 s, overshoot 0.0, gain-jump 0.04,
    # roll-control 0.14, and a clean settled hold (control_reversal_rate 0.0).
    # control_spike_limit stays loose: a mid-slew target step re-triggers the detector and the
    # approach-phase transient produces ~225 control jumps before a clean hold (the documented
    # residual a bending-mode notch would remove). cross_axis_slack is left generous (a single run
    # cannot tighten the gyro on/off comparison).
    # hold_chatter_floor=0.15 catches the ±0.2-range residual bending oscillation that the default
    # floor=0.3 misses (pairs where neither side exceeds 0.3 are not counted). Needs recalibration
    # after the filter/bandwidth changes; initial limit kept at 0.1 pending in-game data.
    winding_limit=1.5,
    path_deviation_limit=0.4,
    nudge_rate=0.15,
    recover_timeout=90,
    rebound_limit=3.0,
    cross_axis_slack=12.0,
    roll_isolation_limit=8.0,
    control_spike_limit=400,
    saturation_limit=5.0,
    overshoot_limit=0.2,
    roll_control_limit=0.3,
    gain_jump_limit=0.12,
    hold_chatter_limit=0.1,
    hold_chatter_floor=0.15,
)


class TestAutoPilotSAS(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab("AutoPilotNormal")
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
        self.ap.sas = False
        self.ap.engaged = False
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def wait_for_autopilot(self, timeout=30):
        self.ap.engaged = True
        self.ap.wait(timeout)
        self.ap.engaged = False

    def set_direction(self, direction, roll=float("nan")):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(direction, flight.direction, delta=0.1)
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=2)

    def check_sas_error(self, mode, expected):
        # KSP can briefly drop SAS back to stability assist (e.g. just after the
        # mode is set, or with no torque authority), which makes reading the
        # error throw. Re-apply the mode and read promptly, retrying on the
        # transient failure rather than waiting through it (a wait lets it
        # revert before the reading is taken).
        deadline = time.time() + 5
        while True:
            try:
                self.ap.sas = True
                self.ap.sas_mode = mode
                error = self.ap.error
                break
            except RuntimeError:
                if time.time() > deadline:
                    raise
                self.wait()
        self.assertAlmostEqual(expected, error, delta=2)

    def test_sas_error(self):
        flight = self.vessel.flight()
        self.set_direction(flight.prograde, roll=27)
        self.ap.stopping_velocity_threshold = 0.02
        self.wait_for_autopilot()
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
        ap.engaged = True
        ap.wait()
        ap.engaged = False


if __name__ == "__main__":
    unittest.main()
