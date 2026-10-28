#!/bin/bash
# Generate the logos and banners into media/build. The arguments go to generate.py.
# Its dependencies are installed into a virtualenv in media/build/venv.
set -euo pipefail

here="$(dirname "$(readlink -f "$0")")"
venv="$here/build/venv"
if ! cmp -s "$here/requirements.txt" "$venv/requirements.txt"; then
  rm -rf "$venv"
  python3 -m venv "$venv"
  "$venv/bin/pip" install --quiet --disable-pip-version-check -r "$here/requirements.txt"
  cp "$here/requirements.txt" "$venv/requirements.txt"
fi
exec "$venv/bin/python" "$here/generate.py" "$@"
