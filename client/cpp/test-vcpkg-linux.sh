#!/bin/bash
# Test installing the kRPC C++ client via vcpkg overlay port.
# Builds the release archive with Bazel, or uses a provided archive.
# Usage: test-vcpkg.sh [/path/to/krpc-cpp-VERSION.zip]
# Requires: VCPKG_ROOT environment variable pointing to a vcpkg installation.
set -e
set -o pipefail
set -x
set -o functrace

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$scriptroot/../.."

if [ -z "${VCPKG_ROOT:-}" ]; then
  echo "Error: VCPKG_ROOT is not set. Set it to your vcpkg installation directory." >&2
  exit 1
fi
vcpkg_bin="${VCPKG_ROOT}/vcpkg"
if [ ! -x "$vcpkg_bin" ]; then
  echo "Error: vcpkg executable not found at $vcpkg_bin" >&2
  exit 1
fi
toolchain="${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"

# Use a provided archive or build one with Bazel
if [ -n "${1:-}" ]; then
  archive="$(realpath "$1")"
  version=$(basename "$archive" .zip | sed 's/krpc-cpp-//')
else
  bazel build //client/cpp:cpp
  bazel_bin=$(bazel info bazel-bin)
  version=$(tools/krpc-version.sh)
  archive="$bazel_bin/client/cpp/krpc-cpp-$version.zip"
fi
# Strip any dev suffix (e.g. 0.5.4-12345-abc) to get a valid semver for vcpkg.json.
version_semver=$(echo "$version" | grep -oE '^[0-9]+\.[0-9]+\.[0-9]+')

# Create a temporary overlay port that points to the local archive
tmpport=$(mktemp -d)
trap 'rm -rf "$tmpport"' EXIT
cp "$scriptroot/vcpkg-port/"* "$tmpport/"
sed -i \
  -e "s|URLS \"https://[^\"]*\"|URLS \"file://${archive}\"|" \
  -e "s|FILENAME \"krpc-cpp-[^\"]*\"|FILENAME \"krpc-cpp-$version.zip\"|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port (SHA512 0 skips hash verification)
"$vcpkg_bin" install krpc --overlay-ports="$tmpport"

# Consumer test: a small project that uses find_package(krpc CONFIG REQUIRED)
out=$(pwd)/bazel-bin/client/cpp/test-vcpkg
rm -rf "$out"
mkdir -p "$out/consumer"

cat > "$out/consumer/main.cpp" << 'EOF'
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
int main() {
    // Compile+link test only — no server connection.
    std::cout << "krpc library linked OK" << std::endl;
    return 0;
}
EOF

cat > "$out/consumer/CMakeLists.txt" << 'EOF'
cmake_minimum_required(VERSION 3.15)
project(krpc_consumer_test LANGUAGES CXX)
set(CMAKE_CXX_STANDARD 17)
find_package(krpc CONFIG REQUIRED)
add_executable(test_app main.cpp)
target_link_libraries(test_app PRIVATE krpc::krpc)
EOF

mkdir -p "$out/consumer/build"
cmake -S "$out/consumer" -B "$out/consumer/build" \
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  -DCMAKE_BUILD_TYPE=Release
cmake --build "$out/consumer/build" --parallel "$(nproc)"
