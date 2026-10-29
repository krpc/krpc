#!/bin/bash
# Build the kRPC for KSA mod into experimental/ksa/bin/kRPC.
#
#   build.sh            build the mod folder
#   build.sh --install  also copy it into the KSA mods folder and enable it
#   build.sh --testing  also build the TestingTools service, for the tests
#
# The mod and the client stubs are built from a kRPC release: the server core and
# the code generator come from its krpctools package, installed into a virtualenv.
#
# KSA_DIR is the game install, defaulting to ../KSA beside the repo.
# KSA_MODS_DIR is the user mods folder, defaulting to
# "<Documents>/My Games/Kitten Space Agency/mods".
set -euo pipefail


here="$(dirname "$(readlink -f "$0")")"
source "$here/versions.sh"
out="$here/bin/kRPC"
ksa_dir="$(readlink -f "${KSA_DIR:-$(dirname "$(dirname "$(dirname "$here")")")/KSA}")"

install=0
projects=(server/KRPC.KSA.csproj service/KRPC.KSA.Service.csproj)
for arg in "$@"; do
  case "$arg" in
    --install) install=1 ;;
    --testing) projects+=(testing/KRPC.KSA.TestingTools.csproj) ;;
    *) echo "unknown argument: $arg" >&2; exit 1 ;;
  esac
done

venv="$here/bin/venv"
if [ "$("$venv/bin/krpc-clientgen" --version 2>/dev/null)" != "krpc-clientgen version v$krpc_version" ]; then
  rm -rf "$venv"
  python3 -m venv "$venv"
  "$venv/bin/pip" install --quiet --disable-pip-version-check "krpctools==$krpc_version"
fi
# The .NET builds of KRPC.Core and the ServiceDefinitions tool
krpc_bin="$("$venv/bin/python" -c 'from importlib.resources import files; print(files("krpctools") / "bin")')"
chmod +x "$krpc_bin/ServiceDefinitions"

rm -rf "$out"
for project in "${projects[@]}"; do
  dotnet build "$here/$project" -c Release -warnaserror -o "$out" -p:KSADir="$ksa_dir/" -p:KRPCCoreDll="$krpc_bin/KRPC.Core.dll"
done
# The server reads the API documentation from the XML file beside each assembly
cp "$krpc_bin/KRPC.Core.xml" "$out/"
rm -f "$out"/*.pdb

# Generate the service definitions, which runs the server's service scan over the
# service and checks its documentation
"$krpc_bin/ServiceDefinitions" \
  --output="$here/bin/KRPC.KSA.json" --reference-dir="$ksa_dir" --reference-dir="$out" \
  KSA "$out/KRPC.KSA.Service.dll"

# Client stubs, at the paths each client includes them from
stubs="$here/bin/stubs"
rm -rf "$stubs"
mkdir -p "$stubs"
declare -A stub_paths=(
  [csharp]="csharp/KSA.cs"
  [cpp]="cpp/include/krpc/services/ksa.hpp"
  [cnano]="cnano/include/krpc_cnano/services/ksa.h"
  [java]="java/krpc/client/services/KSA.java"
)
for language in "${!stub_paths[@]}"; do
  path="$stubs/${stub_paths[$language]}"
  mkdir -p "$(dirname "$path")"
  "$venv/bin/krpc-clientgen" "$language" KSA "$here/bin/KRPC.KSA.json" --output="$path"
done
sed "s/@CLIENT_VERSION@/$krpc_version/" "$here/stubs/README.md" > "$stubs/README.md"
cp "$here/bin/KRPC.KSA.json" "$stubs/"
echo "Client stubs in $stubs"

if [ "$install" = 1 ]; then
  if [ -z "${KSA_MODS_DIR:-}" ]; then
    documents="$(xdg-user-dir DOCUMENTS 2>/dev/null || echo "$HOME/Documents")"
    KSA_MODS_DIR="$documents/My Games/Kitten Space Agency/mods"
  fi
  manifest="$(dirname "$KSA_MODS_DIR")/manifest.toml"
  rm -rf "$KSA_MODS_DIR/kRPC"
  mkdir -p "$KSA_MODS_DIR"
  cp -r "$out" "$KSA_MODS_DIR/"
  if ! grep -q 'id = "kRPC"' "$manifest" 2>/dev/null; then
    printf '\n[[mods]]\nid = "kRPC"\nenabled = true\n' >> "$manifest"
  fi
  echo "Installed into $KSA_MODS_DIR/kRPC"
fi
