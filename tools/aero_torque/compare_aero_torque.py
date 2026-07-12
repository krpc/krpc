#!/usr/bin/env python3
"""
compare_aero_torque.py

Measure the fidelity of kRPC's ``Flight.SimulateAerodynamicTorqueAt`` against the
running game during an atmospheric re-entry, and produce a comparison plot.

Three torque estimates are logged, all expressed in the vessel BODY frame, in N*m:

  measured   Ground truth from rigid-body dynamics (Euler's equation):

                 tau = I . alpha  +  omega x (I . omega)

             where ``omega`` is the vessel's inertial angular velocity (in body
             components), ``alpha`` its time derivative, and ``I`` the inertia
             tensor. This equals the NET torque on the craft, so it is only a
             valid measure of AERODYNAMIC torque when aero is the dominant torque
             source -- i.e. the craft must re-enter UNCONTROLLED: SAS, RCS,
             reaction wheels and engine thrust all off. ``run``/``sample`` switch
             those off for you before logging.

  simulated  ``Flight.SimulateAerodynamicTorqueAt(...)`` evaluated at the vessel's
             current position, velocity, attitude and angular velocity -- i.e. the
             endpoint asked to predict the torque for the state it is actually in.

  live_sum   ``Flight.AerodynamicTorque`` -- the live per-part (force x lever) sum
             added by the server patch (issue #429). Logged automatically if the
             patched server build is installed; skipped (NaN) otherwise.

The ``measured`` series is the independent oracle. ``simulated`` is what we are
grading. ``live_sum`` isolates one failure mode: it and ``simulated`` share the
part-origin lever convention, so any gap between them is due to the simulator
re-computing per-part forces from drag cubes rather than reading the live ones.

Usage
-----
  # Teleport the active craft onto a re-entry arc (needs the TestingTools
  # plugin; any starting state works, even the launchpad), log, then plot:
  python compare_aero_torque.py run  --out reentry.csv --rate 10 --periapsis 30000

  # Tune the arc: entry speed via apoapsis, placement point via entry altitude:
  python compare_aero_torque.py run  --apoapsis 250000 --entry-altitude 72000

  # You put the craft on a re-entry trajectory yourself; just log + plot:
  python compare_aero_torque.py sample --out reentry.csv --rate 10

  # Isolate a spurious-roll bug with a static AoA sweep (run on the launchpad,
  # no oracle needed -- symmetry says roll torque must be ~0):
  python compare_aero_torque.py probe --speed 300 --aoa-max 40

  # Re-plot an existing log (no game / kRPC needed):
  python compare_aero_torque.py plot --csv reentry.csv --png reentry.png

Notes
-----
* ``krpc`` is only imported for ``run``/``sample``; ``plot`` runs anywhere numpy +
  matplotlib are installed.
* Time stamps use in-game universal time (``space_center.ut``), and ``alpha`` is
  computed from the actual dt between samples, so physics/real-time drift and an
  imprecise loop cadence do not bias the result.
* Vessel body axes (kRPC ``Vessel.reference_frame``): x = right (pitch axis),
  y = forward/nose (roll axis), z = down (yaw axis).
"""

import argparse
import csv
import math
import sys
import time

import numpy as np

# ---- CSV schema -------------------------------------------------------------
# One header, shared by the logger and the analyser.
COLUMNS = (
    ["t", "altitude", "speed", "q", "mach"]
    + ["wx", "wy", "wz"]
    + [f"I{r}{c}" for r in range(3) for c in range(3)]
    + ["sim_x", "sim_y", "sim_z"]
    + ["live_x", "live_y", "live_z"]
)


# ============================================================================
# Data capture (needs a running KSP + kRPC server)
# ============================================================================
def _connect(name):
    import krpc  # local import so `plot` works without kRPC installed

    return krpc.connect(name=name)


# kRPC quaternion helpers (x, y, z, w), matching krpctest.geometry.
def _q_axis_angle(axis, angle):
    s = math.sin(angle / 2.0)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(angle / 2.0))


