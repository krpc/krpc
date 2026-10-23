"""Type-check the documentation example scripts against the typed kRPC client.

Runs pyright over every doc/src/scripts/**/*.py with the krpc package built
from this tree on its search path, so an example that calls a renamed or
removed member, or passes the wrong arguments, fails this test instead of
silently rotting. The inline code blocks in the .rst files are not covered.

The examples deliberately skip None-guards (e.g. on SpaceCenter.active_vessel)
for brevity, so reportOptionalMemberAccess errors are filtered out. pyright
still checks the member against the non-None type, so a renamed or removed
member reached through such an unguarded value fails the test. Passing an
unguarded X | None value where an X is expected is filtered too.
"""

import json
import os
import re
import subprocess
import sys

SCRIPTS_DIR = os.path.join("doc", "src", "scripts")
CONFIG = "pyrightconfig.json"

# (path under doc/src/scripts, pyright rule) pairs that are expected and harmless:
# - DockingGuidance.py imports numpy, which the test does not install, and
#   passes 1 to curses' window.keypad, which typeshed declares as taking a bool.
# - SwitchModes.py adds a handler's exception to a string. In a server side
#   function the handler binds the exception's message, which is a string.
ALLOWED = {
    ("user-guide/server-side-functions/SwitchModes.py", "reportOperatorIssue"),
    ("tutorials/docking-guidance/DockingGuidance.py", "reportMissingImports"),
    ("tutorials/docking-guidance/DockingGuidance.py", "reportArgumentType"),
}

# The extending examples call launch_control and eva, the fictional services
# from extending.rst, which have no generated stubs
FICTIONAL_SERVICE = re.compile(
    r'^Cannot access attribute "(launch_control|eva)" for class "Client"'
)

OPTIONAL_ARG = re.compile(
    r'Argument of type "([\w.]+) \| None" cannot be assigned to parameter '
    r'"\w+" of type "\1"'
)


def main():
    scripts = [
        os.path.abspath(os.path.join(dirpath, f))
        for dirpath, _, filenames in os.walk(SCRIPTS_DIR)
        for f in filenames
        if f.endswith(".py")
    ]
    if not scripts:
        print("no example scripts found under " + SCRIPTS_DIR)
        return 1

    with open(CONFIG) as f:
        config = json.load(f)
    # The scripts are checked for one platform, so that a member typeshed
    # only declares on some of them, such as socket.AF_UNIX, resolves the
    # same way wherever the test runs
    config["pythonPlatform"] = "Linux"
    config["extraPaths"] = [path for path in sys.path if os.path.isdir(path)]
    config["include"] = scripts
    project = os.path.join(os.environ.get("TEST_TMPDIR", "."), "pyright")
    os.makedirs(project, exist_ok=True)
    with open(os.path.join(project, "pyrightconfig.json"), "w") as f:
        json.dump(config, f)

    result = subprocess.run(
        [sys.executable, "-m", "basedpyright", "--project", project, "--outputjson"],
        capture_output=True,
        text=True,
        check=False,
    )
    # 0 is a clean run and 1 is "errors found" (handled below); anything else
    # means pyright did not check the scripts
    if result.returncode not in (0, 1):
        print(result.stdout)
        print(result.stderr)
        print("pyright failed to run (exit status %d)" % result.returncode)
        return 1

    failures = []
    for diagnostic in json.loads(result.stdout)["generalDiagnostics"]:
        if diagnostic["severity"] != "error":
            continue
        rule = diagnostic.get("rule", "")
        message = diagnostic["message"]
        if rule == "reportOptionalMemberAccess" or OPTIONAL_ARG.search(message):
            continue
        # The path is in the runfiles or, through a symlink, the source tree
        path = diagnostic["file"].replace(os.sep, "/")
        name = path.split("doc/src/scripts/", 1)[-1]
        if (name, rule) in ALLOWED:
            continue
        if (
            name.startswith("extending/")
            and rule == "reportAttributeAccessIssue"
            and FICTIONAL_SERVICE.search(message)
        ):
            continue
        line = diagnostic["range"]["start"]["line"] + 1
        failures.append("%s:%d: %s (%s)" % (name, line, message, rule))

    if failures:
        print("%d error(s) in the example scripts:" % len(failures))
        print("\n".join(failures))
        return 1
    print("checked %d example scripts" % len(scripts))
    return 0


if __name__ == "__main__":
    sys.exit(main())
