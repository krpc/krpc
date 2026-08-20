"""Runs a checking sphinx builder over the documentation sources.

  run_sphinx_test.py CONFIG

CONFIG is the json file written by the sphinx_spelling_test and
sphinx_linkcheck_test rules, naming the sphinx-build launcher and the staged
source directory, both runfiles-root-relative, the builder to run and the
-D options to give it.

The spelling builder reports each misspelling as a warning, so it is run with
-W to make one fail the test. The link checker collects every link it could not
follow into a report of its own, which is printed so that a failure says which
links they were.
"""

import argparse
import json
import os
import subprocess
import sys

# The words the spelling builder is to accept beyond the ones its dictionaries
# already hold. sphinxcontrib-spelling requires this to be a readable, writable
# regular file, and Bazel stages a source as a read-only symlink.
DICTIONARY = "dictionary.txt"

# Where the link checker lists the links it could not follow.
LINKCHECK_REPORT = "output.txt"


def make_writable(path):
    """Replace a staged symlink with a writable copy of what it points at."""
    with open(path, "rb") as staged:
        content = staged.read()
    os.remove(path)
    with open(path, "wb") as writable:
        writable.write(content)
    os.chmod(path, 0o644)


def report_linkcheck(out_dir):
    """Print the link checker's findings, which it writes to a file of its own."""
    path = os.path.join(out_dir, LINKCHECK_REPORT)
    if not os.path.exists(path):
        return
    with open(path, "r", encoding="utf-8") as report:
        messages = report.read()
    print("Link checker messages (%d lines):" % len(messages.splitlines()))
    sys.stdout.write(messages)


def run(config):
    builder = config["builder"]
    # Tests run from the runfiles root of the main repository, so the paths in
    # the config resolve against the working directory. The launcher is run by
    # absolute path: a relative one is interpreted against the search path
    # rather than the working directory on some platforms.
    sphinx_build = os.path.abspath(config["sphinx_build"])
    src_dir = config["src_dir"]
    out_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), builder)

    # sphinx-build finds the python interpreter and its packages through
    # RUNFILES_DIR when it cannot find a runfiles tree of its own beside it. It
    # shares this test's tree, which holds everything it needs.
    environment = dict(os.environ)
    if "RUNFILES_DIR" not in environment and "TEST_SRCDIR" in environment:
        environment["RUNFILES_DIR"] = environment["TEST_SRCDIR"]

    command = [sphinx_build, "-b", builder, "-E", "-N", "-T"]
    if builder == "spelling":
        make_writable(os.path.join(src_dir, DICTIONARY))
        # -t spelling asks conf.py for the spellchecker, which no other builder
        # has a use for. -W turns a warning, and so a misspelling, into a failure.
        command.extend(["-t", "spelling", "-W"])
    command.extend("-D%s=%s" % item for item in sorted(config["opts"].items()))
    command.extend([src_dir, out_dir])

    print("+ " + " ".join(command))
    sys.stdout.flush()
    status = subprocess.run(command, env=environment, check=False).returncode

    if builder == "linkcheck":
        report_linkcheck(out_dir)
    return status


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config")
    options = parser.parse_args()
    with open(options.config, "r", encoding="utf-8") as config_file:
        return run(json.load(config_file))


if __name__ == "__main__":
    sys.exit(main())
