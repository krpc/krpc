#!/bin/bash
# Test building the C-nano client on Windows using CMake with vcpkg.
# Expects the release archive (krpc-cnano-*.zip) in the current directory.
# Usage: test-build-windows.sh
set -eo pipefail
set -x

# Extract the release archive
unzip -q krpc-cnano-*.zip
src=$(ls -d krpc-cnano-*/)

# Configure
cmake -S "$src" -B build \
  -DCMAKE_INSTALL_PREFIX=install \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  2>&1 | tee configure.log
grep -q "Found nanopb" configure.log
! grep -q "Fetching nanopb via FetchContent" configure.log

# Build and install
cmake --build build --config Release --parallel
cmake --install build --config Release

# Consumer test
mkdir -p consumer
cat > consumer/main.c << 'EOF'
#include <stdio.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
int main(void) {
    printf("krpc_cnano library linked OK\n");
    return 0;
}
EOF
cat > consumer/CMakeLists.txt << 'EOF'
cmake_minimum_required(VERSION 3.15)
project(krpc_cnano_consumer_test LANGUAGES C)
set(CMAKE_C_STANDARD 99)
find_package(krpc_cnano CONFIG REQUIRED)
add_executable(test_app main.c)
target_link_libraries(test_app PRIVATE krpc_cnano::krpc_cnano)
EOF
cmake -S consumer -B consumer/build \
  "-DCMAKE_PREFIX_PATH=$(pwd)/install" \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build consumer/build --config Release --parallel
