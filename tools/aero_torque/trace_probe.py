#!/usr/bin/env python3
"""
trace_probe.py

Call the (temporary) SimulateAerodynamicForceAtDebug endpoint at the tail and
several mixing directions and print the full per-term trace. Run on the
launchpad with the probe craft loaded, after recompiling with the debug method.

    python trace_probe.py
"""

import argparse
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import q_rot, q_normalize  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--speed", type=float, default=240.0)
    ap.add_argument("--altitude", type=float, default=10000.0)
    ap.add_argument("--name", default="trace-probe")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)
    if not hasattr(flight, "simulate_aerodynamic_force_at_debug"):
        sys.exit(
            "Server build lacks SimulateAerodynamicForceAtDebug -- " "recompile first."
        )

    up = np.array(vessel.position(ref), dtype=float)
    up /= np.linalg.norm(up)
    pos = tuple(up * (body.equatorial_radius + args.altitude))
    rot = vessel.rotation(ref)
    rot_q = q_normalize(np.array(rot))

    for label, d in [
        ("tail", (0.0, -1.0, 0.0)),
        ("tail+10", None),
        ("tail+25", None),
        ("tail+45", None),
    ]:
        if d is None:
            ang = math.radians(float(label.split("+")[1]))
            d = (math.sin(ang), -math.cos(ang), 0.0)
        wind = tuple(args.speed * q_rot(rot_q, np.asarray(d, dtype=float)))
        print(f"===== {label} =====")
        # The REAL endpoint at the identical state, for side-by-side comparison
        # with the canonical in-process trace below it.
        f = np.array(flight.simulate_aerodynamic_force_at(body, pos, wind, rot))
        what = np.array(wind) / np.linalg.norm(wind)
        drag = float(np.dot(f, -what))
        perp = float(np.linalg.norm(f + drag * what))
        print(
            f"REAL endpoint: |F|={np.linalg.norm(f):.1f} "
            f"alongWind={drag:.1f} perp={perp:.1f}"
        )
        print(flight.simulate_aerodynamic_force_at_debug(body, pos, wind, rot))
        print()


if __name__ == "__main__":
    main()
