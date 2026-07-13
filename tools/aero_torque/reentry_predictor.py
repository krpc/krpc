#!/usr/bin/env python3
"""
reentry_predictor.py

Attitude-aware re-entry landing prediction built on the stock aerodynamic
simulation endpoints (issue #914 demo):

    Flight.SimulateAerodynamicForceAt(body, position, velocity, rotation)
    Flight.SimulateAerodynamicTorqueAt(body, position, velocity, rotation,
                                       angular_velocity)

Two predictors share one integrator, differing only in what they assume about
attitude:

  baseline   3-DOF point mass. Assumes the craft holds surface retrograde
             (heatshield into the wind) for the whole descent and samples only
             the FORCE endpoint at that fixed attitude. This is what was
             buildable before the torque endpoint existed, so it is the
             "before" in the A/B comparison.

  6dof       Integrates translation AND rotation. Each step samples the force
             endpoint at the integrated attitude and the torque endpoint at the
             integrated attitude + angular rate, and advances the attitude with
             Euler's rigid-body equation using the vessel's inertia tensor.
             This is the "after": it discovers trim, lift, and oscillation on
             its own.

Both predictors capture the vessel state once (position, velocity, attitude,
angular velocity, mass, inertia tensor) and then integrate offline; only aero
comes from the game. Gravity is inverse-square, mass is held constant (tweak
the heatshield ablator down so this is nearly true), and terrain is not
modelled (aim for ocean; impact is at --stop-altitude, default sea level).

Validation ladder
-----------------
  step 1  baseline predictor vs a craft that actually holds retrograde:
              python reentry_predictor.py run --mode baseline --hold retrograde
          This grades the integrator + frames + force endpoint with no attitude
          modelling in play. It should already land close.
  step 2+ 6-DOF vs an uncontrolled entry (trim-ballast or offset-release craft):
              python reentry_predictor.py run --mode both --hold none

Usage
-----
  # Offline self-test of the integrator, attitude dynamics and geometry
  # (numpy only, no game, no kRPC):
  python reentry_predictor.py selftest

  # Full validation flight: teleport onto a re-entry arc (TestingTools; falls
  # back to a deorbit burn), predict at the capture point, fly it, log, compare:
  python reentry_predictor.py run --mode baseline --hold retrograde --out step1

  # Predict only, from the craft's current state (no teleport, no flight):
  python reentry_predictor.py predict --mode both --out mypred

  # Re-analyse/re-plot an existing run (no game needed):
  python reentry_predictor.py plot --out step1

Craft requirements
------------------
  * Few parts (the sim endpoints iterate parts per call): pod + heatshield +
    parachute is ideal. Do NOT stage between capture and impact.
  * Ablator tweaked low so mass is ~constant; actual mass is logged so the
    residual error can be quantified.
  * SAS/RCS state is managed by --hold; parachutes are left alone (do not arm
    them if you want the log to reach the ground ballistically).
  * Aim the arc at ocean: impact is graded at --stop-altitude above sea level.

Frames and conventions
----------------------
  * All integration happens in the body's non_rotating_reference_frame; the
    endpoints subtract the rotating atmosphere internally.
  * Quaternions are (x, y, z, w), body->world, kRPC convention. Vessel body
    axes: x = right (pitch), y = forward/nose (roll), z = down (yaw).
  * Latitude/longitude and the planet's spin direction are calibrated
    empirically at capture (surface_position oracle points + a timed transform
    pair), so no handedness assumptions are baked in.
  * The prediction runs while the game keeps playing: it only needs the craft's
    part list to stay unchanged. If the coast to the atmosphere is shorter than
    the prediction wall time, re-run with a higher --predict-altitude.
"""

import argparse
import csv
import json
import math
import os
import sys
import time

import numpy as np

NOSE = np.array([0.0, 1.0, 0.0])  # vessel-frame forward/nose axis
ZERO3 = np.zeros(3)
Q_IDENTITY = np.array([0.0, 0.0, 0.0, 1.0])

PRED_COLUMNS = [
    "ut",
    "alt",
    "x",
    "y",
    "z",
    "vx",
    "vy",
    "vz",
    "qx",
    "qy",
    "qz",
    "qw",
    "wx",
    "wy",
    "wz",
    "aoa",
]
ACT_COLUMNS = [
    "ut",
    "alt",
    "lat",
    "lon",
    "speed",
    "qdyn",
    "mass",
    "x",
    "y",
    "z",
    "vx",
    "vy",
    "vz",
    "qx",
    "qy",
    "qz",
    "qw",
    "wx",
    "wy",
    "wz",
    # Live game-applied aero force (Flight.AerodynamicForce) and the
    # force endpoint evaluated at the same logged state, both in the
    # non-rotating frame. NaN when unavailable.
    "flx",
    "fly",
    "flz",
    "fsx",
    "fsy",
    "fsz",
    # Same pair for torque: Flight.AerodynamicTorque (live) and the torque
    # component of SimulateAerodynamicWrenchAt at the logged state. The
    # existing column names are retained for old-log compatibility.
    "tlx",
    "tly",
    "tlz",
    "tsx",
    "tsy",
    "tsz",
]


# ============================================================================
# Quaternion / geometry helpers (numpy, (x, y, z, w), body->world)
# ============================================================================
def q_mult(a, b):
    """Hamilton product a*b: rotate by b first, then a."""
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return np.array(
        [
            aw * bx + bw * ax + ay * bz - az * by,
            aw * by + bw * ay + az * bx - ax * bz,
            aw * bz + bw * az + ax * by - ay * bx,
            aw * bw - ax * bx - ay * by - az * bz,
        ]
    )


def q_conj(q):
    return np.array([-q[0], -q[1], -q[2], q[3]])


def q_normalize(q):
    return q / np.linalg.norm(q)


def q_rot(q, v):
    """Rotate vector v by quaternion q (body->world for an attitude q)."""
    qv = q[:3]
    t = 2.0 * np.cross(qv, v)
    return np.asarray(v) + q[3] * t + np.cross(qv, t)


def q_axis_angle(axis, angle):
    axis = np.asarray(axis, dtype=float)
    axis = axis / np.linalg.norm(axis)
    s = math.sin(angle / 2.0)
    return np.array([axis[0] * s, axis[1] * s, axis[2] * s, math.cos(angle / 2.0)])


def q_shortest_arc(a, b):
    """Smallest rotation taking unit vector a to unit vector b."""
    d = float(np.dot(a, b))
    if d > 1.0 - 1e-12:
        return Q_IDENTITY.copy()
    if d < -1.0 + 1e-9:
        # Antiparallel: rotate pi about any axis perpendicular to a.
        perp = np.cross(a, np.array([1.0, 0.0, 0.0]))
        if np.linalg.norm(perp) < 1e-6:
            perp = np.cross(a, np.array([0.0, 1.0, 0.0]))
        return q_axis_angle(perp, math.pi)
    axis = np.cross(a, b)
    q = np.array([axis[0], axis[1], axis[2], 1.0 + d])
    return q_normalize(q)


def rodrigues(v, axis, angle):
    """Rotate v about unit axis by angle (right-handed)."""
    c, s = math.cos(angle), math.sin(angle)
    return v * c + np.cross(axis, v) * s + axis * np.dot(axis, v) * (1.0 - c)


def retro_attitude(v_air, q_seed):
    """Attitude with the nose on -v_air (heatshield into the wind), changing
    roll as little as possible relative to q_seed."""
    speed = np.linalg.norm(v_air)
    if speed < 1e-6:
        return q_seed
    n = -v_air / speed
    nose = q_rot(q_seed, NOSE)
    return q_normalize(q_mult(q_shortest_arc(nose, n), q_seed))


def total_aoa_deg(q, r, v, w_planet):
    """Total angle of attack: angle between the nose and the airflow."""
    v_air = v - np.cross(w_planet, r)
    speed = np.linalg.norm(v_air)
    if speed < 1e-3:
        return 0.0
    nose = q_rot(q, NOSE)
    c = float(np.dot(nose, -v_air / speed))
    return math.degrees(math.acos(max(-1.0, min(1.0, c))))


def gc_distance(p1, p2, radius):
    """Great-circle distance between two points (any radius), in meters."""
    u1 = p1 / np.linalg.norm(p1)
    u2 = p2 / np.linalg.norm(p2)
    ang = math.atan2(float(np.linalg.norm(np.cross(u1, u2))), float(np.dot(u1, u2)))
    return ang * radius


