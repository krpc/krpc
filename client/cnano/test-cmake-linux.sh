#!/bin/bash
# Test building the C-nano client using CMake.
# Accepts the release archive as the first argument.
# Runs CMake build scenario(s):
#   system) system-installed nanopb
#   fetch)  nanopb fetched via FetchContent (KRPC_FETCH_DEPS=ON)
# Each is followed by a consumer test using find_package(krpc_cnano CONFIG REQUIRED).
# Usage: test-cmake-linux.sh ARCHIVE [system|fetch]  (default: run both)
set -e
set -o pipefail
set -x
set -o functrace

archive="$(realpath "${1:?Usage: test-cmake-linux.sh ARCHIVE [system|fetch]}")"
mode="${2:-all}"

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

# Build a small consumer project that uses find_package(krpc_cnano CONFIG REQUIRED)
# to verify the installed package config and targets work end-to-end.
function consumer_test {
  local install_dir=$1
  local test_dir=$2
  mkdir -p "$test_dir"

  cat > "$test_dir/main.c" << 'EOF'
#include <stdio.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
int main(void) {
    /* Compile+link test only — no server connection. */
    printf("krpc_cnano library linked OK\n");
    return 0;
}
EOF

  cat > "$test_dir/CMakeLists.txt" << 'EOF'
cmake_minimum_required(VERSION 3.15)
project(krpc_cnano_consumer_test LANGUAGES C)
set(CMAKE_C_STANDARD 99)
find_package(krpc_cnano CONFIG REQUIRED)
add_executable(test_app main.c)
target_link_libraries(test_app PRIVATE krpc_cnano::krpc_cnano)
EOF

  mkdir -p "$test_dir/build"
  cmake -S "$test_dir" -B "$test_dir/build" \
    -DCMAKE_PREFIX_PATH="$install_dir" \
    -DCMAKE_BUILD_TYPE=Release
  cmake --build "$test_dir/build" --parallel $(nproc)
}

# 1) System-installed nanopb
if [[ "$mode" == "system" || "$mode" == "all" ]]; then
  build_install "$out/system/build" "$out/system/install" "$out/system/configure.log"
  check_present "$out/system/configure.log" "Found nanopb"
  check_absent  "$out/system/configure.log" "Fetching nanopb via FetchContent"
  consumer_test "$out/system/install" "$out/system/consumer"
fi

# 2) FetchContent nanopb via global option
if [[ "$mode" == "fetch" || "$mode" == "all" ]]; then
  build_install "$out/fetch/build" "$out/fetch/install" "$out/fetch/configure.log" \
    -DKRPC_FETCH_DEPS=ON
  check_present "$out/fetch/configure.log" "Fetching nanopb via FetchContent"
  check_absent  "$out/fetch/configure.log" "Found nanopb"
  consumer_test "$out/fetch/install" "$out/fetch/consumer"
fi
