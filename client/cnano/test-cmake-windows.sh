#!/bin/bash
# Test building the C-nano client on Windows using CMake with vcpkg.
# Expects the release archive (krpc-cnano-*.zip) in the current directory.
# Covers the two things a build chooses between, crossed with each other:
#   how nanopb is provided) system) the nanopb vcpkg installs
#                           fetch)  nanopb fetched via FetchContent (KRPC_FETCH_DEPS=ON)
#   which transport)        serialio)    a serial port, reached through the Windows file API
#                           tcpip)       TCP/IP, through winsock (KRPC_COMMUNICATION_TCP=ON)
#                           localsocket) a unix domain socket, also through winsock
#                                        (KRPC_COMMUNICATION_LOCALSOCKET=ON)
# Each is followed by a consumer test using find_package(krpc_cnano CONFIG REQUIRED).
# Usage: test-cmake-windows.sh [SCENARIO]  (default: run all)
set -eo pipefail
set -x

scenarios="system-serialio system-tcpip system-localsocket \
fetch-serialio fetch-tcpip fetch-localsocket"
scenario="${1:-all}"

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Extract the release archive, over any tree an earlier scenario left behind
unzip -o -q krpc-cnano-*.zip
src=$(ls -d krpc-cnano-*/)

root=$(pwd)
toolchain="C:/vcpkg/scripts/buildsystems/vcpkg.cmake"

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

# Build and install the library, then build the consumer project against the installed package
# with find_package(krpc_cnano CONFIG REQUIRED), to verify the package config and targets work
# end-to-end. Where the library is built for a socket the program opens a connection, which is
# what proves the transport, and the winsock library it needs, reach it through the package.
function run_scenario {
  local name=$1
  local out="$root/$name"
  local log="$out/configure.log"
  local options=()
  mkdir -p "$out"

  case "$name" in
    fetch-*)  options+=(-DKRPC_FETCH_DEPS=ON) ;;
    system-*) ;;
    *) echo "unknown scenario '$name'"; exit 1 ;;
  esac
  case "$name" in
    *-tcpip)       options+=(-DKRPC_COMMUNICATION_TCP=ON) ;;
    *-localsocket) options+=(-DKRPC_COMMUNICATION_LOCALSOCKET=ON) ;;
    *-serialio) ;;
    *) echo "unknown scenario '$name'"; exit 1 ;;
  esac

  # Configure, build and install the library
  cmake -S "$src" -B "$out/build" \
    -DCMAKE_INSTALL_PREFIX="$out/install" \
    -DCMAKE_BUILD_TYPE=Release \
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
    -DVCPKG_TARGET_TRIPLET=x64-windows \
    "${options[@]}" 2>&1 | tee "$log"

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

  cmake --build "$out/build" --config Release --parallel
  cmake --install "$out/build" --config Release

  # Consumer test
  cmake -S "$scriptroot/test-consumer" -B "$out/consumer" \
    "-DCMAKE_PREFIX_PATH=$out/install" \
    -DCMAKE_BUILD_TYPE=Release \
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
    -DVCPKG_TARGET_TRIPLET=x64-windows
  cmake --build "$out/consumer" --config Release --parallel
}

if [[ "$scenario" == "all" ]]; then
  for name in $scenarios; do
    run_scenario "$name"
  done
else
  run_scenario "$scenario"
fi