def _q_mult(q, r):
    q0, q1, q2, q3 = q
    r0, r1, r2, r3 = r
    return (
        +r0 * q3 - r1 * q2 + r2 * q1 + r3 * q0,
        +r0 * q2 + r1 * q3 - r2 * q0 + r3 * q1,
        -r0 * q1 + r1 * q0 + r2 * q3 + r3 * q2,
        -r0 * q0 - r1 * q1 - r2 * q2 + r3 * q3,
    )


def _place_on_reentry(
    conn, vessel, target_periapsis, apoapsis_altitude, entry_altitude
):
    """Teleport the active craft onto a re-entry arc via the TestingTools service.

    The craft is placed on an elliptical orbit with the given apoapsis and
    periapsis altitudes, at ``entry_altitude`` on the DESCENDING branch (past
    apoapsis, falling toward the atmosphere), with its rotation cleared.
    """
    sc = conn.space_center
    body = vessel.orbit.body
    radius = body.equatorial_radius
    if entry_altitude is None:
        entry_altitude = body.atmosphere_depth + 5000.0
    if target_periapsis >= entry_altitude:
        sys.exit(
            f"--periapsis ({target_periapsis:.0f} m) must be below the entry "
            f"altitude ({entry_altitude:.0f} m)"
        )

    r_pe = radius + target_periapsis
    r_ap = radius + max(apoapsis_altitude, entry_altitude + 1000.0)
    sma = 0.5 * (r_pe + r_ap)
    ecc = (r_ap - r_pe) / (r_ap + r_pe)

    # True anomaly where the orbit crosses the entry radius, descending branch
    # (nu in (pi, 2pi)), then convert to the mean anomaly set_orbit expects.
    r0 = radius + entry_altitude
    cos_nu = (sma * (1.0 - ecc * ecc) / r0 - 1.0) / ecc
    nu = 2.0 * math.pi - math.acos(max(-1.0, min(1.0, cos_nu)))
    ecc_anomaly = 2.0 * math.atan2(
        math.sqrt(1.0 - ecc) * math.sin(nu / 2.0),
        math.sqrt(1.0 + ecc) * math.cos(nu / 2.0),
    )
    mean_anomaly = ecc_anomaly - ecc * math.sin(ecc_anomaly)

    print(
        f"Placing craft on re-entry arc: {entry_altitude:.0f} m descending, "
        f"apoapsis {r_ap - radius:.0f} m, periapsis {target_periapsis:.0f} m..."
    )
    conn.testing_tools.set_orbit(
        body.name, sma, ecc, 0.0, 0.0, 0.0, mean_anomaly, sc.ut
    )
    try:
        conn.testing_tools.clear_rotation()
    except Exception:
        pass
    time.sleep(1.0)  # let the game settle after the teleport


def _deorbit(conn, vessel, target_periapsis):
    """Orient retrograde and burn until the periapsis drops below the target."""
    sc = conn.space_center
    c = vessel.control
    print("Deorbit: orienting retrograde...")
    c.throttle = 0.0
    c.sas = True
    time.sleep(0.2)
    try:
        c.sas_mode = sc.SASMode.retrograde
    except Exception:
        print("  (retrograde SAS mode unavailable; point retrograde manually)")

    # Give SAS time to settle onto the retrograde marker.
    t_align = sc.ut
    while sc.ut - t_align < 20:
        time.sleep(0.5)

    if vessel.orbit.periapsis_altitude <= target_periapsis:
        print("  Periapsis already below target; skipping burn.")
        return

    print(f"Deorbit: burning to periapsis <= {target_periapsis:.0f} m...")
    if vessel.available_thrust <= 0:
        # Nothing lit -- try staging once to bring an engine online.
        c.activate_next_stage()
        time.sleep(1.0)

    c.throttle = 1.0
    t_burn = sc.ut
    while vessel.orbit.periapsis_altitude > target_periapsis:
        if sc.ut - t_burn > 120:
            print("  Burn timed out; check staging/fuel. Continuing anyway.")
            break
        time.sleep(0.1)
    c.throttle = 0.0
    print("Deorbit burn complete.")


def _wait_for_atmosphere(conn, vessel, body):
    top = body.atmosphere_depth
    flight = vessel.flight()
    print(f"Coasting to atmospheric interface ({top:.0f} m)...")
    while flight.mean_altitude > top:
        time.sleep(1.0)
    print("Entered atmosphere.")


