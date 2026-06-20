#!/bin/bash
# Test installing the kRPC C++ client via vcpkg overlay port on Windows.
# Expects the release archive (krpc-cpp-*.zip) in the current directory.
# Usage: test-vcpkg-windows.sh
set -eo pipefail
set -x

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

VCPKG_ROOT="${VCPKG_ROOT:-C:/vcpkg}"
vcpkg_bin="${VCPKG_ROOT}/vcpkg"
toolchain="${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"

# Find the archive downloaded by CI into the current directory
archive=$(ls krpc-cpp-*.zip | head -1)
version=$(echo "$archive" | sed 's/krpc-cpp-\(.*\)\.zip/\1/')
# Strip any dev suffix to get a valid semver for vcpkg.json.
version_semver=$(echo "$version" | grep -oE '^[0-9]+\.[0-9]+\.[0-9]+')

# Create a temporary overlay port that points to the local archive
sha512=$(sha512sum "$archive" | awk '{print $1}')
tmpport=$(mktemp -d)
trap 'rm -rf "$tmpport"' EXIT
cp "$scriptroot/vcpkg-port/"* "$tmpport/"
# On Windows in Git Bash, $(pwd) returns /c/... — file:// + /c/... = file:///c/...
sed -i \
  -e "s|URLS \"https://[^\"]*\"|URLS \"file://$(pwd)/$archive\"|" \
  -e "s|FILENAME \"krpc-cpp-[^\"]*\"|FILENAME \"$archive\"|" \
  -e "s|SHA512 0|SHA512 $sha512|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port into a local directory
"$vcpkg_bin" install krpc:x64-windows --overlay-ports="$tmpport" --x-install-root=vcpkg_installed

# Consumer test
mkdir -p consumer
cat > consumer/main.cpp << 'EOF'
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
int main() {
    // Compile+link test only — no server connection.
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
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  "-DVCPKG_INSTALLED_DIR=$(pwd)/vcpkg_installed" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  -DCMAKE_BUILD_TYPE=Release
cmake --build consumer/build --config Release --parallel
