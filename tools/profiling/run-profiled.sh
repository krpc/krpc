#!/usr/bin/env bash
# Run TestServer under the Mono log profiler and write a report.
#
# Usage:
#   tools/profiling/run-profiled.sh [-- TestServer args...]
#
# After the server exits (Ctrl-C), mprof-report prints the profile summary.
#
# Profile modes (set PROFILE_MODE env var, default: sample):
#   sample  - statistical sampling (low overhead, good for hot-path discovery)
#   calls   - exact call counting + alloc tracking (high overhead, accurate)
#
# Example:
#   PROFILE_MODE=calls tools/profiling/run-profiled.sh -- --rpc-port=50000 --stream-port=50001

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROFILE_OUT="${PROFILE_OUT:-/tmp/krpc-profile.mlpd}"
PROFILE_MODE="${PROFILE_MODE:-sample}"

echo "==> Building TestServer..."
bazel build //tools/TestServer >/dev/null 2>&1

RUNDIR="$(mktemp -d)"
trap 'rm -rf "$RUNDIR"' EXIT

# Read the runfiles manifest to symlink all needed assemblies into a flat dir.
# This mirrors what the Bazel launcher script does, but keeps the dir around
# until we've finished profiling.
MANIFEST="$REPO_ROOT/bazel-bin/tools/TestServer/TestServer.runfiles_manifest"
while IFS=' ' read -r _key src; do
    case "$src" in
        *.dll|*.exe|*.xml|*.mdb)
            ln -sf "$src" "$RUNDIR/$(basename "$src")"
            ;;
    esac
done < "$MANIFEST"

case "$PROFILE_MODE" in
    sample)
        # sample=real: wall-clock SIGPROF sampling, low overhead, good for hot-path discovery
        PROFILE_OPTS="log:output=$PROFILE_OUT,sample=real"
        ;;
    calls)
        # calls + alloc: exact call counts and allocation tracking; high overhead
        PROFILE_OPTS="log:output=$PROFILE_OUT,calls,alloc"
        ;;
    *)
        echo "Unknown PROFILE_MODE '$PROFILE_MODE'. Use 'sample' or 'calls'." >&2
        exit 1
        ;;
esac

echo "==> Profile output: $PROFILE_OUT"
echo "==> Mode: $PROFILE_MODE"
echo "==> Run a benchmark in another terminal:"
echo "      tools/profiling/benchmark.py        (RPC throughput)"
echo "      tools/profiling/stream-benchmark.py (streaming throughput)"
echo "==> Press Ctrl-C to stop the server and generate the report."
echo ""

cd "$RUNDIR"
echo "TestServer.exe $@"
/usr/bin/mono --profile="$PROFILE_OPTS" TestServer.exe "$@" || true

echo ""
echo "==> Generating profile report..."
mprof-report "$PROFILE_OUT"