def _go_uncontrolled(vessel):
    """Remove every non-aerodynamic torque source so `measured` == aero torque."""
    c = vessel.control
    c.throttle = 0.0
    c.sas = False
    c.rcs = False
    try:
        vessel.auto_pilot.disengage()
    except Exception:
        pass
    n = 0
    for rw in vessel.parts.reaction_wheels:
        try:
            rw.active = False
            n += 1
        except Exception:
            pass
    print(f"Uncontrolled re-entry: SAS/RCS/thrust off, {n} reaction wheel(s) disabled.")
    print("(Control surfaces are left passive -- with no input they do not actuate.)")


def sample(conn, out_csv, rate, min_altitude):
    """Log measured/simulated/live torque until the craft drops below min_altitude."""
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body

    nonrot = body.non_rotating_reference_frame  # inertial: for true angular velocity
    brf = body.reference_frame  # co-rotating: air-relative kinematics
    flight = vessel.flight(brf)

    # Is the live-sum property present on this server build?
    has_live = hasattr(flight, "aerodynamic_torque")
    try:
        if has_live:
            _ = flight.aerodynamic_torque  # may throw under FAR
    except Exception:
        has_live = False
    print(
        f"Live AerodynamicTorque property: {'available' if has_live else 'not available'}"
    )

    period = 1.0 / rate
    rows = []
    t0 = sc.ut
    print(
        f"Sampling at {rate} Hz until altitude < {min_altitude:.0f} m. Ctrl-C to stop early."
    )
    try:
        while True:
            t = sc.ut - t0
            vrf = vessel.reference_frame  # rotates with the craft; queried fresh

            # Inertial angular velocity, expressed in body components.
            w_nonrot = vessel.angular_velocity(nonrot)
            wx, wy, wz = sc.transform_direction(w_nonrot, nonrot, vrf)

            I = vessel.inertia_tensor  # 9 doubles, vessel-frame basis
            alt = flight.mean_altitude
            speed = flight.speed
            q = flight.dynamic_pressure
            try:
                mach = flight.mach
            except Exception:
                mach = float("nan")

            # Simulator prediction at the current state (returned in brf -> body).
            pos = vessel.position(brf)
            vel = vessel.velocity(brf)
            rot = vessel.rotation(brf)
            wv = vessel.angular_velocity(brf)
            sim = flight.simulate_aerodynamic_torque_at(body, pos, vel, rot, wv)
            sim_x, sim_y, sim_z = sc.transform_direction(sim, brf, vrf)

            # Live per-part sum (returned in brf -> body), if present.
            if has_live:
                try:
                    live = flight.aerodynamic_torque
                    live_x, live_y, live_z = sc.transform_direction(live, brf, vrf)
                except Exception:
                    live_x = live_y = live_z = float("nan")
            else:
                live_x = live_y = live_z = float("nan")

            rows.append(
                [
                    t,
                    alt,
                    speed,
                    q,
                    mach,
                    wx,
                    wy,
                    wz,
                    *I,
                    sim_x,
                    sim_y,
                    sim_z,
                    live_x,
                    live_y,
                    live_z,
                ]
            )

            if alt < min_altitude:
                print("Reached minimum altitude; stopping.")
                break
            time.sleep(period)
    except KeyboardInterrupt:
        print("\nInterrupted; writing what we have.")

    with open(out_csv, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(COLUMNS)
        w.writerows(rows)
    print(f"Wrote {len(rows)} samples to {out_csv}")
    return out_csv


def probe(conn, speed, aoa_max, aoa_step, spin, name):
    """Static angle-of-attack sweep to isolate a spurious-roll bug.

    Holds the craft (no spin) in a synthetic ``speed`` wind and pitches the
    attitude through a range of angles of attack, calling
    ``SimulateAerodynamicTorqueAt`` at each. For a laterally SYMMETRIC craft at a
    pitch-plane angle of attack, the aerodynamic torque must lie entirely on the
    pitch axis -- there is a mirror plane (containing the wind and the nose), so
    any roll or yaw component is forbidden by symmetry and indicates a bug. No
    ground-truth oracle is needed: the symmetry itself is the reference.

    Run it on the launchpad (or anywhere inside an atmosphere) so the simulated
    position has sea-level-ish density; in orbit the density is ~0 and every
    torque is ~0.
    """
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)

    pos = vessel.position(ref)
    nose0 = np.array(vessel.direction(ref), dtype=float)  # roll axis at AoA 0
    up = np.array(pos, dtype=float)
    up = up / np.linalg.norm(up)  # local vertical
    p = np.cross(nose0, up)  # pitch axis
    if np.linalg.norm(p) < 0.1:
        p = np.cross(nose0, np.array([1.0, 0.0, 0.0]))
    p = p / np.linalg.norm(p)
    rot0 = vessel.rotation(ref)

    rho = flight.atmosphere_density
    print(
        f"Static AoA probe: speed {speed:.0f} m/s, spin {spin:.2f} rad/s about roll, "
        f"density {rho:.4f} kg/m^3, q ~ {0.5 * rho * speed * speed:.0f} Pa"
    )
    if rho < 1e-4:
        print(
            "WARNING: density ~ 0 at this position -- run on the launchpad or in "
            "the atmosphere, not in orbit. Torques will all be ~0."
        )
    if spin == 0.0:
        print(
            "Zero spin: a symmetric craft at pitch-plane AoA must produce PITCH torque "
            "only; any roll/yaw is symmetry-forbidden and a bug.\n"
        )
    else:
        print(
            "With spin, a small roll-DAMPING torque (opposing the spin) is physical; "
            "watch for an implausibly LARGE roll that grows with q.\n"
        )
    print(
        f"{'AoA':>5} {'|torque|':>11} {'pitch':>11} {'roll':>11} {'yaw':>11} "
        f"{'roll/|t|':>9} {'offaxis%':>9}"
    )
    print("-" * 72)

    rows = []
    for aoa_deg in range(0, int(aoa_max) + 1, int(aoa_step)):
        th = math.radians(aoa_deg)
        rot = _q_mult(_q_axis_angle(tuple(p), th), rot0)
        vel = tuple(speed * nose0)
        nose_th = nose0 * math.cos(th) + np.cross(p, nose0) * math.sin(th)  # roll axis
        yaw_th = np.cross(p, nose_th)  # yaw axis
        omega = tuple(spin * nose_th)  # spin about roll
        tau = np.array(
            flight.simulate_aerodynamic_torque_at(body, pos, vel, rot, omega),
            dtype=float,
        )
        pitch_t = float(np.dot(tau, p))
        roll_t = float(np.dot(tau, nose_th))
        yaw_t = float(np.dot(tau, yaw_th))
        tot = float(np.linalg.norm(tau))
        frac = math.sqrt(roll_t * roll_t + yaw_t * yaw_t) / tot if tot > 1e-9 else 0.0
        rows.append((aoa_deg, tot, pitch_t, roll_t, yaw_t, frac))
        print(
            f"{aoa_deg:5d} {tot:11.1f} {pitch_t:11.1f} {roll_t:11.1f} {yaw_t:11.1f} "
            f"{(roll_t / tot if tot > 1e-9 else 0.0):9.3f} {frac:9.3f}"
        )

    print("-" * 72)
    # Judge only where there is meaningful torque -- the AoA=0 row is ~zero (no aero
    # moment) and its ratios are pure noise.
    max_tot = max((r[1] for r in rows), default=0.0)
    graded = [r for r in rows if r[1] > max(50.0, 0.05 * max_tot)]
    worst = max((r[5] for r in graded), default=0.0)
    worst_roll = max((abs(r[3]) for r in graded), default=0.0)
    if spin == 0.0:
        if worst > 0.05:
            print(
                f"VERDICT: off-axis (roll+yaw) torque reaches {100 * worst:.0f}% of the "
                "total at real AoA -> symmetry-forbidden component confirmed. The bug is "
                "in the static force x lever geometry (shared by SimAeroTorque and "
                "AerodynamicTorque), not the angular-velocity term."
            )
        else:
            print(
                f"VERDICT: torque stays on the pitch axis (off-axis < {100 * worst:.0f}% "
                "at real AoA). No spurious roll from static geometry -- the reentry "
                "discrepancy needs spin (--spin) and/or supersonic speed (--speed) to "
                "reproduce."
            )
    else:
        print(
            f"VERDICT (spin={spin:.2f}): peak roll torque {worst_roll:.0f} N*m. Re-run "
            f"with --spin {-spin:.2f}: real damping FLIPS sign with the spin (odd); a "
            "spin-independent bug keeps the SAME sign (even). A roll that dwarfs pitch "
            "or scales with q is the reentry bug."
        )
    print(
        "NOTE: assumes a laterally symmetric craft. Canted fins / offset parts give a "
        "small real roll; the bug is the LARGE, AoA- or q-growing one."
    )


