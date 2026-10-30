"""The pytest plugin that runs the conformance tests against one client's testee.

Each test checks the requirement it is named after, and is marked with the profiles it needs, as
profile("<profile>"). A test outside the testee's profiles is skipped, and so is a test marked
untested("<reason>"), which only carries the text of a requirement with no test. A test marked
protocol("<protocol>") runs only over that protocol.

The expected failures file lists one test per line, as "test_<area>.py::test_<name>", with an
optional "# " comment. A test name ending in "@<protocol>" applies only over that protocol. A
listed test must fail, so the list cannot go stale, unless it is marked timing or its line has
"@flaky" after the test name. A line with "@skip" after the test name skips the test, for one
that hangs the testee. The unsupported file lists the tests a testee replies to as unsupported,
in the same form without a flag. A listed test is skipped, and an unlisted one fails. The conventions file is described in driver/conventions.py.
"""

from __future__ import annotations

import os
import pathlib
import shlex
import shutil
import sys
import tempfile
import time
from collections.abc import Callable, Generator, Iterator
from typing import Any, NamedTuple

import pytest
import serverprocess

import krpc
from driver import conventions as conventions_file
from driver.conventions import Conventions
from driver.scripted import ScriptedServer
from driver.testee import (
    Client,
    Endpoint,
    Testee,
    TesteeError,
    TesteeFailed,
    Unsupported,
)

# The server types for each protocol. A serial or websockets server also serves TCP/IP, for
# the control connection. A serial port takes one client, and the control connection is a
# Python client.
SERVER_TYPES = {
    "tcpip": "protobuf",
    "localsocket": "localsocket",
    "serialio": "serialio,protobuf",
    "websockets": "websockets,protobuf",
}

# The time the virtual serial link takes to become usable once the server has started
SERIAL_SETTLE_TIME = 1

# The profiles each protocol cannot carry. Serial IO has no streams or events, and a serial port
# takes one client.
PROTOCOL_LIMITS = {"serialio": {"streams", "events", "connections"}}


def pytest_addoption(parser: pytest.Parser) -> None:
    group = parser.getgroup("conformance")
    group.addoption(
        "--testee", required=True, help="The command that starts the testee"
    )
    group.addoption("--server", required=True, help="The TestServer executable")
    group.addoption("--protocol", default="tcpip", choices=sorted(SERVER_TYPES))
    group.addoption(
        "--expected-failures", default="", help="The expected failures file"
    )
    group.addoption(
        "--unsupported",
        default="",
        help="The file of the tests the client's language has no way to carry out",
    )
    group.addoption(
        "--profiles",
        default="",
        help="The file of the profiles and protocols the client implements",
    )
    group.addoption(
        "--conventions",
        default="",
        help="The file of the client's native error types and naming",
    )


def pytest_configure(config: pytest.Config) -> None:
    config.addinivalue_line("markers", "should: a requirement at level SHOULD")
    config.addinivalue_line("markers", "untested(reason): a requirement with no test")
    config.addinivalue_line(
        "markers", "profile(*names): the profiles a test needs, or excludes as !<name>"
    )
    config.addinivalue_line("markers", "timing: a test that races the clock")
    config.addinivalue_line(
        "markers", "protocol(*names): the protocols a test runs over"
    )


def pytest_collection_modifyitems(
    config: pytest.Config, items: list[pytest.Item]
) -> None:
    protocol = _option(config, "protocol")
    expected = _read_listing(_option(config, "expected_failures"), protocol)
    unsupported = _read_listing(_option(config, "unsupported"), protocol)
    flagged = sorted(
        x for x, listed in unsupported.items() if listed.flaky or listed.skip
    )
    if flagged:
        raise pytest.UsageError(
            "Unsupported tests with a flag: %s" % ", ".join(flagged)
        )
    both = expected.keys() & unsupported.keys()
    if both:
        raise pytest.UsageError(
            "Tests listed as both failing and unsupported: %s" % ", ".join(sorted(both))
        )
    # A run of only some test files leaves the others' lines unchecked
    collected = {x.path.name for x in items}
    unknown = {x for x in [*expected, *unsupported] if x.split("::")[0] in collected}
    for item in items:
        _check_docstring(item)
        untested = item.get_closest_marker("untested")
        if untested is not None:
            item.add_marker(pytest.mark.skip(reason=untested.args[0]))
        key = _listed(expected, item)
        if key is not None:
            unknown.discard(key)
            listed = expected[key]
            if listed.skip:
                item.add_marker(pytest.mark.skip(reason=listed.comment))
            strict = item.get_closest_marker("timing") is None and not listed.flaky
            item.add_marker(pytest.mark.xfail(reason=listed.comment, strict=strict))
        key = _listed(unsupported, item)
        if key is not None:
            unknown.discard(key)
            item.stash[_UNSUPPORTED] = unsupported[key].comment
    if unknown:
        raise pytest.UsageError(
            "Listed tests name no test: %s" % ", ".join(sorted(unknown))
        )


