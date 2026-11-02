"""Smoke tests the krpctools commands on the machine it runs on.

Run from the root of the repository, with the krpc and krpctools packages installed, and the
testee's protobuf module on the python path. Each command is run once, as a user would run it.
"""

import os
import socket
import subprocess
import sys
import tempfile
import time

import krpc
from krpctools import dotnet

# The package ships the module, which Bazel has at tools/build/serverprocess.py
from krpctools.conformance import serverprocess  # pyright: ignore[reportMissingImports]


def run(*command):
    print("+ " + " ".join(command), flush=True)
    subprocess.run(command, check=True)


def wait_until_closed(port):
    """Wait until the port refuses connections, as the server is killed asynchronously."""
    deadline = time.monotonic() + 10
    while time.monotonic() < deadline:
        try:
            socket.create_connection(("127.0.0.1", port), timeout=1).close()
        except OSError:
            return
        time.sleep(0.1)
    raise RuntimeError("the TestServer still listens after krpc-testserver stopped")


def test_testserver():
    """Start the TestServer, connect to it, and check that stopping krpc-testserver stops the
    server."""
    command = ["krpc-testserver", "--tcpip-port=0"]
    print("+ " + " ".join(command), flush=True)
    process = serverprocess.start(command)
    try:
        reader = serverprocess.Reader(process, serverprocess.SERVER_READY)
        reader.wait("the TestServer")
        print(reader.output, end="")
        port = int(serverprocess.setting(reader.output, "tcpip_port"))
        with krpc.connect(name="smoke-test", address="127.0.0.1", port=port) as conn:
            print("connected, server version %s" % conn.krpc.get_status().version)
            # The documentation of every bundled service is shipped
            for service in conn.krpc.get_services().services:
                if not service.documentation:
                    raise RuntimeError("no documentation for service %s" % service.name)
    finally:
        serverprocess.stop(process)
    wait_until_closed(port)


def test_krpctools(directory):
    """Run each krpctools command on the KRPC service in the bundled KRPC.Core.dll."""
    bundle = dotnet.servicedefinitions_dir()
    core = os.path.join(bundle, "KRPC.Core.dll")
    defs = os.path.join(directory, "KRPC.json")
    run(
        "krpc-servicedefs",
        "--reference-dir",
        bundle,
        "--output",
        defs,
        "KRPC",
        core,
    )
    run(
        "krpc-clientgen",
        "python",
        "KRPC",
        defs,
        "--output",
        os.path.join(directory, "krpc.py"),
    )
    # From the assembly, through the ServiceDefinitions tool
    run(
        "krpc-clientgen",
        "csharp",
        "KRPC",
        core,
        "--reference-dir",
        bundle,
        "--output",
        os.path.join(directory, "KRPC.cs"),
    )
    run(
        "krpc-docgen",
        "python",
        "doc/order.txt",
        defs,
        "--page",
        "doc/api/krpc/krpc.tmpl",
        os.path.join(directory, "krpc.rst"),
    )


def test_conformance():
    """Run the connection tests against the Python client, through the bundled TestServer."""
    run(
        "krpc-conformance",
        "--testee",
        # The driver splits the command as a POSIX shell does
        '"%s" client/python/conformance/testee.py' % sys.executable.replace("\\", "/"),
        "--expected-failures=client/python/conformance/expected-failures.txt",
        "--unsupported=client/python/conformance/unsupported.txt",
        "--conventions=client/python/conformance/conventions.json",
        "-k",
        "test_connection",
    )


def main():
    test_testserver()
    with tempfile.TemporaryDirectory() as directory:
        test_krpctools(directory)
    test_conformance()


if __name__ == "__main__":
    main()
