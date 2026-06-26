#!/usr/bin/env bash
# Run throughput and allocation benchmarks against a freshly-built TestServer.
# Outputs results to stdout; also writes profile data to PROFILE_OUT.
#
# Usage:
#   tools/profiling/run-benchmark.sh [label]
#
# Environment:
#   PROFILE_OUT   - path for mono profiler output  (default: /tmp/krpc-profile-<label>.mlpd)
#   PYTHON        - python interpreter with krpc installed
#   RPC_PORT      - RPC port (default: 50000)
#   STREAM_PORT   - stream port (default: 50001)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LABEL="${1:-run}"
RPC_PORT="${RPC_PORT:-50000}"
STREAM_PORT="${STREAM_PORT:-50001}"
PYTHON="${PYTHON:-$REPO_ROOT/env/bin/python}"
PROFILE_OUT="${PROFILE_OUT:-/tmp/krpc-profile-${LABEL}.mlpd}"
BENCH_ITERS="${BENCH_ITERS:-100000}"

echo ""
echo "=========================================="
echo " kRPC benchmark — ${LABEL}"
echo "=========================================="
echo ""

echo "==> Building TestServer..."
bazel build //tools/TestServer 2>&1 | tail -3

# Set up a flat runfiles dir (needed so mono can find all DLLs)
RUNDIR="$(mktemp -d)"
trap 'rm -rf "$RUNDIR"; kill "$SERVER_PID" 2>/dev/null || true' EXIT

MANIFEST="$REPO_ROOT/bazel-bin/tools/TestServer/TestServer.runfiles_manifest"
while IFS=' ' read -r _key src; do
    case "$src" in
        *.dll|*.exe|*.xml|*.mdb)
            ln -sf "$src" "$RUNDIR/$(basename "$src")"
            ;;
    esac
done < "$MANIFEST"

# ---- Throughput benchmark (no profiler) ----
echo ""
echo "--- Throughput (${BENCH_ITERS} iterations each) ---"
echo ""

cd "$RUNDIR"
/usr/bin/mono TestServer.exe --rpc-port="$RPC_PORT" --stream-port="$STREAM_PORT" --quiet \
    >"$RUNDIR/server.log" 2>&1 &
SERVER_PID=$!

# Wait for the server to start
for i in $(seq 1 20); do
    sleep 0.5
    if grep -q "Server started successfully" "$RUNDIR/server.log" 2>/dev/null; then
        break
    fi
    if ! kill -0 "$SERVER_PID" 2>/dev/null; then
        echo "ERROR: Server failed to start. Log:" >&2
        cat "$RUNDIR/server.log" >&2
        exit 1
    fi
done

cd "$REPO_ROOT"
"$PYTHON" tools/profiling/benchmark.py \
    --rpc-port="$RPC_PORT" --stream-port="$STREAM_PORT" \
    --iterations="$BENCH_ITERS"

kill "$SERVER_PID" 2>/dev/null || true
wait "$SERVER_PID" 2>/dev/null || true

# ---- Allocation benchmark (mono log profiler, alloc mode) ----
echo ""
echo "--- Allocation profile (${BENCH_ITERS} iterations) ---"
echo ""

cd "$RUNDIR"
/usr/bin/mono --profile="log:output=$PROFILE_OUT,alloc,nodefaults" \
    TestServer.exe --rpc-port="$RPC_PORT" --stream-port="$STREAM_PORT" --quiet \
    >"$RUNDIR/server-profiled.log" 2>&1 &
SERVER_PID=$!

for i in $(seq 1 20); do
    sleep 0.5
    if grep -q "Server started successfully" "$RUNDIR/server-profiled.log" 2>/dev/null; then
        break
    fi
    if ! kill -0 "$SERVER_PID" 2>/dev/null; then
        echo "ERROR: Profiled server failed to start. Log:" >&2
        cat "$RUNDIR/server-profiled.log" >&2
        exit 1
    fi
done

cd "$REPO_ROOT"
"$PYTHON" tools/profiling/benchmark.py \
    --rpc-port="$RPC_PORT" --stream-port="$STREAM_PORT" \
    --iterations="$BENCH_ITERS"

kill "$SERVER_PID" 2>/dev/null || true
wait "$SERVER_PID" 2>/dev/null || true

echo ""
echo "==> Profiler output written to: $PROFILE_OUT"
echo ""
echo "==> Top allocating types:"
mprof-report --traces --top=30 "$PROFILE_OUT" 2>/dev/null | \
    awk '/^Allocation:/,/^$/' | head -40 || \
    mprof-report "$PROFILE_OUT" 2>/dev/null | grep -A 40 "Allocation summary" | head -45 || \
    echo "(mprof-report parsing failed — check $PROFILE_OUT directly)"
