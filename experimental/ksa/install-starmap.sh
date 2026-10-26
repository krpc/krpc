#!/bin/bash
# Download StarMap and point it at the KSA install.
#
# STARMAP_DIR is the install folder, defaulting to ../StarMap beside the repo.
# KSA_DIR is the game install, defaulting to ../KSA beside the repo.
set -euo pipefail

here="$(dirname "$(readlink -f "$0")")"
source "$here/versions.sh"
url="https://github.com/StarMapLoader/StarMap/releases/download/$starmap_version/StarMap-$starmap_version.zip"
workspace="$(dirname "$(dirname "$(dirname "$here")")")"
starmap_dir="${STARMAP_DIR:-$workspace/StarMap}"
ksa_dir="$(readlink -f "${KSA_DIR:-$workspace/KSA}")"

if [ ! -f "$ksa_dir/KSA.dll" ]; then
  echo "KSA.dll not found in $ksa_dir. Set KSA_DIR to the KSA install." >&2
  exit 1
fi

zip="$(mktemp --suffix=.zip)"
trap 'rm -f "$zip"' EXIT
curl -sSfL -o "$zip" "$url"
echo "$starmap_sha256  $zip" | sha256sum --check --quiet

rm -rf "$starmap_dir"
mkdir -p "$starmap_dir"
unzip -q "$zip" -d "$starmap_dir"
cat > "$starmap_dir/StarMapConfig.json" <<EOF
{
  "GameLocation": "$ksa_dir",
  "RepositoryLocation": "",
  "GameArguments": []
}
EOF
echo "Installed StarMap $starmap_version into $starmap_dir"
