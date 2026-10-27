"""Connection requirements: connecting, refusals and closing."""

import math
import os
import socket
import sys
import threading
import time

import pytest
import testee_pb2 as pb
from driver import profile, protocol, section, should, untested, values
from driver.framing import CLIENT_IDENTIFIER, PROTOCOL_VERSION, encode_size, frame
from driver.scripted import ScriptedServer, definitions, is_call
from driver.testee import (
    AT_ONCE,
    REPLY_TIMEOUT,
    Endpoint,
    TesteeError,
    invocation,
)

import krpc.schema.KRPC_pb2 as KRPC


def served(scripted, server):
    """Answer GetServices with the server's definitions of KRPC and TestService, which a
    client that builds its services from the server's definitions needs."""
    scripted.services = definitions(server, "KRPC", "TestService")
    return scripted


def get_client_name(client, timeout=REPLY_TIMEOUT):
    return client.testee.run(timeout, invoke=invocation("KRPC", "GetClientName")).value


def connection_error(func):
    with pytest.raises(TesteeError) as info:
        func()
    assert info.value.error.kind == pb.Error.CONNECTION
    return info.value.error


def until_disconnect(scripted, reasons):
    """Read until the client sends a disconnect or closes, keeping the disconnect's reason."""
    while True:
        message = scripted.receive()
        if message is None:
            return
        if message.WhichOneof("content") == "disconnect":
            reasons.append(KRPC.Disconnect.Reason.Name(message.disconnect.reason))
            return


def protocol_error_test(client, server, scripted_server, data, reason, framed=True):
    """Send a message's bytes, or raw bytes when not framed, in place of the response to a
    call, and check that the call fails and the client sends a disconnect with the reason."""
    reasons = []
    send = ScriptedServer.send_payload if framed else ScriptedServer.send_raw

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.answer_until("GetClientName")
        send(scripted, data)
        until_disconnect(scripted, reasons)

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint())
    try:
        connection_error(lambda: get_client_name(client))
    finally:
        client.close_quietly()
    scripted.join()
    assert reasons == [reason]


def tcp_protocol(server):
    """The protocol under test when it runs over TCP/IP, or TCP/IP."""
    return "websockets" if server.protocol == "websockets" else "tcpip"


def skip_without_tcp(client, server):
    """Skip a test that needs TCP/IP, for a testee with only a local socket."""
    if tcp_protocol(server) not in client.testee.hello.protocols:
        pytest.skip("%s has no %s protocol" % (client.name, tcp_protocol(server)))


def skip_when_framed(server):
    """Skip a test that sends part of a message. Websockets carries each message whole."""
    if server.protocol == "websockets":
        pytest.skip("Websockets carries each message whole")


def skip_when_resuming(server):
    """Skip a handshake test whose message a client resuming a serial line skips, or reads as
    no answer."""
    if server.protocol == "serialio":
        pytest.skip("A client resuming a serial line skips the message")


def leftover(message):
    """A script that sends a message an earlier session left on the line, before the
    connection response."""

    def script(scripted):
        scripted.receive_connection_request()
        scripted.send(message)
        scripted.send(OK)
        scripted.serve()

    return script


def second_connection_response(scripted):
    """A retry the server read late, answered with a second session before the first
    response."""
    scripted.accept()
    scripted.send(OK)
    scripted.serve()


def receive_retry(scripted, start):
    """The request a resuming client sends again, no sooner than the server's stall timeout
    after the first, with no other message before it."""
    message = scripted.receive()
    if message is None:
        return None
    assert message.WhichOneof("content") == "connection_request"
    early = time.monotonic() - start < STALL_TIMEOUT
    assert not early, "The client sent the request again early"
    return message


def no_answer(status):
    """A script that answers the request with a status a resuming client reads as no answer,
    then accepts the request the client sends again."""

    def script(scripted):
        scripted.refuse(status, "The request was not read")
        if receive_retry(scripted, time.monotonic()) is None:
            return
        scripted.send(OK)
        scripted.serve()

    return script


def disconnect_is_no_answer(scripted):
    """A disconnect for the request, such as from a session it started partway through an
    earlier session's message, which a resuming client reads as no answer. The request the
    client sends again is accepted."""
    scripted.receive_connection_request()
    start = time.monotonic()
    scripted.send(
        KRPC.Message(disconnect=KRPC.Disconnect(reason=KRPC.Disconnect.TIMEOUT))
    )
    if receive_retry(scripted, start) is None:
        return
    scripted.send(OK)
    scripted.serve()


