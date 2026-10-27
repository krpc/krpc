"""A server that plays a scripted exchange of kRPC messages with the client under test, for the
handshake and disconnect rules a TestServer cannot be made to break."""

from __future__ import annotations

import errno
import os
import select
import shutil
import socket
import tempfile
import threading
from collections.abc import Callable, Iterable

import serverprocess

import krpc.schema.KRPC_pb2 as KRPC
from driver import raw
from driver.framing import CLIENT_IDENTIFIER, PROTOCOL_VERSION, frame, receive_frame
from driver.testee import Endpoint
from driver.websocket import WebSocket

# The time a script has to finish once the test is done with it
JOIN_TIMEOUT = 10

# The time the test waits for the client to make a call
CALL_TIMEOUT = 10


def definitions(server: Endpoint, *names: str) -> bytes:
    """The serialized definitions of the named services, from a server, for a scripted server
    to answer GetServices with."""
    services = KRPC.Services.FromString(raw.call(server, "KRPC", "GetServices").value)
    result = KRPC.Services()
    result.services.extend(x for x in services.services if x.name in names)
    return result.SerializeToString()


def is_call(call: KRPC.ProcedureCall, procedure: str) -> bool:
    """Whether a call is to the procedure. A scripted server has no table of procedure ids.
    So a call by id matches every procedure, which suits a script waiting for one call."""
    # Note: match the id against the name hash once procedure ids are name hashes
    return call.procedure == procedure or (
        call.procedure == "" and call.procedure_id != 0
    )


class SerialLine:
    """A pair of pseudo-terminals that socat links, with the server's end open. It reads and
    writes as a socket does, and reads as closed once socat stops, as it does when the client
    closes its end."""

    def __init__(self) -> None:
        self._directory = tempfile.mkdtemp()
        self._socat, server_port, self.client_port = serverprocess.start_socat(
            self._directory
        )
        self._fd = os.open(server_port, os.O_RDWR | os.O_NOCTTY)
        # The script's thread and the test's both close the line
        self._close_lock = threading.Lock()

    def fileno(self) -> int:
        return self._fd

    def recv(self, size: int) -> bytes:
        try:
            return os.read(self._fd, size)
        except OSError as exn:
            # The other end of a pseudo-terminal closed, or the test closed the line
            if exn.errno in (errno.EIO, errno.EBADF):
                return b""
            raise

    def sendall(self, data: bytes) -> None:
        view = memoryview(data)
        while view:
            view = view[os.write(self._fd, view) :]

    def shutdown(self, how: int) -> None:
        """Stop socat, which wakes a read."""
        serverprocess.stop(self._socat)

    def close(self) -> None:
        serverprocess.stop(self._socat)
        with self._close_lock:
            fd, self._fd = self._fd, -1
        if fd >= 0:
            os.close(fd)
        shutil.rmtree(self._directory, ignore_errors=True)