# ============================================================================
# Fixed-frame mapping (offline, from meta)
# ============================================================================
def derotate(p, t, meta):
    """Map a non-rotating-frame position at time t to the body-fixed frame
    snapshotted at capture time ut0 (planet spin removed)."""
    w_vec = np.asarray(meta["w_vec"])
    w_mag = np.linalg.norm(w_vec)
    if w_mag < 1e-12:
        return np.asarray(p, dtype=float)
    axis = w_vec / w_mag
    theta = meta["spin_sign"] * w_mag * (t - meta["ut0"])
    return rodrigues(np.asarray(p, dtype=float), axis, -theta)


def latlon_deg(p_fixed, meta):
    """Latitude/longitude of a body-fixed (capture-snapshot) position, using
    the empirically calibrated basis E = [e0, e90, epole] (rows)."""
    E = np.asarray(meta["E"])
    u = p_fixed / np.linalg.norm(p_fixed)
    lat = math.degrees(math.asin(max(-1.0, min(1.0, float(np.dot(u, E[2]))))))
    lon = math.degrees(math.atan2(float(np.dot(u, E[1])), float(np.dot(u, E[0]))))
    return lat, lon


# ============================================================================
# Aerodynamic models (both share the integrator)
# ============================================================================
class RpcAero:
    """Aero forces/torques from the live game via the simulation endpoints."""

    def __init__(self, conn, meta, want_torque):
        sc = conn.space_center
        self.body = sc.bodies[meta["body"]]
        self.vessel = sc.active_vessel
        self.flight = self.vessel.flight(self.body.non_rotating_reference_frame)
        self.radius = meta["radius"]
        self.atmo_top = meta["atmosphere_depth"]
        self.want_torque = want_torque
        self.calls = 0

    def __call__(self, r, v, q, w, ut):
        if np.linalg.norm(r) - self.radius >= self.atmo_top:
            return ZERO3, ZERO3
        if self.want_torque:
            force, torque = self.flight.simulate_aerodynamic_wrench_at(
                self.body, tuple(r), tuple(v), tuple(q), tuple(w), ut
            )
            self.calls += 1
            return np.array(force), np.array(torque)
        force = self.flight.simulate_aerodynamic_force_at(
            self.body, tuple(r), tuple(v), tuple(q)
        )
        self.calls += 1
        return np.array(force), ZERO3


class SyntheticAero:
    """Simple drag-only model for the offline selftest: exponential atmosphere,
    F = -1/2 rho Cd A |v_air| v_air, no torque."""

    def __init__(self, radius, atmo_top, w_planet, rho0=1.2, scale_h=5600.0, cd_a=2.0):
        self.radius = radius
        self.atmo_top = atmo_top
        self.w_planet = np.asarray(w_planet, dtype=float)
        self.rho0 = rho0
        self.scale_h = scale_h
        self.cd_a = cd_a
        self.calls = 0

    def __call__(self, r, v, q, w, ut):
        alt = np.linalg.norm(r) - self.radius
        if alt >= self.atmo_top:
            return ZERO3, ZERO3
        self.calls += 1
        rho = self.rho0 * math.exp(-max(alt, 0.0) / self.scale_h)
        v_air = v - np.cross(self.w_planet, r)
        force = -0.5 * rho * self.cd_a * np.linalg.norm(v_air) * v_air
        return force, ZERO3


# ============================================================================
# The integrator (shared by baseline and 6dof; aero is injected)
# ============================================================================
def _advance(r, v, q, w, k, h, sixdof):
    """One RK stage advance of the state by h along derivative tuple k."""
    r2 = r + h * k[0]
    v2 = v + h * k[1]
    q2 = q_normalize(q + h * k[2]) if sixdof else q
    w2 = w + h * k[3] if sixdof else w
    return r2, v2, q2, w2


def integrate(
    phys,
    aero,
    mode,
    state0,
    dt_atmo=0.1,
    dt_vac=2.0,
    record_dt=0.5,
    stop_alt=0.0,
    max_time=2000.0,
    progress=None,
    hold_retro_vacuum=False,
):
    """Integrate the entry with classic RK4. Returns a list of PRED_COLUMNS
    rows, ending with a row interpolated to stop_alt if it was reached.

    RK4 (not RK2) is load-bearing for the 6-DOF mode: the capsule pitch
    oscillation stiffens as dynamic pressure grows, and midpoint RK2 pumps
    energy into an oscillator (growth ~ (omega*dt)^4 per step) -- at entry
    conditions it stalls the predicted AoA decay and eventually tumbles the
    prediction. RK4 damps ever so slightly instead and is stable out to
    omega*dt ~ 2.8, i.e. oscillation periods down to ~0.25 s at dt = 0.1.
    The selftest pins this with a damped-pendulum envelope check.

    phys:   dict with mu, radius, atmo_depth, mass, inertia (3x3, body frame),
            w_vec (planet angular velocity, non-rotating frame).
    aero:   callable (r, v, q, w, ut) -> (force_N, torque_Nm), non-rotating frame.
    mode:   "baseline" (attitude snapped to retrograde, force only) or
            "6dof" (attitude integrated from the torque).
    state0: dict with t, r, v, q, w (non-rotating frame).
    """
    mu = phys["mu"]
    radius = phys["radius"]
    atmo_top = phys["atmo_depth"]
    mass = phys["mass"]
    inertia = np.asarray(phys["inertia"], dtype=float).reshape(3, 3)
    w_planet = np.asarray(phys["w_vec"], dtype=float)
    rate_damp = phys.get("rate_damp", 0.0)
    wheel_torque = np.asarray(phys.get("wheel_torque", [0.0, 0.0, 0.0]), dtype=float)
    sixdof = mode != "baseline"

    r = np.array(state0["r"], dtype=float)
    v = np.array(state0["v"], dtype=float)
    q = q_normalize(np.array(state0["q"], dtype=float))
    w = np.array(state0["w"], dtype=float)
    t = float(state0["t"])
    seed = [q.copy()]  # roll-continuity seed for the baseline retro snap
    if not sixdof:
        # The baseline assumes retrograde from the very start; snap the initial
        # attitude too so the recorded rows reflect that assumption.
        q = retro_attitude(v - np.cross(w_planet, r), q)
        seed[0] = q.copy()

    def deriv(t_, r_, v_, q_, w_):
        rn = np.linalg.norm(r_)
        g = -mu / rn**3 * r_
        if not sixdof:
            v_air = v_ - np.cross(w_planet, r_)
            q_used = retro_attitude(v_air, seed[0])
            seed[0] = q_used
            force, _ = aero(r_, v_, q_used, ZERO3, t_)
            return v_, g + force / mass, None, None, q_used
        force, torque = aero(r_, v_, q_, w_, t_)
        qc = q_conj(q_)
        w_body = q_rot(qc, w_)
        tau_body = q_rot(qc, torque)
        if rate_damp > 0.0 and rn - radius < atmo_top:
            # Reaction-wheel rate damping, mirroring the control law the
            # flight loop applies: input = clamp(K * omega) per body axis,
            # torque = -wheel_torque * input. Gated to inside the atmosphere,
            # where the real control loop runs (the coast is SAS-held with
            # ~zero rates) -- this also keeps the stiff damping term on the
            # small atmospheric time step, where it is numerically stable.
            tau_body = tau_body - wheel_torque * np.clip(rate_damp * w_body, -1.0, 1.0)
        alpha_body = np.linalg.solve(
            inertia, tau_body - np.cross(w_body, inertia.dot(w_body))
        )
        w_dot = q_rot(q_, alpha_body)
        q_dot = 0.5 * q_mult(np.array([w_[0], w_[1], w_[2], 0.0]), q_)
        return v_, g + force / mass, q_dot, w_dot, q_

    def make_row(t_, r_, v_, q_, w_):
        alt_ = np.linalg.norm(r_) - radius
        aoa = total_aoa_deg(q_, r_, v_, w_planet)
        return [t_, alt_, *r_, *v_, *q_, *w_, aoa]

    rows = [make_row(t, r, v, q, w)]
    t0, alt0 = t, rows[0][1]
    next_record = t + record_dt
    wall0, last_report = time.time(), time.time()

    while True:
        alt = np.linalg.norm(r) - radius
        if alt <= stop_alt:
            break
        if t - t0 > max_time:
            print(
                f"  prediction stopped: exceeded max time ({max_time:.0f} s) "
                f"at altitude {alt:.0f} m"
            )
            break
        if alt > alt0 + 200000.0:
            print("  prediction stopped: craft is escaping (not a re-entry arc)")
            break

        dt = dt_vac if alt > atmo_top + 2000.0 else dt_atmo
        prev = (t, r.copy(), v.copy(), q.copy(), w.copy())

        k1 = deriv(t, r, v, q, w)
        k2 = deriv(t + 0.5 * dt, *_advance(r, v, q, w, k1, 0.5 * dt, sixdof))
        k3 = deriv(t + 0.5 * dt, *_advance(r, v, q, w, k2, 0.5 * dt, sixdof))
        k4 = deriv(t + dt, *_advance(r, v, q, w, k3, dt, sixdof))
        rd = (k1[0] + 2.0 * k2[0] + 2.0 * k3[0] + k4[0]) / 6.0
        vd = (k1[1] + 2.0 * k2[1] + 2.0 * k3[1] + k4[1]) / 6.0
        r = r + dt * rd
        v = v + dt * vd
        if sixdof:
            qd = (k1[2] + 2.0 * k2[2] + 2.0 * k3[2] + k4[2]) / 6.0
            wd = (k1[3] + 2.0 * k2[3] + 2.0 * k3[3] + k4[3]) / 6.0
            q = q_normalize(q + dt * qd)
            w = w + dt * wd
            if hold_retro_vacuum and np.linalg.norm(r) - radius > atmo_top:
                # Mirror a retro-release flight: SAS tracks the (rotating)
                # surface-retrograde marker through the vacuum coast and lets
                # go at the atmosphere interface. Without this the frozen
                # inertial attitude drifts ~10 deg off retro during the coast.
                q = retro_attitude(v - np.cross(w_planet, r), q)
                w = np.zeros(3)
        else:
            q = k4[4]  # the retro-snapped attitude from the last stage
        t += dt

        if t >= next_record:
            rows.append(make_row(t, r, v, q, w))
            next_record += record_dt

        now = time.time()
        if progress and now - last_report > 5.0:
            calls = getattr(aero, "calls", 0)
            rate = calls / max(now - wall0, 1e-9)
            print(
                f"  [{progress}] t=+{t - t0:6.0f} s  alt={alt / 1000.0:6.1f} km  "
                f"v={np.linalg.norm(v):5.0f} m/s  rpc={calls} ({rate:.0f}/s)"
            )
            last_report = now

        # Interpolate the final row exactly onto stop_alt.
        alt_new = np.linalg.norm(r) - radius
        if alt_new <= stop_alt:
            tp, rp, vp, qp, wp = prev
            altp = np.linalg.norm(rp) - radius
            f = (altp - stop_alt) / max(altp - alt_new, 1e-9)
            ri = rp + f * (r - rp)
            vi = vp + f * (v - vp)
            qi = q_normalize(qp + f * (q - qp))
            wi = wp + f * (w - wp)
            ti = tp + f * (t - tp)
            rows.append(make_row(ti, ri, vi, qi, wi))
            break

    return rows


