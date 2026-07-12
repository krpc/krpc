#!/usr/bin/env python3
"""
state_watch.py

Catch the drag-cube state mutation in the act. Interleaves reads of the
(temporary) Flight.DragCubeStateDump debug property with force-endpoint calls
in mixed wind directions, and prints a diff of the cube state after every call
that changed it. Requires the server build with DragCubeStateDump.

Run on the launchpad with the probe craft loaded:
    python state_watch.py
"""

import argparse
import math
import sys

import numpy as np

import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from reentry_predictor import q_rot, q_normalize  # noqa: E402


def parse_dump(dump):
    """dump -> {partname: {field: value-string}}"""
    out = {}
    for line in dump.strip().splitlines():
        head, rest = line.split(" ", 1)
        fields = {}
        for tok in rest.split(" "):
            if "=" in tok:
                k, v = tok.split("=", 1)
                fields[k] = v
        out[head] = fields
    return out


def diff_dumps(before, after):
    msgs = []
    for part in after:
        b = before.get(part, {})
        for k, v in after[part].items():
            if b.get(k) != v:
                msgs.append(f"  {part}.{k}:\n    was {b.get(k)}\n    now {v}")
    return msgs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--speed", type=float, default=240.0)
    ap.add_argument("--altitude", type=float, default=10000.0)
    ap.add_argument("--name", default="state-watch")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    ref = body.reference_frame
    flight = vessel.flight(ref)
    if not hasattr(flight, "drag_cube_state_dump"):
        sys.exit(
            "Server build lacks DragCubeStateDump -- recompile with the "
            "debug property first."
        )

    up = np.array(vessel.position(ref), dtype=float)
    up /= np.linalg.norm(up)
    pos = tuple(up * (body.equatorial_radius + args.altitude))
    rot = vessel.rotation(ref)
    rot_q = q_normalize(np.array(rot))

    def sim(d):
        wind = tuple(args.speed * q_rot(rot_q, np.asarray(d, dtype=float)))
        f = np.array(flight.simulate_aerodynamic_force_at(body, pos, wind, rot))
        return float(np.linalg.norm(f))

    print("Initial cube state:")
    state = parse_dump(flight.drag_cube_state_dump)
    for part, fields in state.items():
        print(f"  {part}")
        for k, v in fields.items():
            print(f"    {k} = {v}")

    seq = (
        [("-y (tail)", (0.0, -1.0, 0.0))]
        + [
            (
                f"tail+{a}deg",
                (math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.0),
            )
            for a in (10, 25, 45, 25, 10)
        ]
        + [("-y (tail)", (0.0, -1.0, 0.0))]
    )

    print("\nInterleaved probe (state diff after each call):")
    for label, d in seq:
        f = sim(d)
        new_state = parse_dump(flight.drag_cube_state_dump)
        msgs = diff_dumps(state, new_state)
        marker = "  STATE CHANGED" if msgs else ""
        print(f"{label:>14}  |F| = {f:9.1f}{marker}")
        for m in msgs:
            print(m)
        state = new_state


if __name__ == "__main__":
    main()
