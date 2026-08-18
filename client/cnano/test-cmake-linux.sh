#!/bin/bash
# Test building the C-nano client using CMake.
# Accepts the release archive as the first argument.
# Covers the two things a build chooses between, crossed with each other:
#   how nanopb is provided) system) a system-installed nanopb
#                           fetch)  nanopb fetched via FetchContent (KRPC_FETCH_DEPS=ON)
#   which transport)        serialio) a serial port, the default
#                           tcpip)    TCP/IP (KRPC_COMMUNICATION_TCP=ON)
# Each is followed by a consumer test using find_package(krpc_cnano CONFIG REQUIRED).
# Usage: test-cmake-linux.sh ARCHIVE [system-serialio|system-tcpip|fetch-serialio|fetch-tcpip]
#        (default: run all)
set -e
set -o pipefail
set -x
set -o functrace

scenarios="system-serialio system-tcpip fetch-serialio fetch-tcpip"

archive="$(realpath "${1:?Usage: test-cmake-linux.sh ARCHIVE [$(echo $scenarios | tr ' ' '|')]}")"
scenario="${2:-all}"

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$scriptroot/../.."

root=$(pwd)
out=$root/bazel-bin/client/cnano/test-build
version=$(basename "$archive" .zip | sed 's/krpc-cnano-//')

# Extract the release archive
rm -rf "$out"
mkdir -p "$out"
unzip -q "$archive" -d "$out"
mv "$out/krpc-cnano-$version"/* "$out/"
rm -r "$out/krpc-cnano-$version"

# Configure krpc_cnano; save cmake output to log_file for later verification.
function cmake_configure {
  local build_dir=$1
  local install_dir=$2
  local log_file=$3
  shift 3
  mkdir -p "$build_dir"
  cmake -S "$out" -B "$build_dir" \
    -DCMAKE_INSTALL_PREFIX="$install_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    "$@" 2>&1 | tee "$log_file"
}

# Build and install the krpc_cnano library.
function build_install {
  local build_dir=$1
  local install_dir=$2
  local log_file=$3
  shift 3
  cmake_configure "$build_dir" "$install_dir" "$log_file" "$@"
  cmake --build "$build_dir" --parallel $(nproc)
  cmake --install "$build_dir"
}

# Verify a pattern appears in the cmake configure log.
function check_present {
  local log=$1
  local pattern=$2
  if ! grep -q "$pattern" "$log"; then
    echo "FAIL: expected '${pattern}' in cmake configure output but not found"
    exit 1
  fi
}

# Verify a pattern does not appear in the cmake configure log.
function check_absent {
  local log=$1
  local pattern=$2
  if grep -q "$pattern" "$log"; then
    echo "FAIL: '${pattern}' should not appear in cmake configure output"
    exit 1
  fi
}

# Build the consumer project against the installed package, to verify the package config and
# targets work end-to-end. Where the library is built for TCP/IP the program opens a connection,
# which is what proves the transport reaches it through the package.
function consumer_test {
  local install_dir=$1
  local build_dir=$2
  cmake -S "$scriptroot/test-consumer" -B "$build_dir" \
    -DCMAKE_PREFIX_PATH="$install_dir" \
    -DCMAKE_BUILD_TYPE=Release
  cmake --build "$build_dir" --parallel $(nproc)
}

# Build, install and consume the library for one scenario, named for how nanopb is provided and
# which transport the library talks over.
function run_scenario {
  local name=$1
  local dir="$out/$name"
  local log="$dir/configure.log"
  local options=()

  case "$name" in
    fetch-*)  options+=(-DKRPC_FETCH_DEPS=ON) ;;
    system-*) ;;
    *) echo "unknown scenario '$name'"; exit 1 ;;
  esac
  case "$name" in
    *-tcpip)    options+=(-DKRPC_COMMUNICATION_TCP=ON) ;;
    *-serialio) ;;
    *) echo "unknown scenario '$name'"; exit 1 ;;
  esac

  build_install "$dir/build" "$dir/install" "$log" "${options[@]}"

  # Which of the two ways of providing nanopb the configure step actually took
  case "$name" in
    fetch-*)
      check_present "$log" "Fetching nanopb via FetchContent"
      check_absent  "$log" "Found nanopb"
      ;;
    system-*)
      check_present "$log" "Found nanopb"
      check_absent  "$log" "Fetching nanopb via FetchContent"
      ;;
  esac

  consumer_test "$dir/install" "$dir/consumer"
}

if [[ "$scenario" == "all" ]]; then
  for name in $scenarios; do
    run_scenario "$name"
  done
else
  run_scenario "$scenario"
fi