def refusal(status):
    """A script that refuses the request with a status, after which the client sends no
    second request, even after its handshake timeout."""

    def script(scripted):
        scripted.refuse(status, "Refused by the script")
        assert not scripted.readable(RETRY_WAIT)

    return script


def ending_script(ending):
    """A script that serves until a call of GetClientName, then sends a ``disconnect`` in
    place of its response, or ends the connection without one, or partway through the
    response. Closing ends the script too."""

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        if ending == "close":
            scripted.serve()
            return

        def end(request):
            if ending == "disconnect":
                disconnect = KRPC.Disconnect(
                    reason=KRPC.Disconnect.REMOVED, message=REMOVED_MESSAGE
                )
                scripted.send(KRPC.Message(disconnect=disconnect))
            elif ending == "partway":
                response = KRPC.Response(id=request.id)
                response.results.add(value=b"\x00")
                data = frame(KRPC.Message(response=response).SerializeToString())
                scripted.send_raw(data[:3])
            scripted.close()

        end(scripted.answer_until("GetClientName"))

    return script


OK = KRPC.Message(
    connection_response=KRPC.ConnectionResponse(
        status=KRPC.ConnectionResponse.OK,
        protocol_version=PROTOCOL_VERSION,
        client_identifier=CLIENT_IDENTIFIER,
    )
)


# A size prefix that has not ended within five bytes, the most a uint32 takes
MALFORMED_SIZE = b"\xff\xff\xff\xff\xff\x01"


# The handshake timeout a resuming client is given, above the server's stall timeout
RESUME_HANDSHAKE_TIMEOUT = 3.5


# The time a script waits to see that a refused client sends no request again
RETRY_WAIT = 5


# The time the server waits for the rest of a partial message
STALL_TIMEOUT = 3


REMOVED_MESSAGE = "Removed by the script"


# The time a script holds back a message, to check that a client waits for it
DELAY = 0.5


# The timeout values a client takes, as the testee sends them
TIMEOUT_VALUES = {"zero": 0.0, "infinite": math.inf, "negative": -1.0, "nan": math.nan}


STREAM_UPDATE = KRPC.StreamUpdate()


STREAM_UPDATE.results.add(id=1, result=KRPC.ProcedureResult(value=b"\x02"))


# The messages an earlier session can leave on the line
LEFTOVERS = {
    "response": KRPC.Message(response=KRPC.Response(id=7)),
    "stream-update": KRPC.Message(stream_update=STREAM_UPDATE),
}


# The answers a resuming client reads as no answer
NO_ANSWERS = {
    "disconnect": disconnect_is_no_answer,
    "malformed-message": no_answer("MALFORMED_MESSAGE"),
    "timeout": no_answer("TIMEOUT"),
}


section("Connecting")


@protocol("tcpip")
def test_tcpip(client, server):
    """The client connects over TCP/IP to an address and a port."""
    client.connect(server)
    assert client.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


@untested("The test server listens on a port of its own")
def test_default_address():
    """Over TCP/IP and websockets, the address defaults to ``127.0.0.1`` and the port to
    ``50000``."""


@pytest.mark.parametrize("kind", ["port", "socket"])
def test_unreachable(client, server, tmp_path, kind):
    """Connecting raises the connection error when no server listens at the address, no socket
    is at the local socket path, or no serial port is at the path."""
    if kind == "socket":
        if "localsocket" not in client.testee.hello.protocols:
            pytest.skip("%s has no local socket protocol" % client.name)
        endpoint = Endpoint("localsocket", path=str(tmp_path / "missing.sock"))
    elif server.protocol == "serialio":
        endpoint = Endpoint("serialio", path=str(tmp_path / "missing"))
    else:
        skip_without_tcp(client, server)
        # A port that was free a moment ago
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        endpoint = Endpoint(tcp_protocol(server), address="127.0.0.1", port=port)
    connection_error(lambda: client.connect(endpoint))


def test_unknown_host(client, server):
    """A host name that does not resolve raises the connection error."""
    if server.protocol == "serialio":
        pytest.skip("A serial line has no host")
    skip_without_tcp(client, server)
    # The .invalid domain never resolves
    endpoint = Endpoint(tcp_protocol(server), address="krpc.invalid", port=50000)
    connection_error(lambda: client.connect(endpoint))