class ScriptedServer:
    """Listens on a TCP/IP port, on a local socket path when given one, or on a serial line,
    and runs a script against the first connection it accepts. Over websockets, it accepts the
    upgrade before the script runs. An error the script raises, such as a failed assertion, is
    raised again by join."""

    def __init__(
        self,
        script: Callable[[ScriptedServer], None],
        protocol: str = "tcpip",
        path: str = "",
    ) -> None:
        # A path is for a local socket, whatever the protocol under test
        self.protocol = "localsocket" if path else protocol
        self.path = path
        self.port = 0
        self._listener: socket.socket | None = None
        self._line: SerialLine | None = None
        if self.protocol == "serialio":
            self._line = SerialLine()
        elif path:
            # A socket file a crashed run left behind
            if os.path.exists(path):
                os.remove(path)
            self._listener = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            self._listener.bind(path)
        else:
            self._listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self._listener.bind(("127.0.0.1", 0))
            self.port = self._listener.getsockname()[1]
        if self._listener is not None:
            self._listener.listen(1)
        self._script = script
        self._socket: socket.socket | SerialLine | WebSocket | None = None
        self._error: BaseException | None = None
        self._send_lock = threading.Lock()
        self._condition = threading.Condition()
        # The calls answered by serve, each recorded once its response is sent
        self._calls: list[KRPC.ProcedureCall] = []
        self._next_stream_id = 1
        # The serialized Services message serve answers GetServices with
        self.services = KRPC.Services().SerializeToString()
        # Calls serve runs before it answers a call, by procedure name
        self.before_response: dict[str, Callable[[KRPC.ProcedureCall], None]] = {}
        # Messages serve sends with a call's response, in the same write, by procedure name
        self.after_response: dict[
            str, Callable[[KRPC.ProcedureCall], list[KRPC.Message]]
        ] = {}
        self._joined = False
        self._thread = threading.Thread(target=self._run, daemon=True)
        self._thread.start()

    def endpoint(self) -> Endpoint:
        """Where the server listens."""
        if self._line is not None:
            return Endpoint(self.protocol, path=self._line.client_port)
        if self.path:
            return Endpoint(self.protocol, path=self.path)
        return Endpoint(self.protocol, address="127.0.0.1", port=self.port)

    def _run(self) -> None:
        try:
            if self._listener is not None:
                self._socket, _ = self._listener.accept()
                if not self.path:
                    # Each write is its own segment, for a test that splits a message
                    self._socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                if self.protocol == "websockets":
                    self._socket = WebSocket(self._socket)
            else:
                assert self._line is not None
                self._socket = self._line
            self._script(self)
            # A client that closed waits for the server to close, so a finished script closes
            self._socket.close()
        except BaseException as exn:  # noqa: BLE001
            self._error = exn

    def join(self) -> None:
        """Wait for the script, then close the server and remove its socket file. A second
        call does nothing, so it cannot remove a socket another server made since."""
        if self._joined:
            return
        self._joined = True
        self._thread.join(JOIN_TIMEOUT)
        if self._socket is not None:
            self._socket.close()
        if self._listener is not None:
            self._listener.close()
        if self._line is not None:
            self._line.close()
        if self.path and os.path.exists(self.path):
            os.remove(self.path)
        if self._thread.is_alive():
            raise AssertionError("The script did not finish")
        if self._error is not None:
            raise self._error

    def abandon(self) -> BaseException | None:
        """Close the server without waiting for its script, for a test that failed or that
        its testee could not carry out. Shutting the listener down wakes a script waiting to
        accept. Returns the error of a script that failed before the close."""
        if self._joined:
            return None
        self._joined = True
        error = self._error if not self._thread.is_alive() else None
        if self._listener is not None:
            try:
                self._listener.shutdown(socket.SHUT_RDWR)
            except OSError:
                # Not listening, as the script accepted
                pass
            self._listener.close()
        if self._socket is not None:
            self._socket.close()
        if self._line is not None:
            self._line.close()
        self._thread.join(JOIN_TIMEOUT)
        if self.path and os.path.exists(self.path):
            os.remove(self.path)
        return error

    def close(self) -> None:
        """Close the connection to the client. Shutting it down first wakes a script waiting
        to receive."""
        assert self._socket is not None
        try:
            self._socket.shutdown(socket.SHUT_RDWR)
        except OSError:
            # Closed already, as the script finished
            pass
        self._socket.close()

    def receive(self) -> KRPC.Message | None:
        """The next message from the client, or None once it closes its write side."""
        assert self._socket is not None
        if isinstance(self._socket, WebSocket):
            data = self._socket.receive()
        else:
            data = receive_frame(self._socket)
        return None if data is None else KRPC.Message.FromString(data)

    @property
    def websocket(self) -> WebSocket:
        """The websockets connection, over websockets."""
        assert isinstance(self._socket, WebSocket)
        return self._socket

    def readable(self, timeout: float) -> bool:
        """Whether the client sends data, or closes, within the timeout."""
        assert self._socket is not None
        return bool(select.select([self._socket], [], [], timeout)[0])

    def send(self, message: KRPC.Message) -> None:
        """Send a message. The script and the test can both send."""
        self.send_payload(message.SerializeToString())

    def send_payload(self, data: bytes) -> None:
        """Send the bytes of one message, framed for the protocol, such as a message that
        fails to parse."""
        if isinstance(self._socket, WebSocket):
            with self._send_lock:
                self._socket.send(data)
        else:
            self.send_raw(frame(data))

    def send_raw(self, data: bytes) -> None:
        """Send bytes as they are, such as a frame that is cut short. Websockets frames each
        message, so a test that sends raw bytes skips websockets."""
        assert self._socket is not None
        assert not isinstance(self._socket, WebSocket)
        with self._send_lock:
            self._socket.sendall(data)

    def receive_connection_request(self) -> KRPC.ConnectionRequest:
        message = self.receive()
        assert message is not None
        assert message.WhichOneof("content") == "connection_request"
        return message.connection_request

    def accept(self) -> KRPC.ConnectionRequest:
        """Accept the connection request, and return it."""
        request = self.receive_connection_request()
        self.send(
            KRPC.Message(
                connection_response=KRPC.ConnectionResponse(
                    status=KRPC.ConnectionResponse.OK,
                    protocol_version=PROTOCOL_VERSION,
                    client_identifier=CLIENT_IDENTIFIER,
                )
            )
        )
        return request

    def refuse(self, status: str, message: str) -> None:
        """Refuse the connection request with the named status."""
        self.receive_connection_request()
        self.send(
            KRPC.Message(
                connection_response=KRPC.ConnectionResponse(
                    status=status, message=message
                )
            )
        )

    def serve(self) -> KRPC.Disconnect | None:
        """Answer each request, until a disconnect or the end of the stream. GetServices gets
        the services attribute, AddStream and TestService.OnTimer a new stream id, and any
        other call a zero value.
        Each call is recorded once its response is sent."""
        while True:
            message = self.receive()
            if message is None:
                return None
            case = message.WhichOneof("content")
            if case == "disconnect":
                return message.disconnect
            assert case == "request"
            response = KRPC.Response(id=message.request.id)
            following = []
            for call in message.request.calls:
                for procedure, before in self.before_response.items():
                    if is_call(call, procedure):
                        before(call)
                response.results.add(value=self._result(call))
                for procedure, after in self.after_response.items():
                    if is_call(call, procedure):
                        following.extend(after(call))
            sent = [KRPC.Message(response=response)] + following
            if isinstance(self._socket, WebSocket):
                with self._send_lock:
                    self._socket.send_together([x.SerializeToString() for x in sent])
            else:
                self.send_raw(b"".join(frame(x.SerializeToString()) for x in sent))
            with self._condition:
                self._calls.extend(message.request.calls)
                self._condition.notify_all()

    def response(self, request: KRPC.Request) -> KRPC.Response:
        """The response serve gives a request."""
        response = KRPC.Response(id=request.id)
        for call in request.calls:
            response.results.add(value=self._result(call))
        return response

    def answer(self, request: KRPC.Request) -> None:
        """Answer a request as serve does, with no other messages."""
        self.send(KRPC.Message(response=self.response(request)))

    def answer_each(
        self, result: Callable[[KRPC.Request], KRPC.ProcedureResult]
    ) -> None:
        """Answer each request until the client stops sending them: GetServices with the
        services attribute, and any other request with the one result the function gives."""
        while True:
            message = self.receive()
            if message is None or message.WhichOneof("content") != "request":
                return
            response = KRPC.Response(id=message.request.id)
            if message.request.calls[0].procedure == "GetServices":
                response.results.add(value=self.services)
            else:
                response.results.append(result(message.request))
            self.send(KRPC.Message(response=response))

    def answer_until(self, procedure: str) -> KRPC.Request:
        """Answer each request as serve does, until a call of the procedure, and return that
        request unanswered."""
        while True:
            message = self.receive()
            assert message is not None
            if message.WhichOneof("content") != "request":
                continue
            if is_call(message.request.calls[0], procedure):
                return message.request
            self.answer(message.request)

    def _result(self, call: KRPC.ProcedureCall) -> bytes:
        if call.service == "KRPC" and call.procedure == "GetServices":
            return self.services
        if call.service == "KRPC" and call.procedure == "AddStream":
            stream_id = self._next_stream_id
            self._next_stream_id += 1
            return KRPC.Stream(id=stream_id).SerializeToString()
        if call.service == "KRPC" and call.procedure == "RemoveStream":
            return b""
        if call.service == "TestService" and call.procedure == "OnTimer":
            stream_id = self._next_stream_id
            self._next_stream_id += 1
            return KRPC.Event(stream=KRPC.Stream(id=stream_id)).SerializeToString()
        return b"\x00"

    def wait_for_call(self, procedure: str, count: int = 1) -> list[KRPC.ProcedureCall]:
        """The answered calls of a procedure, once there are at least count of them."""

        def calls() -> list[KRPC.ProcedureCall]:
            return [x for x in self._calls if is_call(x, procedure)]

        with self._condition:
            if not self._condition.wait_for(
                lambda: len(calls()) >= count, CALL_TIMEOUT
            ):
                raise AssertionError("The client did not call %s" % procedure)
            return calls()

    def send_update(self, results: Iterable[tuple[int, bytes]]) -> None:
        """Send a stream update, with a value for each stream id."""
        update = KRPC.StreamUpdate()
        for stream_id, value in results:
            update.results.add(id=stream_id, result=KRPC.ProcedureResult(value=value))
        self.send(KRPC.Message(stream_update=update))
