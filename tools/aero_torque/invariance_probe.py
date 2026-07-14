#!/usr/bin/env python3
"""
invariance_probe.py

Test the rotation invariance of Flight.SimulateAerodynamicForceAt's hypothetical
attitude machinery (the delta = desired x current^-1 mapping), with zero physics
confound. Run on the launchpad (any scene with the craft loaded).

Test 1 -- co-rotation invariance: evaluate the force with the attitude AND the
wind both rotated by the same angle about the same axis. The airflow relative
to the craft is IDENTICAL in every case (head-on, AoA 0), so the returned force
magnitude must be constant. Any trend with angle is delta-machinery error, and
its shape can be compared directly with the drag-ratio-vs-AoA curve measured
from flight telemetry.

Test 2 -- two-path AoA sweep: sweep AoA by (a) pitching the attitude with the
wind fixed, and (b) rotating the wind with the attitude fixed. Same relative
geometry per AoA; (a) exercises large deltas, (b) exercises none. The two
sweeps must agree; their ratio is the delta error as a function of delta angle.

Test 3 -- reference-frame invariance: express one physical center-of-mass
state in the rotating body, non-rotating body and vessel frames, call
SimulateAerodynamicWrenchAt in each, and transform each result to the same
non-rotating output frame for comparison.

Usage:
    python invariance_probe.py [--mode all] [--speed 800] [--altitude 10000]
"""

import argparse
import math

import numpy as np


def q_mult(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + bw * ax + ay * bz - az * by,
        aw * by + bw * ay + az * bx - ax * bz,
        aw * bz + bw * az + ax * by - ay * bx,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def q_axis_angle(axis, angle):
    axis = np.asarray(axis, dtype=float)
    axis = axis / np.linalg.norm(axis)
    s = math.sin(angle / 2.0)
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(angle / 2.0))


def rodrigues(v, axis, angle):
    c, s = math.cos(angle), math.sin(angle)
    return v * c + np.cross(axis, v) * s + axis * np.dot(axis, v) * (1.0 - c)