@protocol("localsocket")
def test_localsocket(client, server):
    """The client connects to a local socket path."""
    client.connect(server)
    assert client.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


@protocol("localsocket")
def test_localsocket_default_path(client, server, scripted_server, runtime_dir):
    """The default local socket path follows the rule under
    :ref:`communication-protocol-localsocket-default-path`."""

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    # The testee runs with XDG_RUNTIME_DIR and LOCALAPPDATA set to the runtime directory
    path = os.path.join(runtime_dir, "krpc.sock")
    scripted = served(scripted_server(script, path), server)
    client.connect(Endpoint("localsocket"))
    try:
        get_client_name(client)
    finally:
        client.close()
    scripted.join()


@protocol("localsocket")
def test_localsocket_fallback_path(fallback_client, server, scripted_server):
    """With no ``XDG_RUNTIME_DIR``, the default local socket path is
    ``/tmp/krpc-<user>.sock``. The client ignores ``TMPDIR`` for it."""
    import fcntl  # noqa: PLC0415
    import pwd  # noqa: PLC0415

    user = pwd.getpwuid(os.getuid()).pw_name
    path = "/tmp/krpc-%s.sock" % user
    # Every suite on the machine shares the path, so each takes it in turn
    with open(path + ".lock", "w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        if os.path.exists(path):
            probe = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            try:
                probe.connect(path)
            except OSError:
                # Left behind by a run that did not finish
                os.remove(path)
            else:
                pytest.skip("A server is listening on %s" % path)
            finally:
                probe.close()

        def script(scripted: ScriptedServer) -> None:
            scripted.accept()
            scripted.serve()

        scripted = served(scripted_server(script, path), server)
        fallback_client.connect(Endpoint("localsocket"))
        try:
            get_client_name(fallback_client)
        finally:
            fallback_client.close()
        scripted.join()


# The size of the path in a unix domain socket address
SOCKET_PATH_SIZE = 104 if sys.platform == "darwin" else 108


def test_localsocket_path_too_long(client, server, scripted_dir):
    """A local socket path too long for a socket address raises the connection error or the
    argument error. The client does not connect to the socket a leading part of the
    path names."""
    if "localsocket" not in client.testee.hello.protocols:
        pytest.skip("%s has no local socket protocol" % client.name)
    directory = os.path.join(scripted_dir, "long")
    os.makedirs(directory, exist_ok=True)
    base = directory + os.sep
    # The longest path that fits, with its terminating NUL
    path = base + "p" * (SOCKET_PATH_SIZE - 1 - len(base))
    listener = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    try:
        listener.bind(path)
        listener.listen(1)
        listener.setblocking(False)
        endpoint = Endpoint("localsocket", path=path + "q" * 64)
        with pytest.raises(TesteeError) as info:
            client.connect(endpoint)
        kind = info.value.error.kind
        assert kind in (pb.Error.CONNECTION, pb.Error.ARGUMENT), info.value.error
        with pytest.raises(BlockingIOError):
            listener.accept()
    finally:
        listener.close()
        os.remove(path)


@protocol("serialio")
def test_serialio(client, server):
    """The client connects to a serial port."""
    client.connect(server)
    assert client.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


@protocol("websockets")
def test_websockets(client, server, scripted_server):
    """The client connects over websockets to ``ws://<address>:<port>/``, with no
    subprotocol. Each ``Message`` is one binary websockets message."""

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint())
    try:
        get_client_name(client)
    finally:
        client.close()
    scripted.join()
    upgrade = scripted.websocket.upgrade
    assert upgrade.method == "GET"
    assert upgrade.path == "/"
    assert "sec-websocket-protocol" not in upgrade.headers


section("Handshake")


def test_handshake_name(client, server):
    """The client sends the name the program gives it in the ``connection_request``. The
    name can hold any Unicode character. ``KRPC.GetClientName`` returns that name."""
    name = "Conformance client ü日本"
    client.connect(server, name=name)
    assert client.invoke("KRPC", "GetClientName") == name


def test_handshake_version(client, scripted_server):
    """The client sends protocol version 2."""
    requests = []

    def script(scripted: ScriptedServer) -> None:
        requests.append(scripted.accept())
        scripted.serve()

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    client.close()
    scripted.join()
    assert requests[0].protocol_version == 2


