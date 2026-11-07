"""Runs the conformance driver as krpctools ships it, against the Python testee.

  conformance_test.py WHEEL DOTNET TESTEE EXPECTED_FAILURES UNSUPPORTED CONVENTIONS

This checks the package rather than a client: the layout of the wheel, the krpc-conformance
entry point and the bundled TestServer. So it runs the connection tests alone.
"""

import importlib.metadata
import os
import sys
import zipfile


def main() -> int:
    wheel, dotnet, testee = sys.argv[1:4]
    expected_failures, unsupported, conventions = sys.argv[4:]
    extract_dir = os.path.join(os.environ.get("TEST_TMPDIR", "."), "wheel")
    with zipfile.ZipFile(wheel) as archive:
        archive.extractall(extract_dir)

    # The bundled TestServer is framework dependent, and runs on the toolchain's dotnet host.
    # DOTNET is its exec path, which names an external repository beside the main one.
    dotnet_root = os.path.abspath(
        os.path.join("..", os.path.dirname(dotnet).removeprefix("external/"))
    )
    if not os.path.isfile(os.path.join(dotnet_root, os.path.basename(dotnet))):
        raise SystemExit("no .NET runtime at %s" % dotnet_root)
    os.environ["DOTNET_ROOT"] = dotnet_root

    # Call the entry point as pip would install it
    sys.path.insert(0, extract_dir)
    distribution = importlib.metadata.distribution("krpctools")
    entry_point = distribution.entry_points["krpc-conformance"].load()
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
