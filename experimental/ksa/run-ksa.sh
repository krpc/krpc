#!/bin/bash
# Launch KSA through StarMap, on Linux.
#
# StarMap loads the game into its own process and resolves its files from the working
# directory. The game's native libraries are found through LD_LIBRARY_PATH, as dlopen
# does not search the working directory.
#
# KSA_DIR is the game install, defaulting to ../KSA beside the repo. StarMap reads the game
# location from its config, which we write on every launch.
set -euo pipefail

here="$(dirname "$(readlink -f "$0")")"
workspace="$(dirname "$(dirname "$(dirname "$here")")")"
starmap_dir="${STARMAP_DIR:-$workspace/StarMap}"
ksa_dir="$(readlink -f "${KSA_DIR:-$workspace/KSA}")"

cat > "$starmap_dir/StarMapConfig.json" <<EOF
{
  "GameLocation": "$ksa_dir",
  "RepositoryLocation": "",
  "GameArguments": []
}
EOF
echo "KSA $ksa_dir"

cd "$starmap_dir"
export LD_LIBRARY_PATH="$ksa_dir${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec dotnet StarMap.dll "$@"