def compare_reference_frames(conn, speed, altitude):
    """Compare one physical wrench state through all supported frame types."""
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    common = body.non_rotating_reference_frame
    frames = (
        ("rotating", body.reference_frame),
        ("non-rotating", common),
        ("vessel", vessel.reference_frame),
    )

    up = np.array(vessel.position(common), dtype=float)
    up /= np.linalg.norm(up)
    position = up * (body.equatorial_radius + altitude)
    nose = np.array(vessel.direction(common), dtype=float)
    atmosphere_velocity = np.cross(
        np.array(body.angular_velocity(common), dtype=float), position
    )
    velocity = atmosphere_velocity + speed * nose
    rotation = vessel.rotation(common)

    # Do not pause KSP here. KRPC.Paused can stop the server before the pause
    # setter's response is flushed, deadlocking the probe on some launches.
    # The fixture is stationary on the pad and these calls complete within a
    # few milliseconds, which keeps the sequential frame-conversion skew far
    # below the comparison tolerance.
    probe_ut = sc.ut
    results = []
    for label, ref in frames:
        pos_ref = sc.transform_position(tuple(position), common, ref)
        vel_ref = sc.transform_velocity(tuple(position), tuple(velocity), common, ref)
        rot_ref = sc.transform_rotation(rotation, common, ref)
        rate_ref = body.angular_velocity(ref)
        force, torque = vessel.flight(ref).simulate_aerodynamic_wrench_at(
            body, pos_ref, vel_ref, rot_ref, rate_ref, probe_ut
        )
        results.append(
            (
                label,
                np.array(sc.transform_direction(force, ref, common)),
                np.array(sc.transform_direction(torque, ref, common)),
            )
        )

    force0, torque0 = results[0][1:]
    force_scale = max(np.linalg.norm(force0), 1.0)
    torque_scale = max(np.linalg.norm(torque0), 1.0)
    print(
        "\nTest 3: same physical wrench state in three reference frames "
        "(outputs transformed to non-rotating)"
    )
    print(
        f"{'input frame':>13} {'|F| N':>13} {'|T| N m':>13} "
        f"{'rel dF':>11} {'rel dT':>11}"
    )
    max_residual = 0.0
    for label, force, torque in results:
        df = np.linalg.norm(force - force0) / force_scale
        dt = np.linalg.norm(torque - torque0) / torque_scale
        max_residual = max(max_residual, df, dt)
        print(
            f"{label:>13} {np.linalg.norm(force):13.4f} "
            f"{np.linalg.norm(torque):13.4f} {df:11.3e} {dt:11.3e}"
        )
    print(
        f"Maximum relative residual: {max_residual:.3e} "
        f"({'PASS' if max_residual <= 1e-4 else 'FAIL'} at 1e-4)"
    )
    if max_residual > 1e-4:
        raise SystemExit(
            "REFERENCE-FRAME INVARIANCE FAILED: "
            f"relative residual {max_residual:.3e} > 1e-4"
        )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--speed", type=float, default=800.0)
    ap.add_argument("--altitude", type=float, default=10000.0)
    ap.add_argument("--step", type=float, default=10.0, help="angle step (deg)")
    ap.add_argument(
        "--mode",
        choices=("attitude", "frames", "all"),
        default="all",
        help="run hypothetical-attitude checks, frame comparison, or both",
    )
    ap.add_argument("--name", default="invariance-probe")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    if args.mode == "frames":
        compare_reference_frames(conn, args.speed, args.altitude)
        return
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)

    up = np.array(vessel.position(ref), dtype=float)
    up /= np.linalg.norm(up)
    pos = tuple(up * (body.equatorial_radius + args.altitude))
    nose = np.array(vessel.direction(ref), dtype=float)
    pitch = np.cross(nose, up)
    if np.linalg.norm(pitch) < 0.1:
        pitch = np.cross(nose, np.array([1.0, 0.0, 0.0]))
    pitch /= np.linalg.norm(pitch)
    rot0 = vessel.rotation(ref)
    rho = body.density_at(args.altitude)
    print(
        f"Craft: {vessel.name}, altitude {args.altitude / 1000:.0f} km "
        f"(rho {rho:.4f}), wind {args.speed:.0f} m/s, "
        f"rotations about the pitch axis\n"
    )

    def force(rot, vel):
        return np.array(
            flight.simulate_aerodynamic_force_at(body, pos, tuple(vel), tuple(rot))
        )

    angles = np.arange(0.0, 90.0 + 1e-9, args.step)

    print(
        "Test 1: attitude AND wind co-rotated (AoA always 0; |F| must be " "constant)"
    )
    print(f"{'delta deg':>10} {'|F| N':>12} {'vs delta=0':>11}")
    f0 = None
    for ang in angles:
        th = math.radians(ang)
        rot = q_mult(q_axis_angle(pitch, th), rot0)
        wind = args.speed * rodrigues(nose, pitch, th)  # still head-on
        f = np.linalg.norm(force(rot, wind))
        if f0 is None:
            f0 = f
        print(f"{ang:10.0f} {f:12.1f} {f / f0:11.4f}")

    print(
        "\nTest 2: AoA sweep two ways -- (a) rotate attitude, wind fixed "
        "(large delta); (b) rotate wind, attitude fixed (zero delta)"
    )
    print(f"{'AoA deg':>8} {'|F| a N':>12} {'|F| b N':>12} {'a/b':>8}")
    wind0 = args.speed * nose
    for ang in angles:
        th = math.radians(ang)
        rot_a = q_mult(q_axis_angle(pitch, th), rot0)
        f_a = np.linalg.norm(force(rot_a, wind0))
        wind_b = args.speed * rodrigues(nose, pitch, -th)
        f_b = np.linalg.norm(force(rot0, wind_b))
        print(f"{ang:8.0f} {f_a:12.1f} {f_b:12.1f} {f_a / f_b:8.4f}")

    print(
        "\nTest 1 flat at 1.0000 and Test 2 a/b flat at 1.0000: the delta "
        "machinery is exact.\nA trend growing with angle reproduces the "
        "flight-measured drag-ratio curve and localizes the bug to the "
        "rotation mapping in SimulateAerodynamicForceAt."
    )
    if args.mode == "all":
        compare_reference_frames(conn, args.speed, args.altitude)


if __name__ == "__main__":
    main()
