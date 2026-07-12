#!/usr/bin/env python3
"""
cube_probe.py

Fingerprint the active vessel's effective drag-cube state through the force
endpoint: evaluate SimulateAerodynamicForceAt for hypothetical wind along each
of the vessel's six principal body axes, plus a sweep of nose-to-side mixing
angles, at fixed hypothetical (altitude, speed). The per-direction drag
magnitudes are a direct function of the per-face areaOccluded*cd state, so
running the identical probe in two game contexts (launchpad vs atmospheric
flight) and diffing localizes any context-dependent cube state to specific
faces.

Usage:
  # On the launchpad:
  python cube_probe.py --out cube_pad.json

  # During an atmospheric descent (waits for the condition, then probes fast):
  python cube_probe.py --wait-below 20000 --out cube_flight.json

  # Offline diff:
  python cube_probe.py --compare cube_pad.json cube_flight.json

Vessel body axes: x = right, y = forward/nose, z = down.
"""

import argparse
import json
import math
import os
import sys
import time

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import q_rot, q_normalize  # noqa: E402

AXES = [
    ("+x", (1, 0, 0)),
    ("-x", (-1, 0, 0)),
    ("+y (nose)", (0, 1, 0)),
    ("-y (tail)", (0, -1, 0)),
    ("+z", (0, 0, 1)),
    ("-z", (0, 0, -1)),
]
MIX_DEG = [10, 15, 20, 25, 30, 45, 60, 75]


def probe(args):
    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)

    if args.wait_below is not None:
        print(f"Waiting for altitude < {args.wait_below:.0f} m in atmosphere...")
        while (
            flight.mean_altitude > args.wait_below
            or flight.mean_altitude > body.atmosphere_depth
        ):
            time.sleep(0.5)
        print(
            f"Probing at {flight.mean_altitude:.0f} m "
            f"(dynamic pressure {flight.dynamic_pressure:.0f} Pa)"
        )

    up = np.array(vessel.position(ref), dtype=float)
    up /= np.linalg.norm(up)
    pos = tuple(up * (body.equatorial_radius + args.altitude))
    rot = vessel.rotation(ref)
    rot_q = q_normalize(np.array(rot))

    # Wind directions are derived from the SAME frozen rotation passed to the
    # endpoint (never from the live vessel frame), so a craft that wobbles or
    # settles during the probe cannot skew the probed geometry.
    def wind_local(d):
        return tuple(args.speed * q_rot(rot_q, np.asarray(d, dtype=float)))

    results = {
        "craft": vessel.name,
        "mass": vessel.mass,
        "situation": str(vessel.situation),
        "altitude_arg": args.altitude,
        "speed_arg": args.speed,
        "density_at": body.density_at(args.altitude),
        "pressure_at": body.pressure_at(args.altitude),
        "probes": {},
        "components": {},
    }

    print(f"Craft: {vessel.name} ({vessel.mass:.0f} kg, {vessel.situation})")
    print(
        f"Hypothetical state: {args.altitude / 1000:.0f} km, " f"{args.speed:.0f} m/s\n"
    )
    print(f"{'wind direction':>16} {'|F| N':>12} {'drag N':>12} {'lift N':>12}")

    def one(label, d):
        w = wind_local(d)
        what = np.array(w) / np.linalg.norm(w)
        f = np.array(flight.simulate_aerodynamic_force_at(body, pos, w, rot))
        drag = float(np.dot(f, -what))  # along the airflow
        lift = float(np.linalg.norm(f + drag * what))  # perpendicular residue
        results["probes"][label] = float(np.linalg.norm(f))
        results["components"][label] = {
            "drag": drag,
            "lift": lift,
            "f": [float(x) for x in f],
        }
        print(f"{label:>16} {np.linalg.norm(f):12.1f} {drag:12.1f} " f"{lift:12.1f}")

    for label, d in AXES:
        one(label, d)
    # Nose-to-side mixing sweep in the y/x plane (wind rotating from -y toward
    # +x): this is the AoA-style face mixing where the discrepancy lives.
    for ang in MIX_DEG:
        th = math.radians(ang)
        one(f"tail+{ang}deg", (math.sin(th), -math.cos(th), 0.0))

    # Motion tripwire: the probed geometry is attitude-independent by
    # construction, but a vessel that moved may indicate other state changed.
    rot_end = q_normalize(np.array(vessel.rotation(ref)))
    drift = 2.0 * math.degrees(math.acos(min(1.0, abs(float(np.dot(rot_q, rot_end))))))
    results["attitude_drift_deg"] = drift
    if drift > 0.05:
        print(
            f"NOTE: vessel attitude drifted {drift:.2f} deg during the "
            "probe (settling/wobble). Probe geometry is unaffected (wind "
            "is derived from the frozen rotation), but be aware the craft "
            "was moving."
        )

    with open(args.out, "w") as fh:
        json.dump(results, fh, indent=1)
    print(f"\nWrote {args.out}")


def compare(path_a, path_b):
    with open(path_a) as fh:
        a = json.load(fh)
    with open(path_b) as fh:
        b = json.load(fh)
    print(f"A: {path_a} ({a['craft']}, {a['mass']:.0f} kg, {a['situation']})")
    print(f"B: {path_b} ({b['craft']}, {b['mass']:.0f} kg, {b['situation']})")
    if abs(a["mass"] - b["mass"]) > 0.01 * a["mass"]:
        print("WARNING: different masses -- probably different craft!")
    print(f"\n{'wind direction':>16} {'A N':>12} {'B N':>12} {'A/B':>8}")
    for key in a["probes"]:
        va, vb = a["probes"][key], b["probes"].get(key)
        if vb is None:
            continue
        print(
            f"{key:>16} {va:12.1f} {vb:12.1f} "
            f"{va / vb if vb else float('nan'):8.4f}"
        )
    print("\nA/B = 1.0000 everywhere: cube state identical in both contexts.")
    print(
        "A/B deviating on specific axes/angles: those faces' "
        "areaOccluded*cd differ between contexts."
    )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="cube_probe.json")
    ap.add_argument(
        "--altitude",
        type=float,
        default=10000.0,
        help="hypothetical probe altitude (m)",
    )
    ap.add_argument(
        "--speed",
        type=float,
        default=240.0,
        help="hypothetical wind speed (m/s); subsonic default to "
        "keep the mach curves quiet",
    )
    ap.add_argument(
        "--wait-below",
        type=float,
        default=None,
        help="wait until the craft is below this altitude inside "
        "the atmosphere before probing (for the in-flight run)",
    )
    ap.add_argument(
        "--compare",
        nargs=2,
        metavar=("A.json", "B.json"),
        help="diff two probe files (offline)",
    )
    ap.add_argument("--name", default="cube-probe")
    args = ap.parse_args()
    if args.compare:
        compare(*args.compare)
    else:
        probe(args)


if __name__ == "__main__":
    main()
