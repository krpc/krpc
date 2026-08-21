"""Runs checkstyle over a set of java sources.

  run_checkstyle.py CONFIG

CONFIG is the json file written by the java_checkstyle_test rule, naming the
checkstyle launcher, the properties file and the sources to check, all
runfiles-root-relative. checkstyle exits with the number of violations it
found, so its status is the test's.
"""

import argparse
import json
import os
import subprocess
import sys

# The style to check against, a resource carried by checkstyle itself. The
# properties file supplies the values it leaves open, such as the line length.
STYLE = "/google_checks.xml"


def run(config):
    # Tests run from the runfiles root of the main repository, so the paths in
    # the config resolve against the working directory. The launcher is run by
    # absolute path: a relative one is interpreted against the search path
    # rather than the working directory on some platforms.
    checkstyle = os.path.abspath(config["checkstyle"])

    # The launcher locates the JVM and the checkstyle jars through RUNFILES_DIR
    # when it cannot find a runfiles tree of its own beside it. It shares this
    # test's tree, which holds everything it needs.
    environment = dict(os.environ)
    if "RUNFILES_DIR" not in environment and "TEST_SRCDIR" in environment:
        environment["RUNFILES_DIR"] = environment["TEST_SRCDIR"]

    command = [checkstyle, "-c", STYLE, "-p", config["properties"]]
    command.extend(config["srcs"])
    print("+ %s -c %s -p %s ..." % (checkstyle, STYLE, config["properties"]))
    sys.stdout.flush()
    return subprocess.run(command, env=environment, check=False).returncode


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config")
    options = parser.parse_args()
    with open(options.config, "r", encoding="utf-8") as config_file:
        return run(json.load(config_file))


if __name__ == "__main__":
    sys.exit(main())