# ============================================================================
# Analysis + plotting (no game required)
# ============================================================================
def load_csv(path):
    data = np.genfromtxt(path, delimiter=",", names=True)
    n = data.shape[0] if data.shape else 1
    cols = {name: np.atleast_1d(data[name]).astype(float) for name in data.dtype.names}
    return cols, n


def measured_torque(t, w, I):
    """tau = I.alpha + omega x (I.omega), per sample, in body components.

    ``w`` is (N,3); ``I`` is (N,9) row-major 3x3; ``t`` is (N,). alpha is a
    finite difference against the actual (possibly non-uniform) time stamps.
    """
    alpha = np.gradient(w, t, axis=0)  # central differences interior, one-sided ends
    tau = np.empty_like(w)
    for k in range(len(t)):
        Ik = I[k].reshape(3, 3)
        tau[k] = Ik.dot(alpha[k]) + np.cross(w[k], Ik.dot(w[k]))
    return tau, alpha


def _smooth(x, window):
    if window <= 1:
        return x
    k = np.ones(window) / window
    if x.ndim == 1:
        return np.convolve(x, k, mode="same")
    return np.column_stack(
        [np.convolve(x[:, j], k, mode="same") for j in range(x.shape[1])]
    )


def _stats(meas, other, mask):
    m = meas[mask].ravel()
    o = other[mask].ravel()
    good = np.isfinite(m) & np.isfinite(o)
    m, o = m[good], o[good]
    if m.size < 2:
        return None
    rmse = float(np.sqrt(np.mean((o - m) ** 2)))
    denom = float(np.sqrt(np.mean(m**2))) or float("nan")
    # slope of best-fit line through origin: how much the estimate scales vs truth
    slope = float(np.dot(o, m) / np.dot(m, m))
    corr = float(np.corrcoef(m, o)[0, 1])
    return {
        "rmse": rmse,
        "rmse_pct": 100 * rmse / denom,
        "slope": slope,
        "corr": corr,
        "n": int(m.size),
    }


