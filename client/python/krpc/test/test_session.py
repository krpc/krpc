import socket
import threading
import unittest
from collections.abc import Callable

import krpc
import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.session import Session


class ScriptedServer:
    """A server that runs a script against the first connection it accepts. The script
    reads and writes messages through the server's helpers."""

    def __init__(self, script: Callable[["ScriptedServer"], None]) -> None:
        self._listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._listener.bind(("127.0.0.1", 0))
        self._listener.listen(1)
        self.port = self._listener.getsockname()[1]
        self._script = script
        self._socket: socket.socket | None = None
        self._buffer = b""
        self._thread = threading.Thread(target=self._run, daemon=True)
        self._thread.start()

    def _run(self) -> None:
        self._socket, _ = self._listener.accept()
        try:
            self._script(self)
        except OSError:
            pass

    def join(self) -> None:
        self._thread.join(10)
        if self._socket is not None:
            self._socket.close()
        self._listener.close()

    def receive(self) -> KRPC.Message | None:
        """The next message from the client, or None at the end of the stream."""
        assert self._socket is not None
        while True:
            try:
                size, prefix_length = Decoder.decode_size_prefix(self._buffer)
                if len(self._buffer) >= prefix_length + size:
                    data = self._buffer[prefix_length : prefix_length + size]
                    self._buffer = self._buffer[prefix_length + size :]
                    message = KRPC.Message.FromString(data)
                    return message
            except IndexError:
                pass
            data = self._socket.recv(4096)
            if not data:
                return None
            self._buffer += data

    def send(self, message: KRPC.Message) -> None:
        assert self._socket is not None
        self._socket.sendall(Encoder.encode_message_with_size(message))

    def accept_handshake(self) -> None:
        request = self.receive()
        assert request is not None
        assert request.WhichOneof("content") == "connection_request"
        self.send(
            KRPC.Message(
                connection_response=KRPC.ConnectionResponse(
                    status=KRPC.ConnectionResponse.OK,
                    protocol_version=2,
                    client_identifier=b"\x01" * 16,
                )
            )
        )

    def answer_get_services(self) -> None:
        message = self.receive()
        assert message is not None
        services = KRPC.Services()
        response = KRPC.Response(id=message.request.id)
        response.results.add(value=services.SerializeToString())
        self.send(KRPC.Message(response=response))

    def answer_until_close(self) -> None:
        """Answer each request with the string "x", until the client closes."""
        while True:
            message = self.receive()
            if message is None or not message.HasField("request"):
                break
            response = KRPC.Response(id=message.request.id)
            response.results.add(value=b"\x01x")
            self.send(KRPC.Message(response=response))
        self.wait_for_close()

    def wait_for_close(self) -> None:
        while self.receive() is not None:
            pass


class InterruptingLock:
    """Wraps a session's lock, and raises KeyboardInterrupt once on leaving a with block
    on the test thread when the trigger holds, as a signal arriving there would."""

    def __init__(self, session: Session, trigger: Callable[[], bool]) -> None:
        self._lock = session._lock
        self._trigger = trigger
        self._thread = threading.current_thread()
        self.armed = True

    def __enter__(self) -> None:
        self._lock.acquire()

    def __exit__(self, *args: object) -> None:
        self._lock.release()
        if (
            self.armed
            and threading.current_thread() is self._thread
            and self._trigger()
        ):
            self.armed = False
            raise KeyboardInterrupt


def connect(server: ScriptedServer) -> krpc.Client:
    return krpc.connect(
        name="session_test",
        port=server.port,
        use_pregenerated_stubs=False,
        handshake_timeout=5.0,
        stall_timeout=30.0,
    )


def status_request(conn: krpc.Client) -> object:
    return conn._invoke("KRPC", "GetClientName", [], [], conn._types.string_type)


class TestSession(unittest.TestCase):
    def test_interrupt_once_the_reader_role_is_taken_releases_it(self) -> None:
        server = ScriptedServer(answer_requests)
        conn = connect(server)
        session = conn._session
        lock = InterruptingLock(session, lambda: session._callers > 0)
        session._lock = lock  # pyright: ignore[reportAttributeAccessIssue]
        with self.assertRaises(KeyboardInterrupt):
            status_request(conn)
        self.assertFalse(lock.armed)
        self.assertFalse(session._reading)
        self.assertEqual(0, session._callers)
        self.assertEqual("x", status_request(conn))
        conn.close()
        server.join()

    def test_interrupt_once_the_reader_role_is_released_releases_it_once(
        self,
    ) -> None:
        server = ScriptedServer(answer_requests)
        conn = connect(server)
        session = conn._session
        called = threading.Event()
        lock = InterruptingLock(
            session, lambda: called.is_set() and session._callers == 0
        )
        session._lock = lock  # pyright: ignore[reportAttributeAccessIssue]
        send_request = session._connection.send_request

        def send_and_mark(request: KRPC.Request) -> None:
            send_request(request)
            called.set()

        session._connection.send_request = send_and_mark
        with self.assertRaises(KeyboardInterrupt):
            status_request(conn)
        self.assertFalse(lock.armed)
        self.assertFalse(session._reading)
        self.assertEqual(0, session._callers)
        self.assertEqual("x", status_request(conn))
        conn.close()
        server.join()


def answer_requests(server: ScriptedServer) -> None:
    server.accept_handshake()
    server.answer_get_services()
    server.answer_until_close()


if __name__ == "__main__":
    unittest.main()