def _listed(listing: dict[str, Listed], item: pytest.Item) -> str | None:
    """The line of a listing that names a test, as a whole or by its case."""
    for name in (item.name, getattr(item, "originalname", item.name)):
        key = "%s::%s" % (item.path.name, name)
        if key in listing:
            return key
    return None


# The comment on the line that lists a test as unsupported
_UNSUPPORTED = pytest.StashKey[str]()
# Whether the call phase of a test raised, as a failure, a skip or an unsupported reply
_CALL_FAILED = pytest.StashKey[bool]()


@pytest.hookimpl(wrapper=True)
def pytest_runtest_makereport(
    item: pytest.Item, call: pytest.CallInfo[None]
) -> Generator[None, pytest.TestReport, pytest.TestReport]:
    """Skip a listed test that its testee replies to as unsupported, and fail any other.
    A listed test that runs fails, so the list cannot go stale."""
    report = yield
    if call.when == "call":
        item.stash[_CALL_FAILED] = call.excinfo is not None
    listed = _UNSUPPORTED in item.stash
    if call.excinfo is not None and call.excinfo.errisinstance(Unsupported):
        if listed:
            report.outcome = "skipped"
            # The report's line is 1-based, and the item's 0-based
            line = (item.location[1] or 0) + 1
            report.longrepr = (str(item.path), line, str(call.excinfo.value))
        else:
            report.longrepr = "%s, and the test is not listed as unsupported" % (
                call.excinfo.value
            )
    elif call.when == "call" and listed and report.passed:
        report.outcome = "failed"
        report.longrepr = "The test is listed as unsupported, but ran"
    return report


def _option(config: pytest.Config, name: str) -> str:
    return str(config.getoption(name))


class Listed(NamedTuple):
    """A line of an expected failures or unsupported file: its comment, whether the test is
    flaky, so it can pass by chance, and whether it is skipped, as it hangs."""

    comment: str
    flaky: bool
    skip: bool


def _read_listing(path: str, protocol: str) -> dict[str, Listed]:
    """The tests an expected failures or unsupported file lists for the protocol."""
    if not path:
        return {}
    expected = {}
    with open(path, encoding="utf-8") as listing:
        for line in listing:
            test, _, comment = line.partition("#")
            # A test id can hold spaces, so the flag is the last word
            test, flaky = _flag(test.strip(), "@flaky")
            test, skip = _flag(test, "@skip")
            test, _, only = test.partition("@")
            if only and only not in SERVER_TYPES:
                raise pytest.UsageError(
                    "%s lists %s for an unknown protocol %s" % (path, test, only)
                )
            if test and only in ("", protocol):
                expected[test] = Listed(comment.strip(), flaky, skip)
    return expected


def _flag(test: str, flag: str) -> tuple[str, bool]:
    """A listed test without the flag after it, and whether it had the flag."""
    if test.endswith(" " + flag):
        return test.removesuffix(" " + flag).rstrip(), True
    return test, False


def _check_docstring(item: pytest.Item) -> None:
    """Check that a test has the text of its requirement."""
    function = getattr(item, "function", None)
    if function is None or not function.__doc__:
        raise pytest.UsageError("%s has no docstring" % item.nodeid)


def _environment() -> dict[str, str]:
    """The environment for the testee and the server. Both find their runfiles, such as the
    .NET runtime, in this test's runfiles tree."""
    environment = dict(os.environ)
    if "RUNFILES_DIR" not in environment and "TEST_SRCDIR" in environment:
        environment["RUNFILES_DIR"] = environment["TEST_SRCDIR"]
    return environment