@pytest.mark.parametrize("version", [0, 3])
def test_handshake_wrong_version(client, scripted_server, version):
    """A ``connection_response`` with the status ``OK`` and a ``protocol_version`` other
    than 2 raises the connection error."""

    answered = threading.Event()

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        response = KRPC.ConnectionResponse(
            status=KRPC.ConnectionResponse.OK,
            protocol_version=version,
            client_identifier=CLIENT_IDENTIFIER,
        )
        scripted.send(KRPC.Message(connection_response=response))
        # The connection stays open until the client has read the response
        answered.wait(REPLY_TIMEOUT)

    scripted = scripted_server(script)
    try:
        connection_error(lambda: client.connect(scripted.endpoint()))
    finally:
        answered.set()


@pytest.mark.parametrize(
    "status", ["DENIED", "WRONG_PROTOCOL_VERSION", "MALFORMED_MESSAGE", "TIMEOUT"]
)
def test_handshake_refused(client, server, scripted_server, status):
    """A ``connection_response`` with a status other than ``OK`` raises the connection
    error, with the status and the message."""
    if status in ("MALFORMED_MESSAGE", "TIMEOUT"):
        skip_when_resuming(server)
    message = "Refused by the script"
    scripted = scripted_server(lambda scripted: scripted.refuse(status, message))
    error = connection_error(lambda: client.connect(scripted.endpoint()))
    assert error.status == status
    assert message in error.message


def test_handshake_unknown_status(client, scripted_server):
    """A status the protocol does not define raises the connection error, with the
    message."""
    message = "Refused with a new status"

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        response = KRPC.ConnectionResponse(message=message)
        response.status = 99  # pyright: ignore[reportAttributeAccessIssue]
        scripted.send(KRPC.Message(connection_response=response))

    scripted = scripted_server(script)
    error = connection_error(lambda: client.connect(scripted.endpoint()))
    assert message in error.message


@pytest.mark.parametrize("reason", ["known", "unknown"])
def test_handshake_disconnect(client, server, scripted_server, reason):
    """A ``disconnect`` in place of the ``connection_response`` raises the connection
    error, with its reason and message. A reason the protocol does not define raises the
    connection error with the message."""
    skip_when_resuming(server)
    message = "Stopped during the handshake"

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        disconnect = KRPC.Disconnect(message=message)
        if reason == "known":
            disconnect.reason = KRPC.Disconnect.SERVER_STOPPED
        else:
            disconnect.reason = 99  # pyright: ignore[reportAttributeAccessIssue]
        scripted.send(KRPC.Message(disconnect=disconnect))

    scripted = scripted_server(script)
    error = connection_error(lambda: client.connect(scripted.endpoint()))
    if reason == "known":
        assert error.reason == "SERVER_STOPPED"
    assert message in error.message


@pytest.mark.parametrize("message", ["response", "empty"])
def test_handshake_unexpected_message(client, server, scripted_server, message):
    """Any other message in place of the ``connection_response``, including an empty
    one, raises the connection error."""
    skip_when_resuming(server)
    sent = (
        KRPC.Message(response=KRPC.Response(id=1))
        if message == "response"
        else KRPC.Message()
    )

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        scripted.send(sent)

    scripted = scripted_server(script)
    connection_error(lambda: client.connect(scripted.endpoint()))


def test_handshake_lost(client, scripted_server):
    """A connection that closes before the ``connection_response`` arrives raises the
    connection error."""

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        scripted.close()

    scripted = scripted_server(script)
    connection_error(lambda: client.connect(scripted.endpoint()))


def test_handshake_failed_write(client, server):
    """A ``connection_request`` that fails to be written raises the connection error,
    which carries the error of the write in its message or as its cause."""
    client.fail_write("Failed by the test")
    error = connection_error(lambda: client.connect(server))
    assert "Failed by the test" in error.message + error.cause


def test_handshake_malformed_size(client, server, scripted_server):
    """A ``connection_response`` whose size prefix has not ended within five bytes raises
    the connection error."""
    skip_when_framed(server)
    skip_when_resuming(server)

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        scripted.send_raw(MALFORMED_SIZE)

    scripted = scripted_server(script)
    connection_error(lambda: client.connect(scripted.endpoint()))


