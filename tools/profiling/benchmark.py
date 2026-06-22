#!/usr/bin/env python3
"""
Benchmark script for the kRPC TestServer hot path.

Connects to a running TestServer and hammers it with RPC calls to measure
throughput and give the Mono profiler something to sink its teeth into.

Usage:
    tools/profiling/benchmark.py [--rpc-port PORT] [--iterations N]

Typical workflow:
    # Terminal 1: start the profiled server
    tools/profiling/run-profiled.sh -- --rpc-port=50000 --stream-port=50001

    # Terminal 2: run the benchmark
    tools/profiling/benchmark.py
"""
from __future__ import annotations

import argparse
import sys
import time

import krpc


def bench(label: str, fn, iterations: int) -> float:
    # Warmup
    for _ in range(min(1000, iterations // 10)):
        fn()

    start = time.perf_counter()
    for _ in range(iterations):
        fn()
    elapsed = time.perf_counter() - start

    rps = iterations / elapsed
    us = elapsed / iterations * 1e6
    print(f"  {label:<40s}  {rps:>10.0f} RPC/s  {us:>8.2f} µs/call")
    return rps


def main() -> None:
    parser = argparse.ArgumentParser(description="kRPC TestServer benchmark")
    parser.add_argument("--rpc-port", type=int, default=50000)
    parser.add_argument("--stream-port", type=int, default=50001)
    parser.add_argument("--iterations", type=int, default=50_000,
                        help="RPC calls per benchmark (default: 50000)")
    args = parser.parse_args()

    print(f"Connecting to TestServer on port {args.rpc_port}...")
    try:
        conn = krpc.connect(
            name="benchmark",
            address="127.0.0.1",
            rpc_port=args.rpc_port,
            stream_port=args.stream_port,
        )
    except Exception as e:
        print(f"Failed to connect: {e}", file=sys.stderr)
        print("Is the TestServer running?", file=sys.stderr)
        sys.exit(1)

    ts = conn.test_service
    n = args.iterations

    print(f"\nRunning {n:,} iterations per benchmark...\n")

    bench("int32_to_string(42)",      lambda: ts.int32_to_string(42),       n)
    bench("float_to_string(3.14)",   lambda: ts.float_to_string(3.14),     n)
    bench("enum_return()",           lambda: ts.enum_return(),              n)
    bench("counter()",               lambda: ts.counter(),                  n)

    # Initialise the property so the getter doesn't error.
    ts.string_property = "hello"

    def set_string():
        ts.string_property = "x"

    bench("string_property get",     lambda: ts.string_property,           n)
    bench("string_property set",     set_string,                           n)

    obj = ts.create_test_object("prof")
    bench("object.get_value()",      lambda: obj.get_value(),              n)

    def set_int():
        obj.int_property = 7

    bench("object.int_property get", lambda: obj.int_property,             n)
    bench("object.int_property set", set_int,                              n)

    conn.close()
    print("\nDone.")


if __name__ == "__main__":
    main()