@pytest.fixture(scope="session")
def server(request: pytest.FixtureRequest) -> Iterator[Endpoint]:
    """A TestServer on the protocol under test. A serial endpoint also gives the TCP/IP
    address and port the server serves the control connection on."""
    protocol = _option(request.config, "protocol")
    arguments = ["--type=" + SERVER_TYPES[protocol]]
    directory = None
    socat = None
    client_port = ""
    if protocol == "localsocket":
        directory = tempfile.mkdtemp(dir=serverprocess.socket_directory())
        arguments.append("--localsocket-path=" + os.path.join(directory, "krpc.sock"))
    elif protocol == "serialio":
        directory = tempfile.mkdtemp()
        socat, server_port, client_port = serverprocess.start_socat(directory)
        arguments.append("--serialio-port=" + server_port)
    executable = os.path.abspath(_option(request.config, "server"))
    process = serverprocess.start([executable] + arguments, env=_environment())
    try:
        reader = serverprocess.Reader(process, serverprocess.SERVER_READY)
        reader.wait("the server")
        if protocol == "localsocket":
            path = serverprocess.setting(reader.output, "localsocket_path")
            yield Endpoint(protocol, path=path)
        else:
            port = int(serverprocess.setting(reader.output, "tcpip_port"))
            if protocol == "websockets":
                websockets_port = int(
                    serverprocess.setting(reader.output, "websockets_port")
                )
                yield Endpoint(
                    protocol,
                    address="127.0.0.1",
                    port=websockets_port,
                    control_port=port,
                )
                return
            if protocol == "serialio":
                time.sleep(SERIAL_SETTLE_TIME)
                yield Endpoint(
                    protocol, address="127.0.0.1", path=client_port, control_port=port
                )
                return
            yield Endpoint(protocol, address="127.0.0.1", port=port, control_port=port)
    finally:
        serverprocess.stop(process)
        if socat is not None:
            serverprocess.stop(socat)
        if directory is not None:
            shutil.rmtree(directory, ignore_errors=True)


class TesteeHost:
    """The testee for the session. A testee that exits or misses a deadline is replaced before
    the next test."""

    def __init__(self, command: list[str], runtime_dir: str) -> None:
        self._command = command
        self._runtime_dir = runtime_dir
        self._testee: Testee | None = None

    def get(self) -> Testee:
        if self._testee is None or not self._testee.running:
            if self._testee is not None:
                self._testee.stop()
            # The directory the default local socket path is in
            environment = _environment()
            environment["XDG_RUNTIME_DIR"] = self._runtime_dir
            environment["LOCALAPPDATA"] = self._runtime_dir
            self._testee = Testee(self._command, environment)
        return self._testee

    def stop(self) -> None:
        if self._testee is not None:
            self._testee.stop()

    def spawn(self, unset: list[str], extra: dict[str, str]) -> Testee:
        """A testee of its own, with variables unset in its environment and others added."""
        environment = _environment()
        for name in unset:
            environment.pop(name, None)
        environment.update(extra)
        return Testee(self._command, environment)


def _check_conventions(conventions: Conventions, testee: Testee) -> None:
    """Check that the conventions have each section the testee's profiles need."""
    missing = conventions_file.missing(conventions, list(testee.hello.profiles))
    if missing:
        testee.stop()
        pytest.exit(
            "%s needs %s in its conventions file, passed with --conventions"
            % (testee.hello.client, " and ".join(missing)),
            returncode=1,
        )


def _check_profiles(path: str, protocol: str, testee: Testee) -> None:
    """Check that the testee reports the profiles its profiles file lists, and the protocol
    under test."""
    if not path:
        return
    with open(path, encoding="utf-8") as listing:
        declared = {x.strip() for x in listing if x.strip()}
    profiles = declared - set(SERVER_TYPES)
    hello = testee.hello
    if set(hello.profiles) != profiles or protocol not in hello.protocols:
        testee.stop()
        pytest.exit(
            "%s reports profiles %s and protocols %s, but %s lists %s and the tests run over %s"
            % (
                hello.client,
                sorted(hello.profiles),
                sorted(hello.protocols),
                path,
                sorted(profiles),
                protocol,
            ),
            returncode=1,
        )


@pytest.fixture(scope="session")
def runtime_dir() -> Iterator[str]:
    """The directory the testee finds the default local socket path in."""
    path = tempfile.mkdtemp(dir=serverprocess.socket_directory())
    yield path
    shutil.rmtree(path, ignore_errors=True)


@pytest.fixture(scope="session")
def conventions(request: pytest.FixtureRequest) -> Conventions:
    """The client's conventions file."""
    try:
        return conventions_file.read(_option(request.config, "conventions"))
    except (OSError, ValueError, TypeError) as exn:
        raise pytest.UsageError("Cannot read the conventions file: %s" % exn) from exn


@pytest.fixture(scope="session")
def testee_host(
    request: pytest.FixtureRequest, runtime_dir: str, conventions: Conventions
) -> Iterator[TesteeHost]:
    command = shlex.split(_option(request.config, "testee"))
    # The testee executable by its absolute path
    if os.path.exists(command[0]):
        command[0] = os.path.abspath(command[0])
    host = TesteeHost(command, runtime_dir)
    _check_profiles(
        _option(request.config, "profiles"),
        _option(request.config, "protocol"),
        host.get(),
    )
    _check_conventions(conventions, host.get())
    yield host
    host.stop()