@should
@pytest.mark.parametrize("response", ["none", "partial"])
def test_handshake_timeout(client, server, scripted_server, response):
    """Connecting raises the connection error when the ``connection_response`` has not
    arrived in full after the handshake timeout. The stall timeout does not apply to it.
    The default is 5 seconds, and the program can set it."""
    if response == "partial":
        skip_when_framed(server)

    def script(scripted: ScriptedServer) -> None:
        scripted.receive_connection_request()
        if response == "partial":
            # The start of a response that never completes
            scripted.send_raw(encode_size(20) + b"\x2a")
        # No more, until the client gives up and closes
        while scripted.receive() is not None:
            pass

    scripted = scripted_server(script)
    start = time.monotonic()
    connection_error(
        lambda: client.connect(
            scripted.endpoint(), handshake_timeout=1, stall_timeout=0.25
        )
    )
    assert 0.75 <= time.monotonic() - start < 5


@should
@pytest.mark.parametrize("timeout", list(TIMEOUT_VALUES))
def test_handshake_timeout_values(client, server, scripted_server, timeout):
    """An infinite handshake timeout waits with no deadline. A zero, negative or NaN
    timeout raises the argument error."""
    seconds = TIMEOUT_VALUES[timeout]
    if timeout != "infinite":
        with pytest.raises(TesteeError) as info:
            client.connect(server, handshake_timeout=seconds)
        assert info.value.error.kind == pb.Error.ARGUMENT, info.value.error
        return

    def script(scripted: ScriptedServer) -> None:
        time.sleep(DELAY)
        scripted.accept()
        scripted.serve()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint(), handshake_timeout=seconds)
    try:
        assert values.native(get_client_name(client)) == ""
    finally:
        client.close()


section("Reading Messages")


# A client name long enough that the message's size prefix takes two bytes
LONG_NAME = "a" * 300


@pytest.mark.parametrize(
    "split", ["byte-at-a-time", "with-the-next", "with-the-whole-next"]
)
def test_message_split_across_reads(client, server, scripted_server, split):
    """The client reads a ``Message`` however the bytes arrive: one byte at a time,
    including a size prefix of two bytes, or in one read with all or the start of the
    next message."""
    skip_when_framed(server)
    # A Message holding field 15, which the client skips, as test_unknown_message checks
    next_message = frame(b"\x7a\x04abcd")

    def respond(scripted, request):
        response = KRPC.Response(id=request.id)
        response.results.add(value=encode_size(len(LONG_NAME)) + LONG_NAME.encode())
        data = frame(KRPC.Message(response=response).SerializeToString())
        if split == "byte-at-a-time":
            for byte in data:
                scripted.send_raw(bytes([byte]))
                time.sleep(0.002)
        elif split == "with-the-whole-next":
            scripted.send_raw(data + next_message)
        else:
            half = len(next_message) // 2
            scripted.send_raw(data + next_message[:half])
            time.sleep(0.1)
            scripted.send_raw(next_message[half:])

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        respond(scripted, scripted.answer_until("GetClientName"))
        scripted.serve()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint())
    try:
        assert values.native(get_client_name(client)) == LONG_NAME
        # The reads after the split message stay in step
        assert values.native(get_client_name(client)) == ""
    finally:
        client.close()


@protocol("websockets")
def test_websockets_fragments(client, server, scripted_server):
    """The client reads a ``Message`` split across several websockets frames."""

    def script(scripted: ScriptedServer) -> None:
        def respond(request):
            response = KRPC.Response(id=request.id)
            response.results.add(value=b"\x0afragmented")
            message = KRPC.Message(response=response).SerializeToString()
            scripted.websocket.send(message, fragments=3)

        scripted.accept()
        respond(scripted.answer_until("GetClientName"))
        scripted.serve()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint())
    try:
        assert values.native(get_client_name(client)) == "fragmented"
    finally:
        client.close()
    scripted.join()


def test_unknown_message(client, server, scripted_server):
    """Once the connection is established, the client skips a ``Message`` holding a case
    the protocol does not define."""

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    scripted = served(scripted_server(script), server)
    # A Message holding field 15, which no version of the protocol has used
    scripted.before_response["GetClientName"] = lambda call: scripted.send_payload(
        b"\x7a\x00"
    )
    client.connect(scripted.endpoint())
    try:
        assert values.native(get_client_name(client)) == ""
    finally:
        client.close()


