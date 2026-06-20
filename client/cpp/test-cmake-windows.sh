#!/bin/bash
# Test building the C++ client on Windows using CMake with vcpkg.
# Expects the release archive (krpc-cpp-*.zip) in the current directory.
# Usage: test-build-windows.sh
set -eo pipefail
set -x

# Extract the release archive
unzip -q krpc-cpp-*.zip
src=$(ls -d krpc-cpp-*/)

# Configure
cmake -S "$src" -B build \
  -DCMAKE_INSTALL_PREFIX=install \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  2>&1 | tee configure.log
grep -q "Found protobuf" configure.log
! grep -q "Fetching protobuf via FetchContent" configure.log
! grep -q "Fetching ASIO via FetchContent" configure.log

# Build and install
cmake --build build --config Release --parallel
cmake --install build --config Release

# Consumer test
mkdir -p consumer
cat > consumer/main.cpp << 'EOF'
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
int main() {
    std::cout << "krpc library linked OK" << std::endl;
    return 0;
}
EOF
cat > consumer/CMakeLists.txt << 'EOF'
cmake_minimum_required(VERSION 3.15)
project(krpc_consumer_test LANGUAGES CXX)
set(CMAKE_CXX_STANDARD 17)
find_package(krpc CONFIG REQUIRED)
add_executable(test_app main.cpp)
target_link_libraries(test_app PRIVATE krpc::krpc)
EOF
cmake -S consumer -B consumer/build \
  "-DCMAKE_PREFIX_PATH=$(pwd)/install" \
  -DCMAKE_BUILD_TYPE=Release \
  "-DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build consumer/build --config Release --parallel
