"""Type-check the documentation example scripts against the typed kRPC client.

Runs mypy over every doc/src/scripts/**/*.py with MYPYPATH pointing at the
krpc package built from this tree, so an example that calls a renamed or
removed member, or passes the wrong arguments, fails this test instead of
silently rotting. The inline code blocks in the .rst files and the Lua
examples are not covered.

The examples deliberately skip None-guards (e.g. on SpaceCenter.active_vessel)
for brevity, so 'Item "None" of ...' union-attr errors are filtered out. The
filter is deliberately that narrow: a renamed or removed member reached
through such an unguarded value is reported against the non-None item (e.g.
'Item "Vessel" of "Vessel | None" has no attribute ...') and still fails the
test. Do not filter the union-attr error code wholesale; disabling it makes
mypy infer 'X | Any' for attribute access through the union, and the Any
member then hides all drift further down the chain.
"""

import importlib.util
import os
import re
import sys

from mypy import api

SCRIPTS_DIR = os.path.join("doc", "src", "scripts")

# (file basename, mypy error code) pairs that are expected and harmless:
# - ServiceAPIExample.py calls conn.launch_control, the fictional example
#   service from extending.rst, which has no generated stub.
# - DockingGuidance.py passes 1 to curses' window.keypad, which typeshed
#   declares as taking a bool.
ALLOWED = {
    ("ServiceAPIExample.py", "attr-defined"),
    ("DockingGuidance.py", "arg-type"),
}

ERROR_LINE = re.compile(r"^(?P<path>[^:]+):\d+: error: .*\[(?P<code>[a-z-]+)\]$")

# The other shape the skipped None-guards produce: passing an unguarded
# X | None value where an X is expected
OPTIONAL_ARG = re.compile(r'has incompatible type "([\w.]+) \| None"; expected "\1"')


def main():
    spec = importlib.util.find_spec("krpc")
    if spec is None or spec.origin is None:
        print("could not locate the krpc package in the test's runfiles")
        return 1
    krpc_pkg_root = os.path.dirname(os.path.dirname(spec.origin))

    scripts = []
    for dirpath, _, filenames in os.walk(SCRIPTS_DIR):
        scripts.extend(
            os.path.join(dirpath, f)
            for f in filenames
            # skip the empty __init__.py files rules_python adds to runfiles
            if f.endswith(".py") and f != "__init__.py"
        )
    if not scripts:
        print("no example scripts found under " + SCRIPTS_DIR)
        return 1

    os.environ["MYPYPATH"] = krpc_pkg_root
    cache_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), "mypy_cache")
    stdout, stderr, status = api.run(
        [
            "--ignore-missing-imports",
            "--follow-imports=silent",
            "--no-error-summary",
            "--cache-dir",
            cache_dir,
        ]
        + sorted(scripts)
    )

    # 0 is a clean run and 1 is "type errors found" (handled below); anything
    # else means mypy did not actually check the scripts
    if status not in (0, 1):
        print(stdout)
        print(stderr)
        print("mypy failed to run (exit status %d)" % status)
        return 1

    failures = []
    for line in stdout.splitlines():
        match = ERROR_LINE.match(line)
        if match is None:
            continue
        if ': error: Item "None" of ' in line:
            continue
        if OPTIONAL_ARG.search(line):
            continue
        key = (os.path.basename(match.group("path")), match.group("code"))
        if key not in ALLOWED:
            failures.append(line)

    if stderr:
        print(stderr)
    if failures:
        print("%d error(s) in the example scripts:" % len(failures))
        print("\n".join(failures))
        return 1
    print("checked %d example scripts" % len(scripts))
    return 0


if __name__ == "__main__":
    sys.exit(main())