def test_wrong_direction(client, server, scripted_server):
    """A ``Message`` holding a case the client sends, such as a ``request``, fails the
    connection. The client sends a ``disconnect`` with the reason ``PROTOCOL_ERROR``."""
    request = KRPC.Request(id=1)
    request.calls.add(service="KRPC", procedure="GetClientName")
    data = KRPC.Message(request=request).SerializeToString()
    protocol_error_test(client, server, scripted_server, data, "PROTOCOL_ERROR")


def test_malformed_message(client, server, scripted_server):
    """A message that fails to parse fails the connection. The client sends a
    ``disconnect`` with the reason ``MALFORMED_MESSAGE``."""
    # A field tag cut short
    protocol_error_test(
        client, server, scripted_server, b"\xff\xff", "MALFORMED_MESSAGE"
    )


def test_malformed_size(client, server, scripted_server):
    """A size prefix that has not ended within five bytes fails the connection. The client
    sends a ``disconnect`` with the reason ``MALFORMED_MESSAGE``."""
    skip_when_framed(server)
    protocol_error_test(
        client,
        server,
        scripted_server,
        MALFORMED_SIZE,
        "MALFORMED_MESSAGE",
        framed=False,
    )


def test_size_over_32_bits(client, server, scripted_server):
    """A size prefix whose value does not fit in 32 bits fails the connection. The client
    sends a ``disconnect`` with the reason ``MALFORMED_MESSAGE``."""
    skip_when_framed(server)
    protocol_error_test(
        client,
        server,
        scripted_server,
        b"\xff\xff\xff\xff\x7f",
        "MALFORMED_MESSAGE",
        framed=False,
    )


@should
def test_stall(client, server, scripted_server):
    """A message whose bytes stop arriving for the stall timeout, once its first byte has
    arrived, fails the connection with the reason ``TIMEOUT``. The default is 30
    seconds, and the program can set it."""
    skip_when_framed(server)
    reasons = []

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.answer_until("GetClientName")
        scripted.send_raw(encode_size(10) + b"\x12\x00")
        until_disconnect(scripted, reasons)

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint(), stall_timeout=1)
    try:
        connection_error(lambda: get_client_name(client))
    finally:
        client.close_quietly()
    scripted.join()
    assert reasons == ["TIMEOUT"]


@profile("streams")
@should
def test_stall_while_idle(client, server, scripted_server):
    """A message that stalls while no call is waiting for a response, such as a stream
    update that starts to arrive with the response to a call, fails the connection with
    the reason ``TIMEOUT``."""
    skip_when_framed(server)
    reasons = []

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()

        def respond(request):
            response = scripted.response(request)
            # The start of a stream update, in the same write as the response
            message = KRPC.Message(response=response).SerializeToString()
            scripted.send_raw(frame(message) + encode_size(20) + b"\x1a")

        respond(scripted.answer_until("AddStream"))
        until_disconnect(scripted, reasons)

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint(), stall_timeout=1)
    try:
        try:
            client.add_stream(
                "TestService", "Counter", values.string("stall"), values.sint32(1)
            )
        except TesteeError:
            pass
        scripted.join()
    finally:
        client.close_quietly()
    assert reasons == ["TIMEOUT"]


@should
@pytest.mark.parametrize("timeout", list(TIMEOUT_VALUES))
def test_stall_timeout_values(client, server, scripted_server, timeout):
    """An infinite stall timeout waits with no deadline. A zero, negative or NaN timeout
    raises the argument error."""
    seconds = TIMEOUT_VALUES[timeout]
    if timeout != "infinite":
        with pytest.raises(TesteeError) as info:
            client.connect(server, stall_timeout=seconds)
        assert info.value.error.kind == pb.Error.ARGUMENT, info.value.error
        return
    skip_when_framed(server)

    def respond(scripted, request):
        response = scripted.response(request)
        data = frame(KRPC.Message(response=response).SerializeToString())
        # The first byte of the response, and the rest after a delay
        scripted.send_raw(data[:1])
        time.sleep(DELAY)
        scripted.send_raw(data[1:])

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        respond(scripted, scripted.answer_until("GetClientName"))
        scripted.serve()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint(), stall_timeout=seconds)
    try:
        assert values.native(get_client_name(client)) == ""
    finally:
        client.close()


