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

# Install via the overlay port and build the consumer project against it. The consumer opens
# each transport, which proves the winsock library the sockets need reaches a program through the
# package vcpkg installed.
mkdir -p vcpkg-test
"$vcpkg_bin" install "krpc-cnano:x64-windows" --overlay-ports="$tmpport" \
  --x-install-root="vcpkg-test/vcpkg_installed"
cmake -S "$scriptroot/test-consumer" -B "vcpkg-test/consumer" \
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  "-DVCPKG_INSTALLED_DIR=$(pwd)/vcpkg-test/vcpkg_installed" \
  -DVCPKG_TARGET_TRIPLET=x64-windows \
  -DCMAKE_BUILD_TYPE=Release
cmake --build "vcpkg-test/consumer" --config Release --parallel