# ============================================================================
# Live-game plumbing: connect, capture, hold, sample
# ============================================================================
def _connect(name):
    import krpc  # local import so plot/selftest run without kRPC installed

    return krpc.connect(name=name)


def capture_state(conn, spin_wait=5.0):
    """Snapshot everything the predictors and the offline analysis need.

    Includes two empirical calibrations (no handedness assumptions):
      * the lat/lon basis E from body.surface_position oracle points;
      * the planet spin direction sign from a timed pair of transforms.
    """
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    nonrot = body.non_rotating_reference_frame
    brf = body.reference_frame
    flight = vessel.flight(brf)

    def basis_dir(lat, lon):
        p = np.array(body.surface_position(lat, lon, brf))
        return p / np.linalg.norm(p)

    e0_brf, e90_brf, epole_brf = (basis_dir(0, 0), basis_dir(0, 90), basis_dir(90, 0))

    # Spin-sign calibration: watch a body-fixed direction move in the
    # non-rotating frame over a few seconds.
    ut_a = sc.ut
    e0_a = np.array(sc.transform_direction(tuple(e0_brf), brf, nonrot))
    time.sleep(spin_wait)
    ut_b = sc.ut
    e0_b = np.array(sc.transform_direction(tuple(e0_brf), brf, nonrot))
    w_vec = np.array(body.angular_velocity(nonrot))
    w_mag = np.linalg.norm(w_vec)
    spin_sign = 1.0
    if w_mag > 1e-12 and ut_b > ut_a:
        axis = w_vec / w_mag
        theta = w_mag * (ut_b - ut_a)
        err_plus = np.linalg.norm(rodrigues(e0_a, axis, theta) - e0_b)
        err_minus = np.linalg.norm(rodrigues(e0_a, axis, -theta) - e0_b)
        spin_sign = 1.0 if err_plus <= err_minus else -1.0
        if min(err_plus, err_minus) > 0.5 * abs(err_plus - err_minus):
            print(
                "WARNING: spin-sign calibration is marginal "
                f"(+:{err_plus:.2e} -:{err_minus:.2e}); "
                "was the game paused or under warp during capture?"
            )

    # The state snapshot proper (right after the calibration wait). The game
    # keeps ticking between RPCs, so bracket the position and velocity reads
    # with ut reads and back-propagate the velocity to the position's epoch
    # (gravity-only, first order: fine in vacuum, ~exact over a few ticks).
    # An uncorrected few-tick skew costs a few hundred meters of along-track
    # miss over a full entry.
    mu = body.gravitational_parameter
    ut_a = sc.ut
    r0 = np.array(vessel.position(nonrot))
    ut_b = sc.ut
    v0 = np.array(vessel.velocity(nonrot))
    ut_c = sc.ut
    ut0 = 0.5 * (ut_a + ut_b)  # epoch of the position read
    t_v = 0.5 * (ut_b + ut_c)  # epoch of the velocity read
    g0 = -mu / np.linalg.norm(r0) ** 3 * r0
    v0 = v0 - g0 * (t_v - ut0)
    q0 = np.array(vessel.rotation(nonrot))
    w0 = np.array(vessel.angular_velocity(nonrot))
    mass = vessel.mass
    inertia = list(vessel.inertia_tensor)
    try:
        wheel_pos, _ = vessel.available_reaction_wheel_torque
        wheel_torque = [abs(x) for x in wheel_pos]
    except Exception:
        wheel_torque = [0.0, 0.0, 0.0]

    # Lat/lon basis expressed in the non-rotating frame at ut0.
    E = np.vstack(
        [
            np.array(sc.transform_direction(tuple(e), brf, nonrot))
            for e in (e0_brf, e90_brf, epole_brf)
        ]
    )

    meta = {
        "version": 1,
        "body": body.name,
        "mu": mu,
        "radius": body.equatorial_radius,
        "atmosphere_depth": body.atmosphere_depth,
        "mass": mass,
        "inertia": inertia,
        "wheel_torque": wheel_torque,
        "w_vec": list(w_vec),
        "spin_sign": spin_sign,
        "E": E.tolist(),
        "ut0": ut0,
        "r0": list(r0),
        "v0": list(v0),
        "q0": list(q0),
        "w0": list(w0),
        "lat0": flight.latitude,
        "lon0": flight.longitude,
        "alt0": flight.mean_altitude,
        "craft": vessel.name,
        "files": {},
    }

    # Sanity check: our offline lat/lon math must reproduce the game's.
    lat_c, lon_c = latlon_deg(derotate(r0, ut0, meta), meta)
    dlat = abs(lat_c - meta["lat0"])
    dlon = abs((lon_c - meta["lon0"] + 180.0) % 360.0 - 180.0)
    if dlat > 0.05 or dlon > 0.05:
        print(
            f"WARNING: lat/lon calibration mismatch (computed {lat_c:.3f}, "
            f"{lon_c:.3f} vs game {meta['lat0']:.3f}, {meta['lon0']:.3f})"
        )
    else:
        print(
            f"Capture: {meta['craft']} at {meta['alt0'] / 1000.0:.1f} km, "
            f"lat {lat_c:.3f} lon {lon_c:.3f}, mass {mass:.0f} kg "
            f"(lat/lon calibration OK, spin sign {spin_sign:+.0f})"
        )
    return meta


def _release_keep_wheels(vessel):
    """Uncontrolled except reaction wheels stay enabled (for rate damping)."""
    control = vessel.control
    control.throttle = 0.0
    control.sas = False
    control.rcs = False
    try:
        vessel.auto_pilot.disengage()
    except Exception:
        pass
    print(
        "Released: SAS/RCS/thrust off; reaction wheels LEFT ENABLED for "
        "rate damping."
    )


def setup_hold(conn, hold, keep_wheels=False):
    """Put the craft's control state in the configuration the flight assumes."""
    sc = conn.space_center
    vessel = sc.active_vessel
    control = vessel.control
    control.throttle = 0.0
    control.rcs = False
    if hold == "retrograde":
        control.sas = True
        time.sleep(0.2)
        try:
            control.speed_mode = sc.SpeedMode.surface
        except Exception:
            print("  (could not set surface speed mode; check the navball)")
        try:
            control.sas_mode = sc.SASMode.retrograde
        except Exception:
            print("  (retrograde SAS mode unavailable; point retrograde manually)")
        print("Hold: SAS surface-retrograde engaged; letting it settle...")
        time.sleep(10.0)
    else:
        if keep_wheels:
            _release_keep_wheels(vessel)
        else:
            from compare_aero_torque import _go_uncontrolled

            _go_uncontrolled(vessel)
        time.sleep(2.0)


