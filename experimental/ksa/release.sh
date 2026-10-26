#!/bin/bash
# Build the kRPC for KSA release archive into experimental/ksa/bin/release:
#
#   kRPC-KSA-<version>.zip   one kRPC-KSA-<version> folder, holding the kRPC.KSA mod folder,
#                            the client stubs, the example scripts, the licenses and a readme
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
top="$staging/kRPC-KSA-$mod_version"
mkdir -p "$top"

cp -r "$here/bin/kRPC.KSA" "$top/kRPC.KSA"
cp -r "$here/bin/stubs" "$top/stubs"
mkdir -p "$top/examples"
cp "$here"/examples/*.py "$top/examples/"
cp "$here/release/LICENSE" "$here/release/LICENSE.System.IO.Ports" \
  "$repo/COPYING" "$repo/COPYING.LESSER" "$top/"
cp "$repo/tools/build/protobuf/LICENSE" "$top/LICENSE.Google.Protobuf"
sed -e "s/@VERSION@/$mod_version/g" -e "s/@KRPC_VERSION@/$krpc_version/g" \
  -e "s/@STARMAP_VERSION@/$starmap_version/g" -e "s/@KSA_VERSION@/$ksa_version/g" \
  "$here/release/README.txt" > "$top/README.txt"
(cd "$staging" && zip -qr "$release/kRPC-KSA-$mod_version.zip" "kRPC-KSA-$mod_version")

rm -rf "$staging"
(cd "$release" && sha256sum ./*.zip > SHA256SUMS)
echo "Release archive in $release/kRPC-KSA-$mod_version.zip"
