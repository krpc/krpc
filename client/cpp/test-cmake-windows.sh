#!/bin/bash
# Test building the C++ client on Windows using CMake with vcpkg.
# Expects the release archive (krpc-cpp-*.zip) in the current directory.
# The dependencies come from vcpkg, so this covers the system scenario alone; the fetched
# one is covered on Linux, where FetchContent takes the same path whatever the platform.
# Followed by a consumer test using find_package(krpc CONFIG REQUIRED).
# Usage: test-cmake-windows.sh
set -eo pipefail
set -x

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Extract the release archive
unzip -q krpc-cpp-*.zip
src=$(ls -d krpc-cpp-*/)

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

# Configure
cmake -S "$src" -B build \
  -DCMAKE_INSTALL_PREFIX=install \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  2>&1 | tee configure.log
check_present configure.log "Found protobuf"
check_absent  configure.log "Fetching protobuf via FetchContent"
check_absent  configure.log "Fetching ASIO via FetchContent"

# Build and install
cmake --build build --config Release --parallel
cmake --install build --config Release

# Build the consumer project against the installed package, to verify the package config and
# targets work end-to-end. The program opens a connection over each of the transports the
# client offers, which is what proves both of them reach it through the package.
cmake -S "$scriptroot/test-consumer" -B consumer \
  "-DCMAKE_PREFIX_PATH=$(pwd)/install" \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build consumer --config Release --parallel