def wait_until_altitude_below(conn, target_alt, label):
    vessel = conn.space_center.active_vessel
    flight = vessel.flight(vessel.orbit.body.reference_frame)
    if flight.mean_altitude > target_alt:
        print(f"Coasting to {label} ({target_alt / 1000.0:.0f} km)...")
        while flight.mean_altitude > target_alt:
            time.sleep(1.0)


def run_prediction(conn, meta, mode, args):
    """Run one predictor mode against the live endpoints; returns rows."""
    want_torque = mode != "baseline"
    aero = RpcAero(conn, meta, want_torque)
    phys = {k: meta[k] for k in ("mu", "radius", "mass", "inertia", "w_vec")}
    phys["atmo_depth"] = meta["atmosphere_depth"]
    phys["rate_damp"] = meta.get("rate_damp", 0.0)
    phys["wheel_torque"] = meta.get("wheel_torque", [0.0, 0.0, 0.0])
    state0 = {
        "t": meta["ut0"],
        "r": meta["r0"],
        "v": meta["v0"],
        "q": meta["q0"],
        "w": meta["w0"],
    }
    dt = args.dt_baseline if mode == "baseline" else args.dt_6dof
    if phys["rate_damp"] > 0.0 and mode != "baseline":
        diag = np.asarray(meta["inertia"], dtype=float).reshape(3, 3).diagonal()
        lam = (
            phys["rate_damp"]
            * max(phys["wheel_torque"])
            / max(float(np.min(diag)), 1e-9)
        )
        if lam * dt > 1.0:
            print(
                f"  WARNING: damping rate x dt = {lam * dt:.2f} at dt={dt} s "
                "-- numerically stiff (RK4 stability ends ~2.8, accuracy "
                "wants <1). Reduce --rate-damp or --dt-6dof."
            )
    print(f"Predicting ({mode}, dt={dt} s)...")
    wall = time.time()
    rows = integrate(
        phys,
        aero,
        mode,
        state0,
        dt_atmo=dt,
        dt_vac=args.dt_vacuum,
        record_dt=args.record,
        stop_alt=args.stop_altitude,
        max_time=args.max_time,
        progress=mode,
        hold_retro_vacuum=meta.get("hold") == "retro-release",
    )
    last = rows[-1]
    p_fixed = derotate(np.array(last[2:5]), last[0], meta)
    lat, lon = latlon_deg(p_fixed, meta)
    p0_fixed = derotate(np.array(meta["r0"]), meta["ut0"], meta)
    downrange = gc_distance(p0_fixed, p_fixed, meta["radius"])
    print(
        f"  {mode}: impact lat {lat:.3f} lon {lon:.3f}, "
        f"downrange {downrange / 1000.0:.1f} km, "
        f"flight time {last[0] - meta['ut0']:.0f} s, "
        f"{aero.calls} aero RPCs in {time.time() - wall:.0f} s wall"
    )
    return rows


def sample_flight(conn, meta, rate, min_altitude):
    """Log the actual flight until it splashes/lands or drops below
    min_altitude. Returns ACT_COLUMNS rows."""
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    nonrot = body.non_rotating_reference_frame
    flight = vessel.flight(body.reference_frame)
    flight_nr = vessel.flight(nonrot)
    nan3 = (float("nan"),) * 3
    period = 1.0 / rate
    rows = []
    frozen = 0
    rate_damp = meta.get("rate_damp", 0.0)
    control = vessel.control
    if rate_damp > 0.0:
        print(
            f"Rate damping active: control input = clamp({rate_damp} * "
            "body rate) per axis (reaction wheels must be enabled). If the "
            "oscillation GROWS within the first cycles, abort: sign "
            "convention mismatch."
        )
    print(
        f"Logging at {rate:.0f} Hz until altitude < {min_altitude:.0f} m "
        "or splashdown. Ctrl-C to stop early."
    )
    try:
        while True:
            alt = flight.mean_altitude
            # A destroyed vessel keeps reporting the exact same state; a live
            # one in flight never repeats the altitude bit-for-bit.
            frozen = frozen + 1 if (rows and alt == rows[-1][1]) else 0
            if frozen >= 5:
                print(
                    "State frozen for 5 samples -- vessel destroyed "
                    "(terrain impact?); stopping log and dropping the "
                    "frozen rows."
                )
                rows = rows[: len(rows) - frozen + 1]
                break
            sample_ut = sc.ut
            pos = vessel.position(nonrot)
            vel = vessel.velocity(nonrot)
            rot = vessel.rotation(nonrot)
            w_nr = vessel.angular_velocity(nonrot)
            if rate_damp > 0.0:
                # Same law the prediction models: input = clamp(K * omega)
                # per body axis (pitch<->x, roll<->y, yaw<->z; KSP's positive
                # inputs torque about the negative axes, so +K damps).
                wb = sc.transform_direction(w_nr, nonrot, vessel.reference_frame)
                control.pitch = max(-1.0, min(1.0, rate_damp * wb[0]))
                control.roll = max(-1.0, min(1.0, rate_damp * wb[1]))
                control.yaw = max(-1.0, min(1.0, rate_damp * wb[2]))
            # Live game-applied force/torque and one simulated wrench at the
            # same state. Their difference isolates model error from telemetry
            # noise without making separate force and torque simulation RPCs.
            try:
                f_live = flight_nr.aerodynamic_force
            except Exception:
                f_live = nan3
            try:
                t_live = flight_nr.aerodynamic_torque
            except Exception:
                t_live = nan3
            try:
                f_sim, t_sim = flight_nr.simulate_aerodynamic_wrench_at(
                    body, pos, vel, rot, w_nr, sample_ut
                )
            except Exception:
                f_sim = nan3
                t_sim = nan3
            rows.append(
                [
                    sample_ut,
                    alt,
                    flight.latitude,
                    flight.longitude,
                    flight.speed,
                    flight.dynamic_pressure,
                    vessel.mass,
                    *pos,
                    *vel,
                    *rot,
                    *w_nr,
                    *f_live,
                    *f_sim,
                    *t_live,
                    *t_sim,
                ]
            )
            if alt < min_altitude:
                print("Reached minimum altitude; stopping log.")
                break
            try:
                situation = vessel.situation
                if situation in (
                    sc.VesselSituation.splashed,
                    sc.VesselSituation.landed,
                ):
                    print(f"Vessel {situation.name}; stopping log.")
                    break
            except Exception:
                pass
            time.sleep(period)
    except KeyboardInterrupt:
        print("\nInterrupted; keeping what we have.")
    return rows