def analyze_and_plot(csv_path, png_path, smooth=5, q_floor=200.0):
    import matplotlib

    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    cols, n = load_csv(csv_path)
    t = cols["t"]
    q = cols["q"]
    alt = cols["altitude"]
    w = np.column_stack([cols["wx"], cols["wy"], cols["wz"]])
    I = np.column_stack([cols[f"I{r}{c}"] for r in range(3) for c in range(3)])
    sim = np.column_stack([cols["sim_x"], cols["sim_y"], cols["sim_z"]])
    live = np.column_stack([cols["live_x"], cols["live_y"], cols["live_z"]])
    has_live = np.isfinite(live).any()

    meas, _ = measured_torque(t, w, I)
    meas_s = _smooth(meas, smooth)  # measured torque is the noisy one (differentiated)

    mag = lambda a: np.linalg.norm(a, axis=1)
    axis_names = ["x (pitch)", "y (roll)", "z (yaw)"]

    # Only grade where there is meaningful airflow.
    mask = q > q_floor

    fig = plt.figure(figsize=(13, 9))
    gs = fig.add_gridspec(3, 2, height_ratios=[1.1, 1.1, 1.3], hspace=0.42, wspace=0.26)

    # (1) Torque magnitude over time, with dynamic pressure for context.
    ax0 = fig.add_subplot(gs[0, :])
    ax0.plot(t, mag(meas_s), color="#111", lw=2.0, label="measured (I·α + ω×Iω)")
    ax0.plot(t, mag(sim), color="#d1495b", lw=1.6, label="simulated (endpoint)")
    if has_live:
        ax0.plot(
            t, mag(live), color="#3a7ca5", lw=1.4, ls="--", label="live per-part sum"
        )
    ax0.set_ylabel("|torque|  (N·m)")
    ax0.set_xlabel("time since entry (s)")
    ax0.set_title("Aerodynamic torque magnitude through re-entry")
    ax0.legend(loc="upper right", fontsize=9)
    axq = ax0.twinx()
    axq.plot(t, q / 1000.0, color="#bbb", lw=1.0, zorder=0)
    axq.set_ylabel("dynamic pressure (kPa)", color="#999")
    axq.tick_params(axis="y", colors="#999")

    # (2) Per-axis time series (measured vs simulated).
    ax1 = fig.add_subplot(gs[1, :])
    palette = ["#e07a5f", "#3d405b", "#81b29a"]
    for j in range(3):
        ax1.plot(
            t, meas_s[:, j], color=palette[j], lw=1.8, label=f"measured {axis_names[j]}"
        )
        ax1.plot(
            t,
            sim[:, j],
            color=palette[j],
            lw=1.2,
            ls=":",
            label=f"simulated {axis_names[j]}",
        )
    ax1.axhline(0, color="#ddd", lw=0.8)
    ax1.set_ylabel("torque component (N·m)")
    ax1.set_xlabel("time since entry (s)")
    ax1.set_title("Per-axis torque (solid = measured, dotted = simulated)")
    ax1.legend(loc="upper right", ncol=3, fontsize=7.5)

    # (3) Scatter: estimate vs measured, all axes, where q > floor.
    ax2 = fig.add_subplot(gs[2, 0])
    mm = meas_s[mask]
    ax2.scatter(
        mm.ravel(),
        sim[mask].ravel(),
        s=8,
        alpha=0.35,
        color="#d1495b",
        label="simulated",
    )
    if has_live:
        ax2.scatter(
            mm.ravel(),
            live[mask].ravel(),
            s=8,
            alpha=0.35,
            color="#3a7ca5",
            label="live sum",
        )
    lim = np.nanpercentile(np.abs(mm), 99) if mm.size else 1.0
    lim = float(lim) if np.isfinite(lim) and lim > 0 else 1.0
    ax2.plot([-lim, lim], [-lim, lim], color="#111", lw=1.0, ls="--", label="y = x")
    ax2.set_xlim(-lim, lim)
    ax2.set_ylim(-lim, lim)
    ax2.set_xlabel("measured torque (N·m)")
    ax2.set_ylabel("estimated torque (N·m)")
    ax2.set_title(f"Estimate vs measured  (q > {q_floor:.0f} Pa)")
    ax2.legend(loc="upper left", fontsize=8)
    ax2.set_aspect("equal", "box")

    # (4) Fidelity summary text.
    ax3 = fig.add_subplot(gs[2, 1])
    ax3.axis("off")
    s_sim = _stats(meas_s, sim, mask)
    s_live = _stats(meas_s, live, mask) if has_live else None
    lines = [
        "Fidelity vs measured ground truth",
        f"(graded on {int(mask.sum())} of {n} samples, q > {q_floor:.0f} Pa)",
        "",
    ]
    if s_sim:
        lines += [
            "simulated endpoint:",
            f"   RMSE      {s_sim['rmse']:.1f} N·m  ({s_sim['rmse_pct']:.1f}% of RMS)",
            f"   slope     {s_sim['slope']:.3f}  (1.0 = unbiased)",
            f"   corr      {s_sim['corr']:.4f}",
            "",
        ]
    if s_live:
        lines += [
            "live per-part sum:",
            f"   RMSE      {s_live['rmse']:.1f} N·m  ({s_live['rmse_pct']:.1f}% of RMS)",
            f"   slope     {s_live['slope']:.3f}",
            f"   corr      {s_live['corr']:.4f}",
        ]
    ax3.text(
        0.0,
        1.0,
        "\n".join(lines),
        va="top",
        ha="left",
        family="monospace",
        fontsize=10,
        transform=ax3.transAxes,
    )

    fig.suptitle(
        "SimulateAerodynamicTorqueAt fidelity during re-entry", fontsize=14, y=0.98
    )
    fig.savefig(png_path, dpi=130, bbox_inches="tight")
    print(f"Wrote plot to {png_path}")
    if s_sim:
        print(
            f"simulated: RMSE {s_sim['rmse']:.1f} N·m ({s_sim['rmse_pct']:.1f}%), "
            f"slope {s_sim['slope']:.3f}, corr {s_sim['corr']:.4f}"
        )
    if s_live:
        print(
            f"live sum:  RMSE {s_live['rmse']:.1f} N·m ({s_live['rmse_pct']:.1f}%), "
            f"slope {s_live['slope']:.3f}, corr {s_live['corr']:.4f}"
        )


