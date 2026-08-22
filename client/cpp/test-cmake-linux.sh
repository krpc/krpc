#!/bin/bash
# Test building the C++ client using CMake.
# Accepts the release archive as the first argument.
# Covers how the dependencies are provided:
#   system) system-installed protobuf + ASIO
#   fetch)  protobuf + ASIO + abseil fetched via FetchContent (KRPC_FETCH_DEPS=ON)
# Each is followed by a consumer test using find_package(krpc CONFIG REQUIRED).
# Usage: test-cmake-linux.sh ARCHIVE [SCENARIO]  (default: run all)
set -e
set -o pipefail
set -x
set -o functrace

scenarios="system fetch"

archive="$(realpath "${1:?Usage: test-cmake-linux.sh ARCHIVE [$(echo $scenarios | tr ' ' '|')]}")"
scenario="${2:-all}"

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$scriptroot/../.."

root=$(pwd)
out=$root/bazel-bin/client/cpp/test-build
version=$(basename "$archive" .zip | sed 's/krpc-cpp-//')

# Extract the release archive
rm -rf "$out"
mkdir -p "$out"
unzip -q "$archive" -d "$out"
mv "$out/krpc-cpp-$version"/* "$out/"
rm -r "$out/krpc-cpp-$version"

# Configure krpc; save cmake output to log_file for later verification.
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

# Build and install the krpc library.
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
# targets work end-to-end. The program opens a connection over each of the transports the client
# offers, which is what proves both of them reach it through the package.
function consumer_test {
  local install_dir=$1
  local build_dir=$2
  cmake -S "$scriptroot/test-consumer" -B "$build_dir" \
    -DCMAKE_PREFIX_PATH="$install_dir" \
    -DCMAKE_BUILD_TYPE=Release
  cmake --build "$build_dir" --parallel $(nproc)
}

# Build, install and consume the library for one scenario, named for how its dependencies are
# provided.
function run_scenario {
  local name=$1
  local dir="$out/$name"
  local log="$dir/configure.log"
  local options=()

  case "$name" in
    fetch)  options+=(-DKRPC_FETCH_DEPS=ON) ;;
    system) ;;
    *) echo "unknown scenario '$name'"; exit 1 ;;
  esac

  build_install "$dir/build" "$dir/install" "$log" "${options[@]}"

  # Which of the two ways of providing the dependencies the configure step actually took
  case "$name" in
    fetch)
      check_present "$log" "Fetching protobuf via FetchContent"
      check_present "$log" "Fetching ASIO via FetchContent"
      check_absent  "$log" "Found protobuf"
      ;;
    system)
      check_present "$log" "Found protobuf"
      check_absent  "$log" "Fetching protobuf via FetchContent"
      check_absent  "$log" "Fetching ASIO via FetchContent"
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