def write_csv(path, columns, rows):
    with open(path, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(columns)
        w.writerows(rows)
    print(f"Wrote {len(rows)} rows to {path}")


# ============================================================================
# Offline analysis + plot
# ============================================================================
def load_traj(path):
    data = np.genfromtxt(path, delimiter=",", names=True)
    return {name: np.atleast_1d(data[name]).astype(float) for name in data.dtype.names}


def _fixed_positions(traj, meta):
    pos = np.column_stack([traj["x"], traj["y"], traj["z"]])
    return np.array(
        [derotate(pos[i], traj["ut"][i], meta) for i in range(len(traj["ut"]))]
    )


def _crossing(traj, fixed, alt_target):
    """First descending crossing of alt_target: (fixed position, ut), or None."""
    alt = traj["alt"]
    for i in range(len(alt) - 1):
        if alt[i] >= alt_target > alt[i + 1]:
            f = (alt[i] - alt_target) / max(alt[i] - alt[i + 1], 1e-9)
            p = fixed[i] + f * (fixed[i + 1] - fixed[i])
            t = traj["ut"][i] + f * (traj["ut"][i + 1] - traj["ut"][i])
            return p, t
    return None


def _rolling_peak(t, x, window=6.0):
    """Rolling max of x over a sliding time window (the oscillation envelope),
    evaluated every window/2 seconds."""
    if len(t) == 0:
        return t, x
    centers = np.arange(t[0], t[-1] + 1e-9, window / 2.0)
    out_t, out_x = [], []
    for c in centers:
        m = (t >= c - window / 2.0) & (t <= c + window / 2.0)
        if m.any():
            out_t.append(c)
            out_x.append(float(np.max(x[m])))
    return np.array(out_t), np.array(out_x)


def _traj_aoa(traj, meta):
    w_planet = np.asarray(meta["w_vec"])
    out = np.empty(len(traj["ut"]))
    for i in range(len(out)):
        q = np.array([traj["qx"][i], traj["qy"][i], traj["qz"][i], traj["qw"][i]])
        r = np.array([traj["x"][i], traj["y"][i], traj["z"][i]])
        v = np.array([traj["vx"][i], traj["vy"][i], traj["vz"][i]])
        out[i] = total_aoa_deg(q_normalize(q), r, v, w_planet)
    return out


def analyze(prefix, png=None, checkpoints_km=None):
    """Compare predictions against the actual flight; write a plot; print and
    return the miss table."""
    with open(prefix + "_meta.json") as f:
        meta = json.load(f)
    radius = meta["radius"]

    trajs = {}
    for key in ("baseline", "6dof", "actual"):
        path = meta["files"].get(key)
        if path and os.path.exists(path):
            trajs[key] = load_traj(path)
    if not trajs:
        sys.exit(f"No trajectory files found for prefix '{prefix}'")

    # Trim a frozen post-destruction tail from the actual log (a destroyed
    # vessel keeps reporting bit-identical state).
    if "actual" in trajs:
        alt = trajs["actual"]["alt"]
        mass = trajs["actual"].get("mass")
        last = len(alt) - 1
        while last > 0 and alt[last] == alt[last - 1]:
            last -= 1
        while last > 0 and mass is not None and mass[last] == 0.0:
            last -= 1  # a destroyed vessel reports zero mass
        if last < len(alt) - 1:
            print(
                f"Trimmed {len(alt) - 1 - last} frozen post-impact rows "
                f"from the actual log (impact at {alt[last]:.0f} m ASL)"
            )
            trajs["actual"] = {k: v[: last + 1] for k, v in trajs["actual"].items()}
    fixed = {k: _fixed_positions(t, meta) for k, t in trajs.items()}

    # Cross-check the offline geometry against the game's logged lat/lon.
    if "actual" in trajs:
        act = trajs["actual"]
        errs = []
        for i in range(0, len(act["ut"]), max(1, len(act["ut"]) // 50)):
            lat, lon = latlon_deg(fixed["actual"][i], meta)
            dlon = (lon - act["lon"][i] + 180.0) % 360.0 - 180.0
            errs.append(math.hypot(lat - act["lat"][i], dlon))
        med = float(np.median(errs))
        print(
            f"Geometry cross-check: median |computed - logged| lat/lon "
            f"= {med:.4f} deg {'(OK)' if med < 0.02 else '(SUSPICIOUS)'}"
        )

    if checkpoints_km is None:
        checkpoints_km = [40, 30, 20, 10, 5, 2]
    checkpoints = [c * 1000.0 for c in checkpoints_km]
    stop_alt = meta.get("stop_altitude", 0.0)
    checkpoints.append(stop_alt)  # impact row

    # Miss table: prediction vs actual at each checkpoint altitude.
    table = []
    pred_keys = [k for k in ("baseline", "6dof") if k in trajs]
    if "actual" in trajs:
        for alt_c in checkpoints:
            act_x = _crossing(trajs["actual"], fixed["actual"], alt_c)
            entry = {"alt": alt_c}
            for k in pred_keys:
                pred_x = _crossing(trajs[k], fixed[k], alt_c)
                if act_x and pred_x:
                    entry[k] = (
                        gc_distance(act_x[0], pred_x[0], radius + alt_c),
                        pred_x[1] - act_x[1],
                    )
            if len(entry) > 1:
                table.append(entry)
        label = {stop_alt: "impact"}
        print("\nMiss distance vs actual flight (horizontal, body-fixed):")
        header = f"{'checkpoint':>12}"
        for k in pred_keys:
            header += f"  {k + ' miss':>14} {'dt':>8}"
        print(header)
        for entry in table:
            name = label.get(entry["alt"], f"{entry['alt'] / 1000.0:.0f} km")
            line = f"{name:>12}"
            for k in pred_keys:
                if k in entry:
                    line += (
                        f"  {entry[k][0] / 1000.0:12.2f} km" f" {entry[k][1]:+7.1f} s"
                    )
                else:
                    line += f"  {'--':>14} {'--':>8}"
            print(line)
        if "mass" in trajs.get("actual", {}):
            m = trajs["actual"]["mass"]
            print(
                f"Mass over the flight: {m[0]:.0f} -> {m[-1]:.0f} kg "
                f"({100.0 * (m[0] - m[-1]) / max(m[0], 1e-9):.1f}% lost; "
                "the predictor assumes constant mass)"
            )

    _plot(prefix, png, meta, trajs, fixed, table, pred_keys)
    return table


def _plot(prefix, png, meta, trajs, fixed, table, pred_keys):
    import matplotlib

    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    radius = meta["radius"]
    p0_fixed = derotate(np.array(meta["r0"]), meta["ut0"], meta)
    styles = {
        "actual": dict(color="#111111", lw=2.0),
        "baseline": dict(color="#d1495b", lw=1.6, ls="--"),
        "6dof": dict(color="#3a7ca5", lw=1.6),
    }
    nice = {
        "actual": "actual flight",
        "baseline": "baseline (3-DOF retro)",
        "6dof": "6-DOF (force + torque)",
    }

    fig = plt.figure(figsize=(13, 10))
    gs = fig.add_gridspec(3, 2, height_ratios=[1.3, 1.0, 1.0], hspace=0.42, wspace=0.26)

    # (1) Ground track.
    ax0 = fig.add_subplot(gs[0, :])
    for k, traj in trajs.items():
        ll = np.array([latlon_deg(p, meta) for p in fixed[k]])
        # Break the line where the track wraps across the +/-180 meridian.
        wrap = np.nonzero(np.abs(np.diff(ll[:, 1])) > 180.0)[0] + 1
        lon = np.insert(ll[:, 1], wrap, np.nan)
        lat = np.insert(ll[:, 0], wrap, np.nan)
        ax0.plot(lon, lat, label=nice[k], **styles[k])
        ax0.plot(ll[-1, 1], ll[-1, 0], "o", color=styles[k]["color"], ms=7)
    lat0, lon0 = latlon_deg(p0_fixed, meta)
    ax0.plot(lon0, lat0, "k^", ms=9, label="capture point")
    ax0.set_xlabel("longitude (deg)")
    ax0.set_ylabel("latitude (deg)")
    ax0.set_title(
        f"Ground track -- {meta['craft']} at {meta['body']} "
        "(markers = final logged/predicted point)"
    )
    ax0.legend(loc="best", fontsize=9)
    ax0.grid(alpha=0.3)

    # (2) Altitude vs downrange.
    ax1 = fig.add_subplot(gs[1, 0])
    for k, traj in trajs.items():
        dr = np.array([gc_distance(p0_fixed, p, radius) for p in fixed[k]])
        ax1.plot(dr / 1000.0, traj["alt"] / 1000.0, **styles[k])
    ax1.set_xlabel("downrange from capture (km)")
    ax1.set_ylabel("altitude (km)")
    ax1.set_title("Altitude vs downrange")
    ax1.grid(alpha=0.3)

    # (3) Altitude vs time.
    ax2 = fig.add_subplot(gs[1, 1])
    for k, traj in trajs.items():
        ax2.plot(traj["ut"] - meta["ut0"], traj["alt"] / 1000.0, **styles[k])
    ax2.set_xlabel("time since capture (s)")
    ax2.set_ylabel("altitude (km)")
    ax2.set_title("Altitude vs time")
    ax2.grid(alpha=0.3)

    # (4) Total angle of attack vs time. The oscillating traces overpaint each
    # other, so draw them faint and put a solid rolling-peak envelope for each
    # on top -- the envelope decay is the quantity being graded anyway.
    ax3 = fig.add_subplot(gs[2, 0])
    for k, traj in trajs.items():
        if "qx" not in traj:
            continue
        t_rel = traj["ut"] - meta["ut0"]
        aoa = _traj_aoa(traj, meta)
        if k == "baseline":  # flat ~0 by construction; no envelope needed
            ax3.plot(t_rel, aoa, **styles[k])
            continue
        raw = dict(styles[k], lw=0.7, alpha=0.3)
        raw.pop("ls", None)
        ax3.plot(t_rel, aoa, zorder=2 if k == "actual" else 3, **raw)
        env_t, env = _rolling_peak(t_rel, aoa)
        ax3.plot(env_t, env, zorder=4 if k == "actual" else 5, **styles[k])
    ax3.set_xlabel("time since capture (s)")
    ax3.set_ylabel("total AoA (deg)")
    ax3.set_title("Angle of attack (faint = raw, solid = peak envelope)")
    ax3.grid(alpha=0.3)

    # (5) Miss-table text.
    ax4 = fig.add_subplot(gs[2, 1])
    ax4.axis("off")
    lines = ["Horizontal miss vs actual flight", ""]
    if table:
        stop_alt = meta.get("stop_altitude", 0.0)
        head = f"{'checkpt':>8}"
        for k in pred_keys:
            head += f" {k:>14}"
        lines.append(head)
        for entry in table:
            name = (
                "impact"
                if entry["alt"] == stop_alt
                else f"{entry['alt'] / 1000.0:.0f} km"
            )
            line = f"{name:>8}"
            for k in pred_keys:
                line += (
                    f" {entry[k][0] / 1000.0:11.2f} km"
                    if k in entry
                    else f" {'--':>14}"
                )
            lines.append(line)
    else:
        lines.append("(no actual flight logged; predictions only)")
    ax4.text(
        0.0,
        1.0,
        "\n".join(lines),
        va="top",
        ha="left",
        family="monospace",
        fontsize=10,
        transform=ax4.transAxes,
    )

    fig.suptitle(
        "Re-entry landing prediction: baseline (force only) vs "
        "6-DOF (force + torque, issue #914)",
        fontsize=14,
        y=0.98,
    )
    png = png or (prefix + ".png")
    fig.savefig(png, dpi=130, bbox_inches="tight")
    print(f"Wrote plot to {png}")


# ============================================================================
# Commands
# ============================================================================
def cmd_predict(conn, args, meta=None):
    """Capture (unless given) + run the requested predictor modes."""
    if meta is None:
        meta = capture_state(conn)
    if getattr(args, "rate_damp", 0.0) > 0.0:
        meta["rate_damp"] = args.rate_damp
    elif "rate_damp" not in meta:
        meta["rate_damp"] = 0.0  # replays keep the value they were flown with
    hold = getattr(args, "hold", None)
    if hold is not None:
        meta["hold"] = hold  # needed by the predictor; replays keep theirs
    meta["stop_altitude"] = args.stop_altitude
    # Added in the second-pass log generation. Readers intentionally use
    # get()/ignore unknown keys so version-1 logs without this field replay
    # and plot unchanged.
    meta["aero_api"] = "wrench"
    meta["settings"] = {
        "mode": args.mode,
        "dt_baseline": args.dt_baseline,
        "dt_6dof": args.dt_6dof,
        "dt_vacuum": args.dt_vacuum,
        "record": args.record,
        "max_time": args.max_time,
    }
    modes = ["baseline", "6dof"] if args.mode == "both" else [args.mode]
    for mode in modes:
        rows = run_prediction(conn, meta, mode, args)
        path = f"{args.out}_{mode}.csv"
        write_csv(path, PRED_COLUMNS, rows)
        meta["files"][mode] = path
    with open(args.out + "_meta.json", "w") as f:
        json.dump(meta, f, indent=1)
    print(f"Wrote {args.out}_meta.json")
    return meta


def cmd_run(conn, args):
    from compare_aero_torque import _place_on_reentry, _deorbit

    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    predict_alt = args.predict_altitude
    if predict_alt is None:
        predict_alt = body.atmosphere_depth + 15000.0

    if not args.no_teleport:
        if hasattr(conn, "testing_tools"):
            _place_on_reentry(conn, vessel, args.periapsis, args.apoapsis, predict_alt)
        else:
            print(
                "TestingTools service not available; falling back to a "
                "deorbit burn from the craft's current orbit."
            )
            _deorbit(conn, vessel, args.periapsis)

    # retro-release coasts under SAS exactly like retrograde; it differs only
    # at the atmosphere interface below.
    setup_hold(
        conn,
        "retrograde" if args.hold == "retro-release" else args.hold,
        keep_wheels=args.rate_damp > 0.0,
    )
    wait_until_altitude_below(conn, predict_alt, "the prediction point")
    meta = cmd_predict(conn, args)
    meta["hold"] = args.hold

    flight = vessel.flight(body.reference_frame)
    if flight.mean_altitude < body.atmosphere_depth:
        print(
            "WARNING: the craft entered the atmosphere while predicting; "
            "high-altitude checkpoints will be missing from the actual log. "
            "Re-run with a higher --predict-altitude for full coverage."
        )
    wait_until_altitude_below(conn, body.atmosphere_depth, "atmospheric interface")
    if args.hold == "retro-release":
        # Release at the interface: the craft is aligned near its trim with
        # ~zero rates, so it slides to trim with a deterministic roll
        # orientation instead of tumbling through the capture swing.
        print("Atmosphere interface: releasing to uncontrolled flight.")
        if args.rate_damp > 0.0:
            _release_keep_wheels(vessel)
        else:
            from compare_aero_torque import _go_uncontrolled

            _go_uncontrolled(vessel)

    rows = sample_flight(conn, meta, args.rate, args.min_altitude)
    path = args.out + "_actual.csv"
    write_csv(path, ACT_COLUMNS, rows)
    meta["files"]["actual"] = path
    with open(args.out + "_meta.json", "w") as f:
        json.dump(meta, f, indent=1)

    analyze(args.out, png=args.png, checkpoints_km=_parse_checkpoints(args.checkpoints))


# ============================================================================
# Selftest (offline; validates the exact code paths the live run uses)
# ============================================================================
def selftest():
    failures = []

    def check(name, ok, detail=""):
        print(
            f"  {'PASS' if ok else 'FAIL'}  {name}"
            + (f"  ({detail})" if detail else "")
        )
        if not ok:
            failures.append(name)

    print("1. Quaternion helpers")
    rng = np.random.default_rng(4)
    a = rng.normal(size=3)
    a /= np.linalg.norm(a)
    b = rng.normal(size=3)
    b /= np.linalg.norm(b)
    q = q_shortest_arc(a, b)
    check("shortest arc maps a -> b", np.linalg.norm(q_rot(q, a) - b) < 1e-12)
    q1 = q_axis_angle((0, 0, 1), 0.7)
    q2 = q_axis_angle((0, 1, 0), -1.1)
    v = rng.normal(size=3)
    check(
        "composition (q1*q2 rotates by q2 first)",
        np.linalg.norm(q_rot(q_mult(q1, q2), v) - q_rot(q1, q_rot(q2, v))) < 1e-12,
    )
    v_air = rng.normal(size=3) * 500.0
    q_ret = retro_attitude(v_air, np.array(Q_IDENTITY))
    check(
        "retro attitude puts the nose on -v_air",
        np.linalg.norm(q_rot(q_ret, NOSE) + v_air / np.linalg.norm(v_air)) < 1e-9,
    )

    print("1b. Aerodynamic RPC routing")

    class FakeFlight:
        def __init__(self):
            self.force_calls = 0
            self.wrench_calls = 0
            self.wrench_uts = []

        def simulate_aerodynamic_force_at(self, body, r, v, q):
            self.force_calls += 1
            return (1.0, 2.0, 3.0)

        def simulate_aerodynamic_wrench_at(self, body, r, v, q, w, ut):
            self.wrench_calls += 1
            self.wrench_uts.append(ut)
            return (1.0, 2.0, 3.0), (4.0, 5.0, 6.0)

    def fake_rpc_aero(want_torque):
        aero = RpcAero.__new__(RpcAero)
        aero.body = object()
        aero.flight = FakeFlight()
        aero.radius = 10.0
        aero.atmo_top = 100.0
        aero.want_torque = want_torque
        aero.calls = 0
        return aero

    state_args = (
        np.array([20.0, 0.0, 0.0]),
        np.zeros(3),
        np.array(Q_IDENTITY),
        np.zeros(3),
        1000.0,
    )
    baseline_rpc = fake_rpc_aero(False)
    baseline_force, baseline_torque = baseline_rpc(*state_args)
    check(
        "baseline uses one legacy force RPC",
        baseline_rpc.calls == 1
        and baseline_rpc.flight.force_calls == 1
        and baseline_rpc.flight.wrench_calls == 0
        and np.array_equal(baseline_force, [1.0, 2.0, 3.0])
        and np.array_equal(baseline_torque, ZERO3),
    )
    wrench_rpc = fake_rpc_aero(True)
    wrench_force, wrench_torque = wrench_rpc(*state_args)
    check(
        "6-DOF uses one wrench RPC for force and torque",
        wrench_rpc.calls == 1
        and wrench_rpc.flight.force_calls == 0
        and wrench_rpc.flight.wrench_calls == 1
        and wrench_rpc.flight.wrench_uts == [1000.0]
        and np.array_equal(wrench_force, [1.0, 2.0, 3.0])
        and np.array_equal(wrench_torque, [4.0, 5.0, 6.0]),
    )

    class StageTimeAero:
        def __init__(self):
            self.uts = []

        def __call__(self, r, v, q, w, ut):
            self.uts.append(ut)
            return ZERO3, ZERO3

    stage_aero = StageTimeAero()
    integrate(
        {
            "mu": 0.0,
            "radius": 10.0,
            "atmo_depth": 100.0,
            "mass": 1.0,
            "inertia": np.eye(3).ravel().tolist(),
            "w_vec": [0.0, 0.0, 0.0],
        },
        stage_aero,
        "6dof",
        {
            "t": 100.0,
            "r": [20.0, 0.0, 0.0],
            "v": [-1.0, 0.0, 0.0],
            "q": Q_IDENTITY,
            "w": [0.0, 0.0, 0.0],
        },
        dt_atmo=1.0,
        dt_vac=1.0,
        record_dt=1.0,
        stop_alt=9.5,
        max_time=2.0,
    )
    check(
        "RK4 passes each stage's actual UT to the wrench",
        stage_aero.uts == [100.0, 100.5, 100.5, 101.0],
        f"UTs {stage_aero.uts}",
    )

    print("2. Circular orbit (gravity only)")
    mu, radius = 3.5316e12, 600000.0
    r_orb = radius + 100000.0
    v_orb = math.sqrt(mu / r_orb)
    period = 2.0 * math.pi * math.sqrt(r_orb**3 / mu)
    phys = {
        "mu": mu,
        "radius": radius,
        "atmo_depth": 70000.0,
        "mass": 1000.0,
        "inertia": np.eye(3).ravel().tolist(),
        "w_vec": [0.0, 0.0, 0.0],
    }
    aero0 = SyntheticAero(radius, 0.0, [0, 0, 0])  # atmo_top 0: never active
    state = {
        "t": 0.0,
        "r": [r_orb, 0, 0],
        "v": [0, v_orb, 0],
        "q": Q_IDENTITY,
        "w": [0, 0, 0],
    }
    rows = integrate(
        phys,
        aero0,
        "baseline",
        state,
        dt_atmo=1.0,
        dt_vac=1.0,
        record_dt=period / 10.0,
        stop_alt=-1.0e9,
        max_time=period,
    )
    r_end = np.array(rows[-1][2:5])
    drift = abs(np.linalg.norm(r_end) - r_orb) / r_orb
    check("radius drift < 0.1% after one orbit", drift < 1e-3, f"drift {drift:.2e}")

    print("3. Terminal velocity (linear drag, closed form)")

    class LinearDrag:
        calls = 0

        def __call__(self, r, v, q, w, ut):
            return -1000.0 * v, ZERO3  # b = 1000 kg/s; m = 1000 kg -> tau = 1 s

    g_alt = mu / (radius + 5000.0) ** 2  # gravity at the fall altitude
    state = {
        "t": 0.0,
        "r": [radius + 5000.0, 0, 0],
        "v": [0, 0, 0],
        "q": Q_IDENTITY,
        "w": [0, 0, 0],
    }
    rows = integrate(
        phys,
        LinearDrag(),
        "baseline",
        state,
        dt_atmo=0.01,
        dt_vac=0.01,
        record_dt=0.5,
        stop_alt=-1.0e9,
        max_time=8.0,
    )
    v_end = np.linalg.norm(np.array(rows[-1][5:8]))
    v_t = g_alt * 1.0  # g * tau
    check(
        "speed ~= terminal velocity after 8 tau",
        abs(v_end - v_t) / v_t < 0.01,
        f"{v_end:.3f} vs {v_t:.3f} m/s",
    )

    print("4. Torque-free tumbling (6-DOF: L and E conserved)")
    inertia = np.diag([1.0, 2.0, 3.0])
    phys_t = dict(phys, inertia=inertia.ravel().tolist(), mass=1000.0)
    w0 = np.array([0.3, 0.5, -0.2])
    state = {"t": 0.0, "r": [r_orb, 0, 0], "v": [0, v_orb, 0], "q": Q_IDENTITY, "w": w0}
    rows = integrate(
        phys_t,
        aero0,
        "6dof",
        state,
        dt_atmo=0.02,
        dt_vac=0.02,
        record_dt=1.0,
        stop_alt=-1.0e9,
        max_time=20.0,
    )

    def ang_mom_energy(row):
        qr = q_normalize(np.array(row[8:12]))
        wr = np.array(row[12:15])
        w_body = q_rot(q_conj(qr), wr)
        L = q_rot(qr, inertia.dot(w_body))
        return L, 0.5 * float(w_body.dot(inertia.dot(w_body)))

    L0, E0 = ang_mom_energy(rows[0])
    L1, E1 = ang_mom_energy(rows[-1])
    check(
        "angular momentum conserved",
        np.linalg.norm(L1 - L0) / np.linalg.norm(L0) < 1e-3,
        f"drift {np.linalg.norm(L1 - L0) / np.linalg.norm(L0):.2e}",
    )
    check(
        "rotational energy conserved",
        abs(E1 - E0) / E0 < 1e-3,
        f"drift {abs(E1 - E0) / E0:.2e}",
    )

    print(
        "4b. Damped attitude oscillation (envelope must track the analytic "
        "decay; RK2 fails this at short periods -- found in flight, step 2)"
    )

    class Pendulum:
        """tau = k * (nose x target) - c * w: stiffness + rate damping."""

        calls = 0

        def __init__(self, k, c):
            self.k, self.c = k, c

        def __call__(self, r, v, q_, w_, ut):
            target = -v / np.linalg.norm(v)
            nose = q_rot(q_, NOSE)
            return ZERO3, self.k * np.cross(nose, target) - self.c * np.asarray(w_)

    i_pitch = 328.0  # matches the step-2 test craft
    lam = math.log(4.0) / 120.0  # envelope 40 -> 10 deg over 120 s
    for period_s in (3.25, 1.6):
        omega = 2.0 * math.pi / period_s
        pend = Pendulum(i_pitch * omega * omega, 2.0 * i_pitch * lam)
        phys_p = {
            "mu": 1.0,
            "radius": 1.0,
            "atmo_depth": 1e12,
            "mass": 1000.0,
            "inertia": np.diag([i_pitch, 345.0, i_pitch]).ravel().tolist(),
            "w_vec": [0.0, 0.0, 0.0],
        }
        v0 = np.array([500.0, 0.0, 0.0])
        q_align = q_axis_angle((0, 0, 1), math.pi / 2.0)
        if np.dot(q_rot(q_align, NOSE), -v0 / np.linalg.norm(v0)) < 0.99:
            q_align = q_axis_angle((0, 0, 1), -math.pi / 2.0)
        q0 = q_mult(q_axis_angle((0, 0, 1), math.radians(40.0)), q_align)
        state = {
            "t": 0.0,
            "r": [1.0e9, 0.0, 0.0],
            "v": v0.tolist(),
            "q": q0.tolist(),
            "w": [0.0, 0.0, 0.0],
        }
        rows = integrate(
            phys_p,
            pend,
            "6dof",
            state,
            dt_atmo=0.1,
            dt_vac=0.1,
            record_dt=0.05,
            stop_alt=-1e30,
            max_time=185.0,
        )
        t_arr = np.array([row[0] for row in rows])
        aoa_arr = np.array([row[-1] for row in rows])
        m = t_arr >= 170.0
        peak = float(np.max(aoa_arr[m]))
        want = 40.0 * math.exp(-lam * 175.0)
        check(
            f"envelope tracks analytic decay (T={period_s} s, dt=0.1)",
            abs(peak - want) / want < 0.15,
            f"peak {peak:.2f} vs analytic {want:.2f} deg at t~175 s",
        )

    print("5. Fixed-frame geometry (derotation + lat/lon, both spin signs)")
    axis = np.array([0.2, 0.9, 0.1])
    axis /= np.linalg.norm(axis)
    e_pole = axis
    e0 = np.cross(axis, np.array([1.0, 0.0, 0.0]))
    e0 /= np.linalg.norm(e0)
    e90 = np.cross(e_pole, e0)
    w_mag = 2.9e-4
    for sign in (1.0, -1.0):
        meta_s = {
            "w_vec": (w_mag * axis).tolist(),
            "spin_sign": sign,
            "ut0": 1000.0,
            "E": np.vstack([e0, e90, e_pole]).tolist(),
        }
        lat_t, lon_t = 23.0, -57.0
        u = (
            math.cos(math.radians(lat_t)) * math.cos(math.radians(lon_t)) * e0
            + math.cos(math.radians(lat_t)) * math.sin(math.radians(lon_t)) * e90
            + math.sin(math.radians(lat_t)) * e_pole
        )
        ok = True
        for dt in (0.0, 300.0, 4000.0):
            p_t = rodrigues(u * 650000.0, axis, sign * w_mag * dt)
            lat, lon = latlon_deg(derotate(p_t, 1000.0 + dt, meta_s), meta_s)
            if abs(lat - lat_t) > 1e-6 or abs(lon - lon_t) > 1e-6:
                ok = False
        check(f"surface-fixed point is invariant (spin sign {sign:+.0f})", ok)

    print("6. Synthetic entry: dt convergence (Richardson check on defaults)")
    w_planet = np.array([0.0, 0.0, 2.9157e-4])
    phys_e = {
        "mu": mu,
        "radius": radius,
        "atmo_depth": 70000.0,
        "mass": 800.0,
        "inertia": np.eye(3).ravel().tolist(),
        "w_vec": w_planet.tolist(),
    }
    state = {
        "t": 0.0,
        "r": [radius + 80000.0, 0, 0],
        "v": [-100.0, 2200.0, 0],
        "q": Q_IDENTITY,
        "w": [0, 0, 0],
    }
    meta_e = {"w_vec": w_planet.tolist(), "spin_sign": 1.0, "ut0": 0.0}

    def impact(dt_atmo, dt_vac):
        aero = SyntheticAero(radius, 70000.0, w_planet)
        rows = integrate(
            phys_e,
            aero,
            "baseline",
            dict(state),
            dt_atmo=dt_atmo,
            dt_vac=dt_vac,
            record_dt=5.0,
            stop_alt=0.0,
            max_time=4000.0,
        )
        last = rows[-1]
        return derotate(np.array(last[2:5]), last[0], meta_e), last[0], last[1]

    p_coarse, t_coarse, alt_c = impact(0.5, 2.0)
    p_fine, t_fine, alt_f = impact(0.05, 0.2)
    check("both reach the surface", alt_c < 1.0 and alt_f < 1.0)
    miss = gc_distance(p_coarse, p_fine, radius)
    check(
        "impact point converged (coarse vs 10x finer < 2 km)",
        miss < 2000.0,
        f"miss {miss:.0f} m, dt_impact " f"{abs(t_coarse - t_fine):.1f} s",
    )

    print()
    if failures:
        sys.exit(f"SELFTEST FAILED: {len(failures)} failure(s): " + ", ".join(failures))
    print("SELFTEST PASSED")


# ============================================================================
# CLI
# ============================================================================
def _parse_checkpoints(text):
    return [float(x) for x in text.split(",") if x.strip()]


def _add_predict_args(p):
    p.add_argument("--mode", choices=["baseline", "6dof", "both"], default="both")
    p.add_argument(
        "--dt-baseline",
        type=float,
        default=0.5,
        help="integration step in atmosphere, baseline mode (s)",
    )
    p.add_argument(
        "--dt-6dof",
        type=float,
        default=0.1,
        help="integration step in atmosphere, 6-DOF mode (s)",
    )
    p.add_argument(
        "--dt-vacuum",
        type=float,
        default=2.0,
        help="integration step above the atmosphere (s)",
    )
    p.add_argument(
        "--record",
        type=float,
        default=0.5,
        help="prediction output cadence (s of simulated time)",
    )
    p.add_argument(
        "--stop-altitude",
        type=float,
        default=0.0,
        help="predict down to this altitude above sea level (m)",
    )
    p.add_argument(
        "--max-time",
        type=float,
        default=2000.0,
        help="give up predicting after this much simulated time (s)",
    )
    p.add_argument(
        "--rate-damp",
        type=float,
        default=0.0,
        help="reaction-wheel rate-damping gain K: control input = "
        "clamp(K * body angular rate). Applied to the real flight "
        "AND modelled identically in the 6-DOF prediction (torque "
        "= -wheel_torque * clamp(K * omega) per axis). 0 = off. "
        "Suppresses the phase-sensitive trim oscillation so the "
        "attitude history is deterministic; ~0.5 is a good start.",
    )
    p.add_argument("--out", default="reentry_run", help="output file prefix")
    p.add_argument("--name", default="reentry-predictor")


def main(argv=None):
    p = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    sub = p.add_subparsers(dest="cmd", required=True)

    pr = sub.add_parser(
        "run", help="teleport onto a re-entry arc, predict, " "fly, log and compare"
    )
    _add_predict_args(pr)
    pr.add_argument(
        "--hold",
        choices=["retrograde", "retro-release", "none"],
        default="none",
        help="retrograde: SAS holds surface retrograde all the way "
        "down (validation step 1); retro-release: SAS holds "
        "retrograde through the coast, then releases at the "
        "atmosphere interface -- the craft slides gently to its "
        "trim with a deterministic roll orientation (best for "
        "trim-ballast craft); none: fully uncontrolled from the "
        "start (tumbling release)",
    )
    pr.add_argument("--periapsis", type=float, default=30000.0)
    pr.add_argument(
        "--apoapsis",
        type=float,
        default=100000.0,
        help="apoapsis of the re-entry arc (m); sets entry speed",
    )
    pr.add_argument(
        "--predict-altitude",
        type=float,
        default=None,
        help="capture/predict at this altitude, descending "
        "(default: atmosphere top + 15 km)",
    )
    pr.add_argument(
        "--no-teleport",
        action="store_true",
        help="use the craft's current trajectory as-is",
    )
    pr.add_argument(
        "--rate", type=float, default=5.0, help="actual-flight log rate (Hz)"
    )
    pr.add_argument(
        "--min-altitude",
        type=float,
        default=500.0,
        help="stop logging below this altitude (m)",
    )
    pr.add_argument(
        "--checkpoints",
        default="40,30,20,10,5,2",
        help="comparison altitudes (km, comma-separated)",
    )
    pr.add_argument("--png", default=None)

    pp = sub.add_parser(
        "predict",
        help="capture + predict from the current "
        "state only (no teleport, no flight)",
    )
    _add_predict_args(pp)
    pp.add_argument(
        "--replay",
        default=None,
        metavar="PREFIX",
        help="re-predict from the capture state stored in "
        "PREFIX_meta.json instead of capturing fresh, and compare "
        "against PREFIX's logged actual flight. The same craft "
        "must be loaded in the game (any location works); this "
        "isolates server-side endpoint changes with zero "
        "flight-to-flight variance.",
    )
    pp.add_argument("--png", default=None)
    pp.add_argument("--checkpoints", default="40,30,20,10,5,2")

    pl = sub.add_parser("plot", help="re-analyse an existing run (no game)")
    pl.add_argument("--out", default="reentry_run", help="run file prefix")
    pl.add_argument("--png", default=None)
    pl.add_argument("--checkpoints", default="40,30,20,10,5,2")

    sub.add_parser(
        "selftest", help="offline integrator/geometry checks " "(numpy only, no game)"
    )

    args = p.parse_args(argv)
    if args.cmd == "selftest":
        selftest()
    elif args.cmd == "plot":
        analyze(
            args.out, png=args.png, checkpoints_km=_parse_checkpoints(args.checkpoints)
        )
    elif args.cmd == "predict":
        conn = _connect(args.name)
        meta = None
        if args.replay:
            with open(args.replay + "_meta.json") as f:
                meta = json.load(f)
            old_actual = meta["files"].get("actual")
            actual_aero_api = meta.get(
                "actual_aero_api", meta.get("aero_api", "legacy endpoints")
            )
            meta["files"] = {}
            if old_actual and os.path.exists(old_actual):
                meta["files"]["actual"] = old_actual
                # The new prediction uses the wrench API, but a retained
                # actual CSV keeps the simulation columns from the generation
                # that recorded it.
                meta["actual_aero_api"] = actual_aero_api
            vessel = conn.space_center.active_vessel
            if vessel.name != meta["craft"]:
                print(
                    f"WARNING: active vessel is '{vessel.name}' but the "
                    f"replayed capture is of '{meta['craft']}' -- the "
                    "simulation runs on the active vessel's parts, so the "
                    "comparison is only valid with the same craft loaded."
                )
            if abs(vessel.mass - meta["mass"]) > 0.01 * meta["mass"]:
                print(
                    f"WARNING: active vessel mass {vessel.mass:.0f} kg != "
                    f"captured {meta['mass']:.0f} kg -- this is probably NOT "
                    "the same craft (names alone don't distinguish unsaved "
                    "craft); the prediction would use the wrong drag cubes."
                )
            print(
                f"Replaying capture from {args.replay}_meta.json "
                f"({meta['craft']} at {meta['alt0'] / 1000.0:.1f} km)"
            )
        meta = cmd_predict(conn, args, meta=meta)
        if meta["files"].get("actual"):
            analyze(
                args.out,
                png=args.png,
                checkpoints_km=_parse_checkpoints(args.checkpoints),
            )
    elif args.cmd == "run":
        conn = _connect(args.name)
        cmd_run(conn, args)


if __name__ == "__main__":
    main()
