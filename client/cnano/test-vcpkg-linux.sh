#!/bin/bash
# Test installing the kRPC C-nano client via vcpkg overlay port, once per transport it offers.
# Builds the release archive with Bazel, or uses a provided archive.
# Usage: test-vcpkg.sh [/path/to/krpc-cnano-VERSION.zip]
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
  version=$(basename "$archive" .zip | sed 's/krpc-cnano-//')
else
  bazel build //client/cnano:cnano
  bazel_bin=$(bazel info bazel-bin)
  version=$(tools/krpc-version.sh)
  archive="$bazel_bin/client/cnano/krpc-cnano-$version.zip"
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
  -e "s|FILENAME \"krpc-cnano-[^\"]*\"|FILENAME \"krpc-cnano-$version.zip\"|" \
  -e "s|SHA512 0|SHA512 $sha512|" \
  "$tmpport/portfile.cmake"
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$version_semver\"/" "$tmpport/vcpkg.json"

# Install via the overlay port and build the consumer project against it, once per transport the
# port offers. Where the library is built for TCP/IP the consumer opens a connection, which is
# what proves the transport reaches a program through the package vcpkg installed.
out=$(pwd)/bazel-bin/client/cnano/test-vcpkg
rm -rf "$out"
mkdir -p "$out"

function run_scenario {
  local name=$1
  local package=$2
  local dir="$out/$name"
  mkdir -p "$dir"
  "$vcpkg_bin" install "$package" --overlay-ports="$tmpport" \
    --x-install-root="$dir/vcpkg_installed"
  cmake -S "$scriptroot/test-consumer" -B "$dir/consumer" \
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain" \
    "-DVCPKG_INSTALLED_DIR=$dir/vcpkg_installed" \
    -DCMAKE_BUILD_TYPE=Release
  cmake --build "$dir/consumer" --parallel "$(nproc)"
}

run_scenario serialio "krpc-cnano"
run_scenario tcpip "krpc-cnano[tcp]"
