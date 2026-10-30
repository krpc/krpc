"""Runs a client's comms tests against a TestServer instance.

  run_client_test.py CONFIG

CONFIG is the json file written by the client_test rule, naming the server and
test executables (runfiles-root-relative) and the server transport to use.

The server picks its ports at startup and reports them on stdout, so the harness
starts it, waits for it to say it is up, and passes the ports it printed to the
test executable in the environment. The local socket transport reports the paths
of its sockets in the same way. For the serial transport there is neither: socat
pairs two pseudo-terminals and the server and test each get one end.

The server's output only reaches the test log when something goes wrong -- on a
failing test, or on the harness being killed, which is how the test runner
reports a timeout. Without it a server-side failure is invisible: the client just
blocks until the test times out.
"""

import argparse
import json
import os
import shutil
import signal
import subprocess
import sys
import tempfile
import time

from serverprocess import (
    SERVER_READY,
    Reader,
    setting,
    socket_directory,
    start,
    start_socat,
    stop,
)

# The virtual serial link is not always usable the instant the server reports
# itself started, so let it settle before the client opens its end.
SERIAL_SETTLE_TIME = 1


def run(config):
    # Tests run from the runfiles root of the main repository, so the paths in
    # the config resolve against the working directory. The executables are
    # launched by absolute path: relative ones are interpreted against the
    # search path rather than the working directory on some platforms.
    server = os.path.abspath(config["server"])
    test = os.path.abspath(config["test"])
    serial = config["server_type"] == "serialio"
    local_socket = config["server_type"] == "localsocket"
    websockets = config["server_type"].startswith("websockets")

    # The executables locate their own runfiles, the .NET runtime and assemblies,
    # the python interpreter and its packages, through RUNFILES_DIR
    # when they cannot find a runfiles tree of their own beside them. They share
    # this test's tree, which holds everything they need.
    environment = dict(os.environ)
    if "RUNFILES_DIR" not in environment and "TEST_SRCDIR" in environment:
        environment["RUNFILES_DIR"] = environment["TEST_SRCDIR"]

    socat = None
    server_port = None
    server_process = None
    sockets = None
    try:
        arguments = ["--type=" + config["server_type"]]
        test_environment = dict(environment)
        if serial:
            socat, server_port, client_port = start_socat()
            print(
                "Virtual serial ports established: %s <-> %s"
                % (server_port, client_port)
            )
            # Transfers run on a background thread whose failures show up only in
            # the server's log, so ask for the detail needed to diagnose them.
            arguments += ["--debug", "--serialio-port=" + server_port]
            test_environment["KRPC_TEST_SERIALIO_PORT"] = client_port
        elif local_socket:
            # A socket address has to fit in the kernel's address structure, which
            # leaves far less room than the sandbox's own directories take up, so
            # the socket goes in a short temporary directory of its own rather
            # than under the test's.
            sockets = tempfile.mkdtemp(dir=socket_directory())
            arguments += ["--localsocket-path=" + os.path.join(sockets, "krpc.sock")]

        server_process = start([server] + arguments, env=environment)
        reader = Reader(server_process, SERVER_READY)
        reader.wait("the server")

        if serial:
            time.sleep(SERIAL_SETTLE_TIME)
            print("Server started, port = %s" % server_port)
        elif local_socket:
            path = setting(reader.output, "localsocket_path")
            test_environment["KRPC_TEST_LOCALSOCKET_PATH"] = path
            print("Server started, socket = %s" % path)
        else:
            name = "websockets_port" if websockets else "tcpip_port"
            port = setting(reader.output, name)
            test_environment["KRPC_TEST_TCPIP_PORT"] = port
            print("Server started, port = %s" % port)

        # Dump the server's output if the harness is killed, which is how the
        # test runner reports a timeout on the platforms that can signal it.
        def on_terminate(_signum, _frame):
            report(reader.output)
            sys.exit(1)

        signal.signal(signal.SIGTERM, on_terminate)

        sys.stdout.flush()
        result = subprocess.run([test], env=test_environment, check=False).returncode
        if result != 0:
            report(reader.output)
        return result
    finally:
        for process in (server_process, socat):
            if process is not None:
                stop(process)
        if sockets is not None:
            shutil.rmtree(sockets, ignore_errors=True)


def report(output):
    sys.stdout.write("=== server output ===\n")
    sys.stdout.write(output)
    sys.stdout.flush()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config")
    options = parser.parse_args()
    with open(options.config, encoding="utf-8") as config_file:
        return run(json.load(config_file))


if __name__ == "__main__":
    sys.exit(main())
