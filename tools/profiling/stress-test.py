#!/usr/bin/env python3
"""
Continuous RPC stress-test against the kRPC in-game server.

Hammers a set of lightweight RPCs in a tight loop until stopped with Ctrl-C.
Watch RPC/s on the kRPC in-game window; this script also prints a local count
every few seconds so you can cross-check.

Usage:
    tools/profiling/stress-test.py [--host HOST] [--rpc-port PORT] [--stream-port PORT]

The script adapts to the current game state:
  - Space Center / any scene: calls basic RPCs (ut, game scene, paused)
  - Flight scene with active vessel: also calls vessel and flight RPCs
If the game state changes (scene switch, vessel loss), the RPC set is rebuilt
automatically without restarting the script.
"""
from __future__ import annotations

import argparse
import signal
import sys
import time
from typing import Callable

import krpc
from krpc.error import RPCError


REPORT_INTERVAL = 2.0  # seconds between local RPC/s prints


def build_rpc_list(conn: krpc.client.Client) -> tuple[list[Callable[[], object]], str]:
    """
    Probe the current game state and return (callables, description).
    Never raises — skips any RPC that errors during probing.
    """
    rpcs: list[Callable[[], object]] = []
    labels: list[str] = []

    kr = conn.krpc
    sc = conn.space_center

    # Always available
    rpcs.append(kr.get_status)
    labels.append("krpc.get_status")

    rpcs.append(lambda: kr.current_game_scene)
    labels.append("krpc.current_game_scene")

    rpcs.append(lambda: kr.paused)
    labels.append("krpc.paused")

    # Space-center-level properties (available outside main menu)
    try:
        _ = sc.ut
        rpcs.append(lambda: sc.ut)
        labels.append("space_center.ut")

        rpcs.append(lambda: sc.warp_factor)
        labels.append("space_center.warp_factor")

        rpcs.append(lambda: sc.warp_mode)
        labels.append("space_center.warp_mode")
    except RPCError:
        pass

    # Active vessel (flight scene)
    vessel = None
    try:
        vessel = sc.active_vessel
    except RPCError:
        pass

    if vessel is not None:
        try:
            _ = vessel.name
            rpcs.append(lambda: vessel.name)          # type: ignore[union-attr]
            labels.append("vessel.name")

            rpcs.append(lambda: vessel.met)           # type: ignore[union-attr]
            labels.append("vessel.met")

            rpcs.append(lambda: vessel.mass)          # type: ignore[union-attr]
            labels.append("vessel.mass")

            rpcs.append(lambda: vessel.situation)     # type: ignore[union-attr]
            labels.append("vessel.situation")
        except RPCError:
            vessel = None

    if vessel is not None:
        try:
            flight = vessel.flight()
            rpcs.append(lambda: flight.mean_altitude)
            labels.append("flight.mean_altitude")

            rpcs.append(lambda: flight.surface_altitude)
            labels.append("flight.surface_altitude")

            rpcs.append(lambda: flight.speed)
            labels.append("flight.speed")

            rpcs.append(lambda: flight.velocity)
            labels.append("flight.velocity")
        except RPCError:
            pass

    desc = f"{len(rpcs)} RPCs: [{', '.join(labels)}]"
    return rpcs, desc


def stress_loop(conn: krpc.client.Client) -> None:
    rpcs, desc = build_rpc_list(conn)
    print(f"RPC set: {desc}")
    print("Running — Ctrl-C to stop.\n")

    total = 0
    total_at_last_report = 0
    errors = 0
    t_report = time.perf_counter()

    while True:
        for fn in rpcs:
            try:
                fn()
                total += 1
            except RPCError:
                errors += 1
                # Game state changed — rebuild the RPC list
                try:
                    rpcs, desc = build_rpc_list(conn)
                    print(f"\nRPC set changed: {desc}")
                except Exception:
                    raise  # propagate connection errors

        now = time.perf_counter()
        if now - t_report >= REPORT_INTERVAL:
            elapsed = now - t_report
            rps = (total - total_at_last_report) / elapsed
            total_at_last_report = total
            t_report = now
            label = f"{rps:>9,.0f} RPC/s"
            if errors:
                label += f"  ({errors} errors since start)"
            print(f"\r{label}", end="", flush=True)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Continuous RPC stress-test for the kRPC in-game server"
    )
    parser.add_argument("--host", default="127.0.0.1",
                        help="kRPC server host (default: 127.0.0.1)")
    parser.add_argument("--rpc-port", type=int, default=50000,
                        help="RPC port (default: 50000)")
    parser.add_argument("--stream-port", type=int, default=50001,
                        help="Stream port (default: 50001)")
    args = parser.parse_args()

    # Clean exit on Ctrl-C without a traceback
    signal.signal(signal.SIGINT, lambda *_: sys.exit(0))

    print(f"Connecting to kRPC server at {args.host}:{args.rpc_port} ...")
    try:
        conn = krpc.connect(
            name="stress-test",
            address=args.host,
            rpc_port=args.rpc_port,
            stream_port=args.stream_port,
        )
    except Exception as exc:
        print(f"Failed to connect: {exc}", file=sys.stderr)
        sys.exit(1)

    print(f"Connected (server version {conn.krpc.get_status().version}).\n")

    try:
        stress_loop(conn)
    except (ConnectionResetError, OSError) as exc:
        print(f"\nConnection lost: {exc}", file=sys.stderr)
        sys.exit(1)
    finally:
        conn.close()


if __name__ == "__main__":
    main()
