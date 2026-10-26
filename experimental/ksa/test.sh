#!/bin/bash
# Run the KSA integration tests. Builds and installs the mod with the TestingTools
# service, then runs pytest over test/. Arguments go to pytest, for example:
#
#   test.sh                                  launch KSA, run every test, quit
#   test.sh test/test_orbit.py -v            one file
#   test.sh -k test_rename                   by name
#   test.sh --no-launch                      against a game started with run-ksa.sh
#
# A launched game is saved as krpctest after loading, and each test class loads that
# save. --no-launch needs that save, from an earlier launched run.
set -euo pipefail


here="$(dirname "$(readlink -f "$0")")"
source "$here/versions.sh"

"$here/build.sh" --install --testing

venv="$here/bin/test-venv"
if [ "$("$venv/bin/python" -c 'import krpc; print(krpc.__version__)' 2>/dev/null)" != "$krpc_version" ]; then
  rm -rf "$venv"
  python3 -m venv "$venv"
  "$venv/bin/pip" install --quiet --disable-pip-version-check "krpc==$krpc_version" pytest
fi

cd "$here"
exec "$venv/bin/python" -m pytest -p no:cacheprovider -c test/pytest.ini "$@"