@pytest.fixture
def testee(testee_host: TesteeHost) -> Testee:
    return testee_host.get()


@pytest.fixture(autouse=True)
def _profiles(request: pytest.FixtureRequest, testee: Testee) -> None:
    """Skip a test that needs a profile the testee does not implement, or one marked
    "!<profile>" that it does."""
    for marker in request.node.iter_markers("profile"):
        for name in marker.args:
            if name.startswith("!") and name[1:] in testee.hello.profiles:
                pytest.skip("%s has the %s profile" % (testee.hello.client, name[1:]))
            if not name.startswith("!") and name not in testee.hello.profiles:
                pytest.skip("%s has no %s profile" % (testee.hello.client, name))
    protocol = _option(request.config, "protocol")
    for marker in request.node.iter_markers("profile"):
        for name in marker.args:
            if name in PROTOCOL_LIMITS.get(protocol, set()):
                pytest.skip("The %s protocol has no %s profile" % (protocol, name))
    for marker in request.node.iter_markers("protocol"):
        if protocol not in marker.args:
            pytest.skip("Runs over the %s protocol" % " or ".join(marker.args))


@pytest.fixture
def fallback_client(
    testee_host: TesteeHost, tmp_path: pathlib.Path, conventions: Conventions
) -> Iterator[Client]:
    """A client whose testee has no XDG_RUNTIME_DIR or LOCALAPPDATA, so its default local
    socket path falls back to /tmp. TMPDIR names a directory of the test's own, which the
    path must not follow. USER and LOGNAME are set, as a login shell sets them. Windows always has
    LOCALAPPDATA, so it never falls back."""
    if sys.platform == "win32":
        pytest.skip("Windows always has LOCALAPPDATA")
    import pwd  # noqa: PLC0415

    user = pwd.getpwuid(os.getuid()).pw_name
    testee = testee_host.spawn(
        ["XDG_RUNTIME_DIR", "LOCALAPPDATA"],
        {"TMPDIR": str(tmp_path), "USER": user, "LOGNAME": user},
    )
    try:
        yield Client(testee, conventions)
    finally:
        testee.stop()


@pytest.fixture
def client(testee: Testee, conventions: Conventions) -> Client:
    """The client under test, not yet connected."""
    return Client(testee, conventions)


@pytest.fixture
def connected(
    request: pytest.FixtureRequest, client: Client, server: Endpoint
) -> Iterator[Client]:
    """The client under test, connected to the TestServer."""
    client.connect(server, name=request.node.name)
    yield client
    if client.testee.running:
        try:
            client.close()
        except (TesteeError, TesteeFailed, Unsupported):
            # A close of a connection the test ended can fail
            pass


@pytest.fixture(scope="session")
def control(server: Endpoint) -> Iterator[Any]:
    """A Python client connection the driver uses to look at the server."""
    if server.protocol == "localsocket":
        conn = krpc.connect_local(name="conformance-control", path=server.path)
    else:
        conn = krpc.connect(name="conformance-control", port=server.control_port)
    yield conn
    conn.close()


@pytest.fixture(scope="session")
def scripted_dir() -> Iterator[str]:
    """A directory short enough for a socket path, for the sockets the driver listens on."""
    path = tempfile.mkdtemp(dir=serverprocess.socket_directory())
    yield path
    shutil.rmtree(path, ignore_errors=True)


@pytest.fixture
def scripted_server(
    request: pytest.FixtureRequest, scripted_dir: str
) -> Iterator[Callable[..., ScriptedServer]]:
    """Starts a ScriptedServer on the protocol under test, at a local socket path when given
    one, and checks its script once the test is done. A test that failed, or that its
    testee could not carry out, abandons its scripts, keeping the error of a script that
    had already failed as a report section."""
    servers: list[ScriptedServer] = []
    protocol = _option(request.config, "protocol")
    local = protocol == "localsocket"

    def start(
        script: Callable[[ScriptedServer], None], path: str = ""
    ) -> ScriptedServer:
        if not path and local:
            path = os.path.join(scripted_dir, "scripted-%d.sock" % len(servers))
        servers.append(ScriptedServer(script, protocol, path))
        return servers[-1]

    yield start
    failed = request.node.stash.get(_CALL_FAILED, False)
    error: BaseException | None = None
    for scripted in servers:
        try:
            if failed:
                script_error = scripted.abandon()
                if script_error is not None:
                    request.node.add_report_section(
                        "teardown", "script", repr(script_error)
                    )
            else:
                scripted.join()
        except BaseException as exn:  # noqa: BLE001
            # Every server is closed, and the first error raised
            error = error or exn
    if error is not None:
        raise error