section("Closing")


def test_close(client, server, scripted_server):
    """Closing sends a ``disconnect`` with the reason ``CLOSED``. The client then closes
    its write side, and reads and discards until the server closes."""
    if server.protocol == "serialio":
        pytest.skip("A serial line has no half-close")
    if server.protocol == "websockets":
        pytest.skip("A websockets connection ends with a close frame")
    closed = []

    def script(server: ScriptedServer) -> None:
        server.accept()
        disconnect = server.serve()
        assert disconnect is not None
        assert disconnect.reason == KRPC.Disconnect.CLOSED
        # The client closes its write side, then reads until the server closes
        assert server.receive() is None
        time.sleep(DELAY)
        closed.append(time.monotonic())
        server.close()

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    client.close()
    returned = time.monotonic()
    scripted.join()
    assert closed, "The server did not see the client close its write side"
    assert returned >= closed[0], "Close returned before the server closed"
    assert returned - closed[0] < 1, "Close returned long after the server closed"


@protocol("websockets")
def test_websockets_close(client, scripted_server):
    """Over websockets, closing sends a ``disconnect`` with the reason ``CLOSED``, then a
    close frame. The client then waits for the server's close frame."""
    closed = []

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        disconnect = scripted.serve()
        assert disconnect is not None
        assert disconnect.reason == KRPC.Disconnect.CLOSED
        # The close frame follows the disconnect
        assert scripted.receive() is None
        assert scripted.websocket.close_received
        time.sleep(DELAY)
        closed.append(time.monotonic())
        scripted.websocket.send_close()

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    client.close()
    returned = time.monotonic()
    scripted.join()
    assert closed, "The server did not see the client's close frame"
    assert returned >= closed[0], "Close returned before the server's close frame"
    assert returned - closed[0] < 1, (
        "Close returned long after the server's close frame"
    )


def test_close_wait_limit(client, server, scripted_server):
    """Closing waits for the server to close for up to 1 second."""
    if server.protocol == "serialio":
        pytest.skip("A serial line has no half-close")
    returned = threading.Event()

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        assert scripted.serve() is not None
        # The server keeps the connection open until the close returns
        returned.wait(REPLY_TIMEOUT)

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    start = time.monotonic()
    try:
        client.close()
    finally:
        returned.set()
    # The limit, and a margin for the testee
    assert time.monotonic() - start < 1.5


@profile("threads")
def test_close_interrupted(client, scripted_server):
    """Closing from a thread with an interrupt pending sends a ``disconnect`` with the
    reason ``CLOSED`` and waits for the server to close. The interrupt is
    still pending once the close returns."""
    disconnects = []

    def script(server: ScriptedServer) -> None:
        server.accept()
        disconnects.append(server.serve())
        assert server.receive() is None
        server.close()

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    try:
        assert client.close_interrupted() is True
    finally:
        # A second close does nothing, and closes a client that cannot interrupt
        client.close()
    scripted.join()
    assert disconnects[0] is not None
    assert disconnects[0].reason == KRPC.Disconnect.CLOSED


def test_close_twice(connected):
    """Closing a connection that is already closed does nothing."""
    connected.close()
    connected.close()


section("Losing the Connection")


@pytest.mark.parametrize("ending", ["disconnect", "no-disconnect", "partway"])
def test_lost(client, server, scripted_server, ending):
    """The connection fails when the server sends a ``disconnect``, or the connection ends
    without one, including partway through a message. The waiting call raises the
    connection error, with the reason and message of the ``disconnect``."""
    if ending == "partway":
        skip_when_framed(server)
    scripted = served(scripted_server(ending_script(ending)), server)
    client.connect(scripted.endpoint())
    try:
        error = connection_error(lambda: get_client_name(client))
    finally:
        client.close_quietly()
    if ending == "disconnect":
        assert error.reason == "REMOVED"
        assert REMOVED_MESSAGE in error.message


