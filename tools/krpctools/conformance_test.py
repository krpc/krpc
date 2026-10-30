"""Runs the conformance driver as krpctools ships it, against the Python testee.

  conformance_test.py SDIST PKG DOTNET TESTEE EXPECTED_FAILURES UNSUPPORTED CONVENTIONS

This checks the package rather than a client: the layout of the sdist, the krpc-conformance
entry point and the bundled TestServer. So it runs the connection tests alone.
"""

import importlib
import os
import sys
import tarfile

import tomllib


def main() -> int:
    sdist, pkg, dotnet, testee = sys.argv[1:5]
    expected_failures, unsupported, conventions = sys.argv[5:]
    extract_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), "sdist")
    with tarfile.open(sdist) as tar:
        tar.extractall(extract_dir, filter="data")
    pkg_dir = os.path.join(extract_dir, pkg)

    # The bundled TestServer is framework dependent, and runs on the toolchain's .NET runtime.
    # DOTNET is its exec path, which names an external repository beside the main one.
    dotnet_root = os.path.abspath(
        os.path.join("..", os.path.dirname(dotnet).removeprefix("external/"))
    )
    if not os.path.isfile(os.path.join(dotnet_root, os.path.basename(dotnet))):
        raise SystemExit("no .NET runtime at %s" % dotnet_root)
    os.environ["DOTNET_ROOT"] = dotnet_root

    # Call the entry point as pip would install it
    with open(os.path.join(pkg_dir, "pyproject.toml"), "rb") as config_file:
        scripts = tomllib.load(config_file)["project"]["scripts"]
    module, function = scripts["krpc-conformance"].split(":")
    sys.path.insert(0, pkg_dir)
    entry_point = getattr(importlib.import_module(module), function)
    sys.argv = [
        "krpc-conformance",
        "--testee=" + testee,
        "--expected-failures=" + expected_failures,
        "--unsupported=" + unsupported,
        "--conventions=" + conventions,
        "-k",
        "test_connection",
    ]
    return entry_point()


if __name__ == "__main__":
    sys.exit(main())
