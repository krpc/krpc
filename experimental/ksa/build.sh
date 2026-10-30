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
dotnet build "$here/grpc/KRPC.KSA.Grpc.csproj" -c Release -warnaserror -o "$out/grpc" -p:KSADir="$ksa_dir/" -p:KRPCCoreDll="$krpc_bin/KRPC.Core.dll"
# KSA ships no ASP.NET Core, which the gRPC server runs on, so the mod carries the
# assemblies of its runtime pack for each platform, in grpc/<rid>
nupkg="$(mktemp --suffix=.nupkg)"
trap 'rm -f "$nupkg"' EXIT
for rid in win-x64 linux-x64; do
  aspnetcore="$here/bin/aspnetcore-$aspnetcore_version-$rid"
  if [ ! -d "$aspnetcore" ]; then
    package="microsoft.aspnetcore.app.runtime.$rid"
    sha256_var="aspnetcore_${rid//-/_}_sha256"
    curl -sSfL -o "$nupkg" \
      "https://api.nuget.org/v3-flatcontainer/$package/$aspnetcore_version/$package.$aspnetcore_version.nupkg"
    echo "${!sha256_var}  $nupkg" | sha256sum --check --quiet
    rm -rf "$aspnetcore.tmp"
    unzip -q "$nupkg" "runtimes/$rid/lib/net10.0/*.dll" -d "$aspnetcore.tmp"
    mv "$aspnetcore.tmp/runtimes/$rid/lib/net10.0" "$aspnetcore"
    rm -rf "$aspnetcore.tmp"
  fi
  mkdir -p "$out/grpc/$rid"
  cp "$aspnetcore"/*.dll "$out/grpc/$rid/"
done
# The server reads the API documentation from the XML file beside each assembly
cp "$krpc_bin/KRPC.Core.xml" "$out/"
if [ -f "$out/KRPC.KSA.TestingTools.dll" ]; then
  cp "$here/testing/settings.toml" "$out/"
fi
rm -f "$out"/*.pdb "$out"/grpc/*.pdb "$out"/grpc/*.deps.json "$out"/grpc/*.runtimeconfig.json

# Generate the service definitions, which runs the server's service scan over the
# service and checks its documentation
"$krpc_bin/ServiceDefinitions" \
  --output="$here/bin/KRPC.KSA.json" --reference-dir="$ksa_dir" --reference-dir="$out" \
  KSA "$out/KRPC.KSA.Service.dll"
# The definitions of the StdLib structures the service uses, such as the colors
"$krpc_bin/ServiceDefinitions" --output="$here/bin/StdLib.json" StdLib "$krpc_bin/KRPC.Core.dll"

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
# The StdLib headers of the C++ and C nano clients
declare -A stdlib_includes=(
  [cpp]="--include=StdLib=krpc/services/std_lib.hpp"
  [cnano]="--include=StdLib=krpc_cnano/services/std_lib.h"
)
for language in "${!stub_paths[@]}"; do
  path="$stubs/${stub_paths[$language]}"
  mkdir -p "$(dirname "$path")"
  "$venv/bin/krpc-clientgen" "$language" KSA "$here/bin/KRPC.KSA.json" "$here/bin/StdLib.json" --output="$path" ${stdlib_includes[$language]:-}
done
python3 "$here/grpc/protogen.py" "$here/bin/KRPC.KSA.json" KSA "$stubs/grpc/KSA.proto"
python3 "$here/grpc/protogen.py" "$here/bin/StdLib.json" StdLib "$stubs/grpc/StdLib.proto"
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
