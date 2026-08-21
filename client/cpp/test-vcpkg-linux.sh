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
sha512=$(sha512sum "$archive" | awk '{print $1}')
tmpport=$(mktemp -d)
trap 'rm -rf "$tmpport"' EXIT
cp "$scriptroot/vcpkg-port/"* "$tmpport/"
sed -i \
  -e "s|URLS \"https://[^\"]*\"|URLS \"file://${archive}\"|" \
  -e "s|FILENAME \"krpc-cpp-[^\"]*\"|FILENAME \"krpc-cpp-$version.zip\"|" \
  -e "s|SHA512 0|SHA512 $sha512|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port into a local directory
out=$(pwd)/bazel-bin/client/cpp/test-vcpkg
rm -rf "$out"
mkdir -p "$out"
"$vcpkg_bin" install krpc --overlay-ports="$tmpport" --x-install-root="$out/vcpkg_installed"

# Build the consumer project against it, to verify the package config and targets work
# end-to-end. The program opens a connection over each of the transports the client offers,
# which is what proves both of them reach a program through the package vcpkg installed.
cmake -S "$scriptroot/test-consumer" -B "$out/consumer" \
  "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
  "-DVCPKG_INSTALLED_DIR=$out/vcpkg_installed" \
  -DCMAKE_BUILD_TYPE=Release
cmake --build "$out/consumer" --parallel "$(nproc)"