# ============================================================================
# CLI
# ============================================================================
def main(argv=None):
    p = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    sub = p.add_subparsers(dest="cmd", required=True)

    pr = sub.add_parser(
        "run",
        help="place the craft on a re-entry arc "
        "(TestingTools; falls back to a deorbit burn), then log + plot",
    )
    pr.add_argument("--out", default="reentry.csv")
    pr.add_argument("--png", default=None, help="plot path (default: <out>.png)")
    pr.add_argument("--rate", type=float, default=10.0, help="samples per second")
    pr.add_argument(
        "--periapsis", type=float, default=30000.0, help="target periapsis (m)"
    )
    pr.add_argument(
        "--apoapsis",
        type=float,
        default=100000.0,
        help="apoapsis of the re-entry arc (m); sets the entry speed",
    )
    pr.add_argument(
        "--entry-altitude",
        type=float,
        default=None,
        help="altitude to place the craft at, descending "
        "(default: atmosphere top + 5 km)",
    )
    pr.add_argument(
        "--min-altitude", type=float, default=1000.0, help="stop below this (m)"
    )
    pr.add_argument("--name", default="aero-torque")

    ps = sub.add_parser("sample", help="log + plot (you start the re-entry)")
    ps.add_argument("--out", default="reentry.csv")
    ps.add_argument("--png", default=None)
    ps.add_argument("--rate", type=float, default=10.0)
    ps.add_argument("--min-altitude", type=float, default=1000.0)
    ps.add_argument("--name", default="aero-torque")

    pb = sub.add_parser(
        "probe",
        help="static AoA sweep to isolate spurious roll "
        "(no oracle; run on the launchpad)",
    )
    pb.add_argument(
        "--speed", type=float, default=300.0, help="synthetic wind speed (m/s)"
    )
    pb.add_argument(
        "--aoa-max", type=float, default=40.0, help="max angle of attack (deg)"
    )
    pb.add_argument("--aoa-step", type=float, default=5.0, help="AoA increment (deg)")
    pb.add_argument(
        "--spin",
        type=float,
        default=0.0,
        help="angular velocity about the roll axis (rad/s); 0 = static",
    )
    pb.add_argument("--name", default="aero-torque")

    pp = sub.add_parser("plot", help="re-plot an existing CSV (no game needed)")
    pp.add_argument("--csv", required=True)
    pp.add_argument("--png", default=None)
    pp.add_argument(
        "--smooth", type=int, default=5, help="moving-average window on measured"
    )
    pp.add_argument(
        "--q-floor",
        type=float,
        default=200.0,
        help="grade only where q exceeds this (Pa)",
    )

    args = p.parse_args(argv)

    if args.cmd == "plot":
        png = args.png or (args.csv.rsplit(".", 1)[0] + ".png")
        analyze_and_plot(args.csv, png, smooth=args.smooth, q_floor=args.q_floor)
        return

    conn = _connect(args.name)
    if args.cmd == "probe":
        probe(conn, args.speed, args.aoa_max, args.aoa_step, args.spin, args.name)
        return
    vessel = conn.space_center.active_vessel
    body = vessel.orbit.body
    if args.cmd == "run":
        if hasattr(conn, "testing_tools"):
            _place_on_reentry(
                conn, vessel, args.periapsis, args.apoapsis, args.entry_altitude
            )
        else:
            print(
                "TestingTools service not available; falling back to a deorbit "
                "burn from the craft's current orbit."
            )
            _deorbit(conn, vessel, args.periapsis)
        _wait_for_atmosphere(conn, vessel, body)
    _go_uncontrolled(vessel)
    sample(conn, args.out, args.rate, args.min_altitude)
    png = args.png or (args.out.rsplit(".", 1)[0] + ".png")
    analyze_and_plot(args.out, png)


if __name__ == "__main__":
    main()
