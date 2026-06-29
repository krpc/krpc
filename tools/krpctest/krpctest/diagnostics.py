"""Parsing and metrics for the autopilot diagnostic log.

The attitude controller, when ``auto_pilot.diagnostic_logging`` is enabled, emits one
``[KRPC.AP]`` line per physics tick (see ``AttitudeController.LogDiagnostics``). This module
turns ``auto_pilot.diagnostic_log`` into a list of :class:`Sample` and provides the dynamic
metrics the autopilot test plan asserts on (settling time, overshoot, path straightness,
orbit/precession winding, feedforward spikes, control saturation).

Angle and rate triples are ``(pitch, roll, yaw)`` in the roll-invariant frame, matching the
controller's convention.
"""

import math
import re

_FLOAT = r"[-+]?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?"
_LINE_PREFIX = "[KRPC.AP]"


class Sample:  # pylint: disable=too-many-instance-attributes
    # One physics tick parsed from a diagnostic-log line: one field per logged channel.
    def __init__(  # pylint: disable=too-many-arguments,too-many-positional-arguments
        self,
        time,
        err,
        ang_err,
        omega_ri,
        tgt_omega_ri,
        ff_ri,
        gyro,
        ctrl,
        kp,
        ki,
        tgt_smooth=None,
    ):
        self.time = time
        self.err = err
        self.ang_err = ang_err
        self.omega_ri = omega_ri
        self.tgt_omega_ri = tgt_omega_ri
        self.ff_ri = ff_ri
        self.gyro = gyro
        self.ctrl = ctrl
        self.kp = kp
        self.ki = ki
        # Effective (slewed) target as (pitch, heading) in degrees -- the control target after
        # target smoothing is applied. Equals the commanded target when smoothing is disabled.
        self.tgt_smooth = tgt_smooth

    @property
    def pitch_error(self):
        return self.ang_err[0]

    @property
    def yaw_error(self):
        return self.ang_err[2]

    @property
    def omega_magnitude(self):
        return math.sqrt(sum(value * value for value in self.omega_ri))


def _triple(line, label):
    matched = re.search(r"(?:^|\s)" + re.escape(label) + r"=\(([^)]*)\)", line)
    if matched is None:
        return None
    return tuple(float(value) for value in re.findall(_FLOAT, matched.group(1)))


def _scalar(line, label):
    matched = re.search(r"(?:^|\s)" + re.escape(label) + r"=(" + _FLOAT + r")", line)
    return float(matched.group(1)) if matched else None


def parse_line(line):
    if _LINE_PREFIX not in line:
        return None
    return Sample(
        time=_scalar(line, "t"),
        err=_scalar(line, "err"),
        ang_err=_triple(line, "ang_err"),
        omega_ri=_triple(line, "omega_ri"),
        tgt_omega_ri=_triple(line, "tgt_omega_ri"),
        ff_ri=_triple(line, "ff_ri"),
        gyro=_triple(line, "gyro"),
        ctrl=_triple(line, "ctrl"),
        kp=_triple(line, "Kp"),
        ki=_triple(line, "Ki"),
        tgt_smooth=_triple(line, "tgt_smooth"),
    )


def parse_log(text):
    samples = []
    for line in text.splitlines():
        sample = parse_line(line)
        if sample is not None and sample.time is not None:
            samples.append(sample)
    return samples


def error_radius(samples):
    # The 2D pitch/yaw error magnitude per tick.
    return [math.hypot(sample.pitch_error, sample.yaw_error) for sample in samples]


def settling_time(samples, angle_threshold=1.0, rate_threshold=0.05):
    # Time at which the error settles below angle_threshold (deg) and the angular velocity
    # below rate_threshold (rad/s) for the remainder of the trace. None if it never settles.
    entered = None
    for sample in samples:
        within = (
            sample.err < angle_threshold and sample.omega_magnitude < rate_threshold
        )
        if within:
            if entered is None:
                entered = sample.time
        else:
            entered = None
    return entered


def total_winding(samples):
    # Net angle, in turns, swept by the (pitch_error, yaw_error) vector. A spiral-in stays
    # well below one turn; a sustained orbit / limit cycle keeps accumulating.
    total = 0.0
    previous = None
    for sample in samples:
        angle = math.atan2(sample.yaw_error, sample.pitch_error)
        if previous is not None:
            delta = angle - previous
            while delta > math.pi:
                delta -= 2 * math.pi
            while delta < -math.pi:
                delta += 2 * math.pi
            total += abs(delta)
        previous = angle
    return total / (2 * math.pi)


def max_radius_increase(samples):
    # Largest tick-to-tick increase in the error radius. Near zero (within noise) means a
    # monotone spiral-in; a large value means the nose is orbiting back outwards.
    radius = error_radius(samples)
    return max((nxt - cur for cur, nxt in zip(radius, radius[1:])), default=0.0)


def radius_rebound(samples):
    # Largest rise in the error radius after its running minimum, in degrees: zero for a
    # monotone approach, positive if the nose overshoots or orbits back outwards. Unlike the
    # per-axis overshoot below it does not divide by an initial value, so it is well-behaved
    # when a component starts near zero (e.g. a cross-axis nudge).
    radius = error_radius(samples)
    floor = None
    rebound = 0.0
    for value in radius:
        floor = value if floor is None else min(floor, value)
        rebound = max(rebound, value - floor)
    return rebound


