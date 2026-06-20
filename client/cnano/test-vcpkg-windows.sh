#!/bin/bash
# Test installing the kRPC C-nano client via vcpkg overlay port on Windows.
# Expects the release archive (krpc-cnano-*.zip) in the current directory.
# Usage: test-vcpkg-windows.sh
set -eo pipefail
set -x

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

VCPKG_ROOT="${VCPKG_ROOT:-C:/vcpkg}"
vcpkg_bin="${VCPKG_ROOT}/vcpkg"
toolchain="${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"

# Find the archive downloaded by CI into the current directory
archive=$(ls krpc-cnano-*.zip | head -1)
version=$(echo "$archive" | sed 's/krpc-cnano-\(.*\)\.zip/\1/')
# Strip any dev suffix to get a valid semver for vcpkg.json.
version_semver=$(echo "$version" | grep -oE '^[0-9]+\.[0-9]+\.[0-9]+')

# Create a temporary overlay port that points to the local archive
tmpport=$(mktemp -d)
trap 'rm -rf "$tmpport"' EXIT
cp "$scriptroot/vcpkg-port/"* "$tmpport/"
# On Windows in Git Bash, $(pwd) returns /c/... — file:// + /c/... = file:///c/...
sed -i \
  -e "s|URLS \"https://[^\"]*\"|URLS \"file://$(pwd)/$archive\"|" \
  -e "s|FILENAME \"krpc-cnano-[^\"]*\"|FILENAME \"$archive\"|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port (SHA512 0 skips hash verification)
"$vcpkg_bin" install krpc-cnano:x64-windows --overlay-ports="$tmpport"

# Consumer test
mkdir -p consumer
cat > consumer/main.c << 'EOF'
#include <stdio.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
int main(void) {
    /* Compile+link test only — no server connection. */
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
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  -DCMAKE_BUILD_TYPE=Release
cmake --build consumer/build --config Release --parallel
