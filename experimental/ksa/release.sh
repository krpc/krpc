#!/bin/bash
# Build the kRPC for KSA release archive into experimental/ksa/bin/release:
#
#   kRPC-KSA-<version>.zip   the kRPC mod folder, the client stubs, the example scripts,
#                            the licenses and a readme
#   SHA256SUMS
set -euo pipefail

here="$(dirname "$(readlink -f "$0")")"
source "$here/versions.sh"
repo="$(dirname "$(dirname "$here")")"
release="$here/bin/release"

# A build without the TestingTools service
"$here/build.sh"

rm -rf "$release"
staging="$release/staging"
mkdir -p "$staging"

# The mod folder sits at the top of the archive, where Borea finds the one folder holding a
# mod.toml. Borea installs only that folder, so it carries the licenses too
cp -r "$here/bin/kRPC" "$staging/kRPC"
cp -r "$here/bin/stubs" "$staging/stubs"
mkdir -p "$staging/examples"
cp "$here"/examples/*.py "$staging/examples/"
for dir in "$staging" "$staging/kRPC"; do
  cp "$here/release/LICENSE" "$here/release/LICENSE.System.IO.Ports" \
    "$repo/COPYING" "$repo/COPYING.LESSER" "$dir/"
  cp "$repo/tools/build/protobuf/LICENSE.Google.Protobuf" "$dir/LICENSE.Google.Protobuf"
done
sed -e "s/@VERSION@/$mod_version/g" -e "s/@KRPC_VERSION@/$krpc_version/g" \
  -e "s/@STARMAP_VERSION@/$starmap_version/g" -e "s/@KSA_VERSION@/$ksa_version/g" \
  "$here/release/README.txt" > "$staging/README.txt"
(cd "$staging" && zip -qr "$release/kRPC-KSA-$mod_version.zip" .)

rm -rf "$staging"
(cd "$release" && sha256sum ./*.zip > SHA256SUMS)
echo "Release archive in $release/kRPC-KSA-$mod_version.zip"
