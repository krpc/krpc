#!/bin/bash
# Test installing the kRPC C-nano client via vcpkg overlay port on Windows, once per
# transport it offers.
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
sha512=$(sha512sum "$archive" | awk '{print $1}')
tmpport=$(mktemp -d)
trap 'rm -rf "$tmpport"' EXIT
cp "$scriptroot/vcpkg-port/"* "$tmpport/"
# cygpath -m converts /d/a/... to D:/a/... so the file:// URL has the drive letter colon.
archive_url="file:///$(cygpath -m "$(pwd)/$archive")"
sed -i \
  -e "s|URLS \"https://[^\"]*\"|URLS \"$archive_url\"|" \
  -e "s|FILENAME \"krpc-cnano-[^\"]*\"|FILENAME \"$archive\"|" \
  -e "s|SHA512 0|SHA512 $sha512|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port and build the consumer project against it, once per transport the
# port offers. Where the library is built for TCP/IP the consumer opens a connection, which is
# what proves the transport, and the winsock library it needs, reach a program through the
# package vcpkg installed.
function run_scenario {
  local name=$1
  local package=$2
  mkdir -p "$name"
  "$vcpkg_bin" install "$package" --overlay-ports="$tmpport" \
    --x-install-root="$name/vcpkg_installed"
  cmake -S "$scriptroot/test-consumer" -B "$name/consumer" \
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
    "-DVCPKG_INSTALLED_DIR=$(pwd)/$name/vcpkg_installed" \
    -DVCPKG_TARGET_TRIPLET=x64-windows \
    -DCMAKE_BUILD_TYPE=Release
  cmake --build "$name/consumer" --config Release --parallel
}

run_scenario serialio "krpc-cnano:x64-windows"
run_scenario tcpip "krpc-cnano[tcp]:x64-windows"
