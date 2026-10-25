#!/bin/bash
# Build the kRPC for KSA release archives into experimental/ksa/bin/release:
#
#   kRPC.KSA-<version>.zip         the mod folder, with the licenses and an install readme
#   krpc-ksa-stubs-<version>.zip   client stubs and service definitions
#   krpc-ksa-docs-<version>.zip    the HTML documentation
#   SHA256SUMS
set -euo pipefail

here="$(dirname "$(readlink -f "$0")")"
source "$here/versions.sh"
repo="$(dirname "$(dirname "$here")")"
release="$here/bin/release"

# A build without the TestingTools service
"$here/build.sh"
"$here/docs.sh"

rm -rf "$release"
staging="$release/staging"
mkdir -p "$staging/mod" "$staging/docs"

cp -r "$here/bin/kRPC.KSA" "$staging/mod/"
cp "$here/release/LICENSE" "$here/release/LICENSE.System.IO.Ports" \
  "$repo/COPYING" "$repo/COPYING.LESSER" "$staging/mod/"
cp "$repo/tools/build/protobuf/LICENSE" "$staging/mod/LICENSE.Google.Protobuf"
sed -e "s/@VERSION@/$mod_version/g" -e "s/@KRPC_VERSION@/$krpc_version/g" \
  -e "s/@STARMAP_VERSION@/$starmap_version/g" -e "s/@KSA_VERSION@/$ksa_version/g" \
  "$here/release/README.txt" > "$staging/mod/README.txt"
(cd "$staging/mod" && zip -qr "$release/kRPC.KSA-$mod_version.zip" .)

cp -r "$here/bin/docs" "$staging/docs/krpc-ksa-docs-$mod_version"
(cd "$staging/docs" && zip -qr "$release/krpc-ksa-docs-$mod_version.zip" .)

cp "$here/bin/krpc-ksa-stubs-$mod_version.zip" "$release/"
rm -rf "$staging"
(cd "$release" && sha256sum ./*.zip > SHA256SUMS)
echo "Release archives in $release"
