"""Runs the conformance tests against one client.

  krpc-conformance --testee COMMAND [--server PATH] [--protocol PROTOCOL]
                   [--expected-failures PATH] [--unsupported PATH] [--profiles PATH]
                   [--conventions PATH] [PYTEST ARGS]
  krpc-conformance --config CONFIG [PYTEST ARGS]

COMMAND starts the testee. The server defaults to the TestServer krpctools ships in its bin
directory. CONFIG is the json file the conformance_test rule writes, with the same settings as
runfiles-relative paths.
"""

import argparse
import json
import os
import shlex
import sys

import pytest


def main() -> int:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    # The tests import the driver package from the conformance directory
    sys.path.insert(0, root)
    parser = argparse.ArgumentParser(
        description="Run the kRPC conformance tests", allow_abbrev=False
    )
    parser.add_argument("--config", help="The settings, from the conformance_test rule")
    parser.add_argument("--testee", help="The command that starts the testee")
    parser.add_argument(
        "--server",
        default=os.path.join(os.path.dirname(root), "bin", "TestServer", "TestServer"),
        help="The TestServer executable",
    )
    parser.add_argument("--protocol", default="tcpip")
    parser.add_argument("--expected-failures", default="")
    parser.add_argument("--unsupported", default="")
    parser.add_argument("--profiles", default="")
    parser.add_argument("--conventions", default="")
    args, pytest_args = parser.parse_known_args()
    if args.config:
        with open(args.config, encoding="utf-8") as config_file:
            config = json.load(config_file)
        # The testee is a single executable, which may have spaces in its path
        args.testee = shlex.quote(config["testee"])
        args.server = config["server"]
        args.protocol = config["protocol"]
        args.expected_failures = config["expected_failures"]
        args.unsupported = config["unsupported"]
        args.profiles = config["profiles"]
        args.conventions = config["conventions"]
    if not args.testee:
        parser.error("the testee command is required")
    arguments = [
        os.path.join(root, "tests"),
        "--rootdir=" + root,
        "-c",
        os.path.join(root, "pytest.ini"),
        "-p",
        "driver.plugin",
        # The runfiles tree is read only
        "-p",
        "no:cacheprovider",
        "-v",
        "--testee=" + args.testee,
        "--server=" + args.server,
        "--protocol=" + args.protocol,
        "--expected-failures=" + args.expected_failures,
        "--unsupported=" + args.unsupported,
        "--profiles=" + args.profiles,
        "--conventions=" + args.conventions,
    ]
    return pytest.main(arguments + pytest_args)


if __name__ == "__main__":
    sys.exit(main())
