#!/usr/bin/env python3
"""
torque_trace.py

Print the (temporary) per-part sim-vs-live aero torque breakdown at the
vessel's current state, several times during a descent. Requires the server
build with Flight.TraceAeroTorqueLive.

Fly (or quickload into) a trim descent and run:
    python torque_trace.py --below 30000 --count 4 --interval 15
"""

import argparse
import time


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--below",
        type=float,
        default=30000.0,
        help="start tracing below this altitude (m)",
    )
    ap.add_argument("--count", type=int, default=4)
    ap.add_argument(
        "--interval", type=float, default=15.0, help="seconds between traces"
    )
    ap.add_argument("--name", default="torque-trace")
    args = ap.parse_args()

    import krpc

    conn = krpc.connect(name=args.name)
    sc = conn.space_center
    vessel = sc.active_vessel
    body = vessel.orbit.body
    flight = vessel.flight(body.reference_frame)
    if not hasattr(flight, "trace_aero_torque_live"):
        raise SystemExit("Server build lacks TraceAeroTorqueLive -- recompile.")

    print(f"Waiting for altitude < {args.below:.0f} m...")
    while flight.mean_altitude > args.below:
        time.sleep(0.5)
    for k in range(args.count):
        print(
            f"===== trace {k + 1} (alt {flight.mean_altitude:.0f} m, "
            f"AoA context: see dump) ====="
        )
        print(flight.trace_aero_torque_live(body))
        print()
        if k + 1 < args.count:
            time.sleep(args.interval)


if __name__ == "__main__":
    main()