def _component_overshoot(values):
    initial = values[0] if values else 0.0
    if initial == 0:
        return 0.0
    peak = 0.0
    for value in values:
        if (value < 0) != (initial < 0):
            peak = max(peak, abs(value))
    return peak / abs(initial)


def overshoot(samples):
    # Largest fractional swing of the pitch or yaw error past zero, relative to its start.
    pitch = _component_overshoot([sample.pitch_error for sample in samples])
    yaw = _component_overshoot([sample.yaw_error for sample in samples])
    return max(pitch, yaw)


def path_deviation(samples):
    # Max perpendicular distance of the (pitch_error, yaw_error) trajectory from the straight
    # line between its start and the origin, normalised by the initial error magnitude. Zero
    # for a perfect great-circle slew; grows as the path curves.
    if not samples:
        return 0.0
    start_x = samples[0].pitch_error
    start_y = samples[0].yaw_error
    radius0 = math.hypot(start_x, start_y)
    if radius0 == 0:
        return 0.0
    unit_x = start_x / radius0
    unit_y = start_y / radius0
    worst = 0.0
    for sample in samples:
        perpendicular = abs(sample.pitch_error * unit_y - sample.yaw_error * unit_x)
        worst = max(worst, perpendicular)
    return worst / radius0


def feedforward_spikes(samples, threshold=0.5):
    # Number of ticks where any feedforward axis jumps by more than threshold in one step.
    count = 0
    for cur, nxt in zip(samples, samples[1:]):
        if any(abs(rhs - lhs) > threshold for lhs, rhs in zip(cur.ff_ri, nxt.ff_ri)):
            count += 1
    return count


def control_spikes(samples, threshold=0.5):
    # Number of ticks where any *actual control* axis (post-clamp, in [-1, 1]) jumps by more than
    # threshold in one step. Unlike feedforward_spikes -- which looks at the pre-clamp acceleration
    # feedforward, where a setpoint discontinuity legitimately produces a large impulse that the
    # [-1, 1] clamp then absorbs -- this measures the jerk actually commanded to the actuators.
    count = 0
    for cur, nxt in zip(samples, samples[1:]):
        if cur.ctrl is None or nxt.ctrl is None:
            continue
        if any(abs(rhs - lhs) > threshold for lhs, rhs in zip(cur.ctrl, nxt.ctrl)):
            count += 1
    return count


def control_reversal_rate(samples, floor=0.3):
    # Fraction of adjacent ticks on which a large-magnitude control axis (pitch or yaw) flips sign.
    # This is the signature of a structural / bending-mode limit cycle (test plan §11.9): the
    # actuators slam ±full deflection every tick or two, so the net torque averages to ~0. Only
    # pairs where at least one side exceeds `floor` are counted, so the small, slowly-varying
    # corrections of a settled rigid craft (which never approach full deflection) do not register.
    # Returns the worst of the pitch and yaw axes; roll is handled separately and rarely flexes.
    worst = 0.0
    for axis in (0, 2):
        series = [sample.ctrl[axis] for sample in samples if sample.ctrl is not None]
        pairs = [
            (lhs, rhs)
            for lhs, rhs in zip(series, series[1:])
            if abs(lhs) > floor or abs(rhs) > floor
        ]
        if pairs:
            flips = sum(1 for lhs, rhs in pairs if lhs * rhs < 0)
            worst = max(worst, flips / len(pairs))
    return worst


def control_oscillation_amplitude(samples):
    # RMS of the pitch/yaw control about its mean over the sample window, worst axis. This catches a
    # *low-frequency* limit cycle (e.g. the ~1.4 Hz bending mode of a large launch vehicle) that
    # control_reversal_rate misses: at ~1 Hz the control reverses sign only every ~25 ticks, not
    # tick-to-tick, so the reversal-rate reads ~0 while the actuators still swing hard. A settled
    # hold sits near zero here (only slow trim, removed by subtracting the mean); a limit cycle
    # drives it up to order 1. Use over a steady hold (constant target) so the mean is the trim.
    worst = 0.0
    for axis in (0, 2):
        series = [sample.ctrl[axis] for sample in samples if sample.ctrl is not None]
        if not series:
            continue
        mean = sum(series) / len(series)
        rms = math.sqrt(sum((value - mean) ** 2 for value in series) / len(series))
        worst = max(worst, rms)
    return worst


def saturation_time(samples, dt=None):
    # Total time any control axis is at full deflection. dt defaults to the mean tick spacing.
    if not samples:
        return 0.0
    if dt is None:
        dt = (
            (samples[-1].time - samples[0].time) / (len(samples) - 1)
            if len(samples) > 1
            else 0.0
        )
    saturated = sum(
        1 for sample in samples if any(abs(value) >= 0.999 for value in sample.ctrl)
    )
    return saturated * dt


def max_gain_jump(samples):
    # Largest single-tick fractional *increase* in any axis's Kp. The autotuner sets Kp from
    # the (one-sided smoothed) torque, so when available torque suddenly drops the smoothing
    # keeps Kp rising only a few percent per tick; without it Kp would spike. Fractional so the
    # bound is craft-independent. Ticks without parsed gains (kp is None) are skipped.
    biggest = 0.0
    for cur, nxt in zip(samples, samples[1:]):
        if cur.kp is None or nxt.kp is None:
            continue
        for previous, current in zip(cur.kp, nxt.kp):
            if previous > 0:
                biggest = max(biggest, (current - previous) / previous)
    return biggest