@profile("threads")
def test_lost_pending_calls(client, server, scripted_server):
    """The calls waiting for a response when the connection fails raise the connection
    error."""
    held = []

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        while len(held) < 2:
            message = scripted.receive()
            assert message is not None
            if message.WhichOneof("content") != "request":
                continue
            if is_call(message.request.calls[0], "Int32ToString"):
                held.append(message.request)
            else:
                scripted.answer(message.request)
        disconnect = KRPC.Disconnect(
            reason=KRPC.Disconnect.SERVER_STOPPED, message="Stopped by the test"
        )
        scripted.send(KRPC.Message(disconnect=disconnect))
        scripted.close()

    scripted = served(scripted_server(script), server)
    client.connect(scripted.endpoint())
    try:
        calls = [
            client.start(thread, invoke=invocation("TestService", "Int32ToString", x))
            for thread, x in (("a", values.sint32(1)), ("b", values.sint32(2)))
        ]
        for call in calls:
            error = call.error()
            assert error.kind == pb.Error.CONNECTION, error
            assert error.reason == "SERVER_STOPPED", error
    finally:
        client.close()


def test_failed_write(connected):
    """A call whose request fails to be written fails the connection. The call raises the
    connection error, which carries the error of the write in its message or as its cause.
    A later call raises the connection error at once."""
    connected.fail_write("Failed by the test")
    error = connection_error(lambda: get_client_name(connected))
    assert "Failed by the test" in error.message + error.cause
    start = time.monotonic()
    connection_error(lambda: get_client_name(connected))
    assert time.monotonic() - start < AT_ONCE


@pytest.mark.parametrize("ending", ["close", "disconnect"])
def test_call_after_end(client, server, scripted_server, ending):
    """Once the connection is closed or has failed, each call raises the connection error
    at once."""
    scripted = served(scripted_server(ending_script(ending)), server)
    client.connect(scripted.endpoint())
    try:
        if ending == "close":
            client.close()
        else:
            connection_error(lambda: get_client_name(client))
        start = time.monotonic()
        connection_error(lambda: get_client_name(client))
        assert time.monotonic() - start < AT_ONCE
    finally:
        client.close_quietly()


section(
    "Resuming a Serial Line",
    "These requirements let a client that may be resuming a serial line start a new session. "
    "The line can hold what an earlier session left on it. See "
    ":ref:`communication-protocol-serialio-resetting`.",
)


def resume(client, server, scripted_server, script, connects=True):
    """Connect a client resuming a serial line to a script, and check it connects or fails."""
    scripted = served(scripted_server(script), server)
    endpoint = scripted.endpoint()
    try:
        if not connects:
            connection_error(
                lambda: client.connect(
                    endpoint, handshake_timeout=RESUME_HANDSHAKE_TIMEOUT
                )
            )
        else:
            client.connect(endpoint, handshake_timeout=RESUME_HANDSHAKE_TIMEOUT)
            try:
                get_client_name(client)
            finally:
                client.close()
    except BaseException:
        # A serial server sees no close, so a script waiting on the client is woken
        scripted.close()
        raise
    scripted.join()


@protocol("serialio")
@pytest.mark.parametrize("message", sorted(LEFTOVERS))
def test_serialio_leftovers(client, server, scripted_server, message):
    """While it waits for its ``connection_response``, the client skips a ``response`` or a
    ``stream_update``. Either can be left over from an earlier session."""
    resume(client, server, scripted_server, leftover(LEFTOVERS[message]))


@protocol("serialio")
@pytest.mark.parametrize("answer", sorted(NO_ANSWERS))
def test_serialio_no_answer(client, server, scripted_server, answer):
    """The client treats a ``disconnect``, and the statuses ``MALFORMED_MESSAGE`` and
    ``TIMEOUT``, as no answer. Each can follow a request that landed in a stale partial
    message. The client stays silent until its handshake timeout, then sends the request
    again. It sends it no sooner than 3 seconds after the first, the server's stall timeout
    for a partial message."""
    resume(client, server, scripted_server, NO_ANSWERS[answer])


@protocol("serialio")
@pytest.mark.parametrize("status", ["DENIED", "WRONG_PROTOCOL_VERSION"])
def test_serialio_refusal(client, server, scripted_server, status):
    """The client never sends the request again after ``DENIED`` or
    ``WRONG_PROTOCOL_VERSION``."""
    resume(client, server, scripted_server, refusal(status), connects=False)


@protocol("serialio")
def test_serialio_repeated_connection_response(client, server, scripted_server):
    """The client reads ``connection_response`` messages until the first ``response`` of the
    session. Retries queue on the line while the server is not reading, and the server
    answers each with a new session. The client continues in the last one."""
    resume(client, server, scripted_server, second_connection_response)
