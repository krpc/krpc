#!/usr/bin/env python3
"""
trim_probe.py

Measure a capsule's aerodynamic trim angle on the launchpad, using
SimulateAerodynamicTorqueAt: at each flight condition, find the angle of
attack where the pitch torque crosses zero (the attitude the capsule will
actually fly), and report the lift it generates there.

Use while designing a trim-ballast craft: aim for a trim of ~8-15 degrees at
the hypersonic conditions (30-40 km) for a strong lifting-entry effect.

Run on the launchpad with the craft loaded:
    python trim_probe.py
"""

import argparse
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import q_rot, q_mult, q_normalize, q_axis_angle  # noqa: E402

CONDITIONS = [
    (45000.0, 2300.0),
    (35000.0, 2000.0),
    (25000.0, 1200.0),
    (12000.0, 400.0),
    (6000.0, 250.0),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--max-aoa", type=float, default=30.0)
    ap.add_argument("--step", type=float, default=1.0)
    ap.add_argument("--name", default="trim-probe")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)

    up = np.array(vessel.position(ref), dtype=float)
    up /= np.linalg.norm(up)
    rot0 = q_normalize(np.array(vessel.rotation(ref)))
    radius = body.equatorial_radius
    print(f"Craft: {vessel.name} ({vessel.mass:.0f} kg)")
    print(
        f"{'alt km':>7} {'v m/s':>6} {'trim deg':>9} {'lift N':>9} "
        f"{'drag N':>9} {'L/D':>6}"
    )

    for alt, speed in CONDITIONS:
        pos = tuple(up * (radius + alt))
        # Tail-first (re-entry attitude) wind along the vessel -y axis.
        wind = tuple(speed * q_rot(rot0, np.array([0.0, -1.0, 0.0])))
        w_hat = np.array(wind) / speed
        zero3 = (0.0, 0.0, 0.0)

        tau0 = np.array(
            flight.simulate_aerodynamic_torque_at(body, pos, wind, tuple(rot0), zero3)
        )
        if np.linalg.norm(tau0) < 1.0:
            print(f"{alt / 1000:7.0f} {speed:6.0f} {'~0 (symmetric)':>9}")
            continue
        # The static torque at AoA 0 pushes the capsule toward its trim; the
        # rotation axis of that push is the torque direction.
        axis = tau0 / np.linalg.norm(tau0)

        prev_ang, prev_tau = 0.0, float(np.dot(tau0, axis))
        trim = None
        ang = args.step
        while ang <= args.max_aoa + 1e-9:
            rot = q_normalize(q_mult(q_axis_angle(axis, math.radians(ang)), rot0))
            tau = float(
                np.dot(
                    np.array(
                        flight.simulate_aerodynamic_torque_at(
                            body, pos, wind, tuple(rot), zero3
                        )
                    ),
                    axis,
                )
            )
            if prev_tau > 0.0 >= tau:
                trim = prev_ang + (ang - prev_ang) * prev_tau / (prev_tau - tau)
                break
            prev_ang, prev_tau = ang, tau
            ang += args.step

        if trim is None:
            print(
                f"{alt / 1000:7.0f} {speed:6.0f} {'>' + str(args.max_aoa):>9}"
                "  (no zero crossing found -- unstable or trim beyond range)"
            )
            continue
        rot_trim = q_normalize(q_mult(q_axis_angle(axis, math.radians(trim)), rot0))
        f = np.array(
            flight.simulate_aerodynamic_force_at(body, pos, wind, tuple(rot_trim))
        )
        drag = float(np.dot(f, -w_hat))
        lift = float(np.linalg.norm(f + drag * w_hat))
        print(
            f"{alt / 1000:7.0f} {speed:6.0f} {trim:9.2f} {lift:9.0f} "
            f"{drag:9.0f} {lift / drag if drag else float('nan'):6.3f}"
        )

    print(
        "\nTarget: trim ~8-15 deg with L/D ~0.2+ at the 25-45 km conditions "
        "for a demo-scale lifting entry."
    )


if __name__ == "__main__":
    main()
