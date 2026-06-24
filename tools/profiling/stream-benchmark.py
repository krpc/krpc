#!/usr/bin/env python3
"""
Benchmark streaming RPC throughput for the kRPC TestServer hot path.

Measures server-side stream update rate and client-side read overhead
across different numbers of concurrent streams.

The server runs at 60 FPS, so the maximum possible stream update rate
is ~60 StreamUpdate messages/second regardless of the number of streams.
Each message bundles all changed results for a tick into one payload.

Usage:
    tools/profiling/stream-benchmark.py [--rpc-port PORT] [--measure-secs N]

Typical workflow:
    # Terminal 1: start the profiled server
    tools/profiling/run-profiled.sh -- --rpc-port=50000 --stream-port=50001

    # Terminal 2: run the stream benchmark
    tools/profiling/stream-benchmark.py
"""

from __future__ import annotations

import argparse
import sys
import threading
import time

import krpc

WARMUP_SECS = 2.0


def bench_update_rate(
    label: str,
    conn: krpc.client.Client,
    num_streams: int,
    measure_secs: float,
) -> float:
    """
    Add num_streams changing streams and count StreamUpdate messages/sec.

    Each stream tracks counter(id='sN'), which increments every evaluation,
    so the value always changes and the server always sends an update each tick.
    All N results are bundled in one StreamUpdate message per server tick.

    Returns StreamUpdate messages/sec.
    """
    ts = conn.test_service

    count = 0
    lock = threading.Lock()

    def on_update() -> None:
        nonlocal count
        with lock:
            count += 1

    # Use distinct counter IDs so each becomes a separate stream on the server.
    streams = [conn.add_stream(ts.counter, id=f"sb{i}") for i in range(num_streams)]
    conn.add_stream_update_callback(on_update)
    for s in streams:
        s.start(wait=False)

    # Warmup — let the EMA on the server stabilise before measuring.
    time.sleep(WARMUP_SECS)
    with lock:
        count = 0

    t0 = time.perf_counter()
    time.sleep(measure_secs)
    elapsed = time.perf_counter() - t0

    with lock:
        n = count

    conn.remove_stream_update_callback(on_update)
    for s in streams:
        s.remove()

    ups = n / elapsed
    us = elapsed / n * 1e6 if n > 0 else float("inf")
    print(f"  {label:<45s}  {ups:>6.1f} updates/s  {us:>7.1f} µs/update  ({n:,} total)")
    return ups


def bench_server_time(
    label: str,
    conn: krpc.client.Client,
    num_streams: int,
    measure_secs: float,
) -> None:
    """
    Add num_streams *static* streams and report the server's
    time_per_stream_update after the measurement window.

    Static streams use float_to_string(3.14) — the value never changes so
    the server evaluates the procedure but sends no updates.  This isolates
    the cost of procedure execution + change-detection from serialisation.
    """
    ts = conn.test_service

    streams = [conn.add_stream(ts.float_to_string, 3.14) for _ in range(num_streams)]
    for s in streams:
        s.start(wait=False)

    time.sleep(WARMUP_SECS)

    # Accumulate server-reported time_per_stream_update over the window.
    samples: list[float] = []
    t_end = time.perf_counter() + measure_secs
    while time.perf_counter() < t_end:
        samples.append(conn.krpc.get_status().time_per_stream_update)
        time.sleep(0.1)

    for s in streams:
        s.remove()

    if samples:
        avg_us = sum(samples) / len(samples) * 1e6
        print(f"  {label:<45s}  server time_per_stream_update: {avg_us:>6.1f} µs/tick")
    else:
        print(f"  {label:<45s}  (no samples)")


def bench_poll_rate(
    label: str,
    conn: krpc.client.Client,
    num_streams: int,
    iterations: int,
) -> None:
    """
    Read cached stream values in a tight loop; measures client-side overhead.

    This does not stress the server beyond whatever update rate it already runs
    at — it only measures how fast the Python client can read the most-recently-
    cached value for a stream (lock + attribute access).
    """
    ts = conn.test_service

    streams = [conn.add_stream(ts.counter, id=f"pr{i}") for i in range(num_streams)]
    for s in streams:
        s.start(wait=True)

    # Warmup
    for _ in range(min(1000, iterations // 10)):
        for s in streams:
            s()

    total = 0
    reads_per_iter = num_streams
    iters_needed = iterations // reads_per_iter
    t0 = time.perf_counter()
    for _ in range(iters_needed):
        for s in streams:
            s()
        total += reads_per_iter
    elapsed = time.perf_counter() - t0

    for s in streams:
        s.remove()

    rps = total / elapsed
    us = elapsed / total * 1e6
    print(f"  {label:<45s}  {rps:>10.0f} reads/s  {us:>6.2f} µs/read")


def main() -> None:
    parser = argparse.ArgumentParser(description="kRPC TestServer streaming benchmark")
    parser.add_argument("--rpc-port", type=int, default=50000)
    parser.add_argument("--stream-port", type=int, default=50001)
    parser.add_argument(
        "--measure-secs",
        type=float,
        default=10.0,
        help="Seconds per update-rate benchmark (default: 10)",
    )
    parser.add_argument(
        "--iterations",
        type=int,
        default=100_000,
        help="Total cached-value reads for the poll benchmark (default: 100000)",
    )
    args = parser.parse_args()

    print(f"Connecting to TestServer on port {args.rpc_port}...")
    try:
        conn = krpc.connect(
            name="stream-benchmark",
            address="127.0.0.1",
            rpc_port=args.rpc_port,
            stream_port=args.stream_port,
        )
    except Exception as e:
        print(f"Failed to connect: {e}", file=sys.stderr)
        print("Is the TestServer running?", file=sys.stderr)
        sys.exit(1)

    n = args.measure_secs

    # ------------------------------------------------------------------ #
    # Section 1: stream update rate (StreamUpdate messages/sec)           #
    # Server is the bottleneck here; this exercises the full stream path: #
    #   evaluate procedures → detect changes → serialise → send           #
    # ------------------------------------------------------------------ #
    print(f"\nStream update rate ({n:.0f}s per run):\n")
    for num in [1, 8, 32, 128, 256, 512]:
        s = "stream" if num == 1 else "streams"
        bench_update_rate(f"{num} changing {s}", conn, num, n)

    # ------------------------------------------------------------------ #
    # Section 2: server time per tick with static streams (no sends)      #
    # Procedure is evaluated every tick but value never changes, so the   #
    # server skips serialisation. Isolates evaluation + change-check cost.#
    # ------------------------------------------------------------------ #
    print(f"\nServer time per tick — static streams ({n:.0f}s per run):\n")
    bench_server_time("0 streams (baseline)", conn, 0, n)
    for num in [1, 16, 256, 512]:
        s = "stream" if num == 1 else "streams"
        bench_server_time(f"{num} static {s}", conn, num, n)

    # ------------------------------------------------------------------ #
    # Section 3: client-side cached-read rate                             #
    # Pure Python overhead of calling stream() to read the cached value.  #
    # ------------------------------------------------------------------ #
    print(f"\nClient-side cached read rate:\n")
    for num in [1, 16, 256, 512]:
        s = "stream" if num == 1 else "streams"
        bench_poll_rate(f"{num} {s}", conn, num, args.iterations)

    conn.close()
    print("\nDone.")


if __name__ == "__main__":
    main()
