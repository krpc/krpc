import math
import socket
import threading
import time
import unittest
from collections.abc import Callable

import krpc
import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.error import ConnectionError
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
        self.received: list[KRPC.Message] = []
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
                    self.received.append(message)
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

    def send_bytes(self, data: bytes) -> None:
        assert self._socket is not None
        self._socket.sendall(data)

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

    def close(self) -> None:
        assert self._socket is not None
        self._socket.shutdown(socket.SHUT_RDWR)


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


def connect(
    server: ScriptedServer,
    handshake_timeout: float | None = 5.0,
    stall_timeout: float | None = 30.0,
) -> krpc.Client:
    return krpc.connect(
        name="session_test",
        port=server.port,
        use_pregenerated_stubs=False,
        handshake_timeout=handshake_timeout,
        stall_timeout=stall_timeout,
    )


def status_request(conn: krpc.Client) -> object:
    return conn._invoke("KRPC", "GetClientName", [], [], conn._types.string_type)


class TestSession(unittest.TestCase):
    def test_handshake_sends_protocol_version_2(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.wait_for_close()

        server = ScriptedServer(script)
        connect(server).close()
        server.join()
        request = server.received[0].connection_request
        self.assertEqual(2, request.protocol_version)
        self.assertEqual("session_test", request.client_name)

    def test_denied(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.receive()
            server.send(
                KRPC.Message(
                    connection_response=KRPC.ConnectionResponse(
                        status=KRPC.ConnectionResponse.DENIED,
                        message="The connection was denied",
                    )
                )
            )

        server = ScriptedServer(script)
        with self.assertRaises(ConnectionError) as context:
            connect(server)
        server.join()
        self.assertEqual("DENIED", context.exception.status)
        self.assertIn("The connection was denied", str(context.exception))

    def test_wrong_protocol_version_in_response(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.receive()
            server.send(
                KRPC.Message(
                    connection_response=KRPC.ConnectionResponse(
                        status=KRPC.ConnectionResponse.OK
                    )
                )
            )

        server = ScriptedServer(script)
        with self.assertRaisesRegex(ConnectionError, "protocol version 2"):
            connect(server)
        server.join()

    def test_disconnect_during_the_handshake(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.receive()
            server.send(
                KRPC.Message(
                    disconnect=KRPC.Disconnect(
                        reason=KRPC.Disconnect.SERVER_STOPPED, message="Stopping"
                    )
                )
            )

        server = ScriptedServer(script)
        with self.assertRaises(ConnectionError) as context:
            connect(server)
        server.join()
        self.assertEqual("SERVER_STOPPED", context.exception.reason)

    def test_unknown_disconnect_reason_during_the_handshake(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.receive()
            server.send(
                KRPC.Message(
                    disconnect=KRPC.Disconnect(
                        reason=99,  # pyright: ignore[reportArgumentType]
                        message="A newer reason",
                    )
                )
            )

        server = ScriptedServer(script)
        with self.assertRaises(ConnectionError) as context:
            connect(server)
        server.join()
        self.assertEqual("99", context.exception.reason)
        self.assertIn("A newer reason", str(context.exception))

    def test_infinite_stall_timeout(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server, handshake_timeout=math.inf, stall_timeout=math.inf)
        conn.close()
        server.join()

    def test_unexpected_message_during_the_handshake(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.receive()
            server.send(KRPC.Message())

        server = ScriptedServer(script)
        with self.assertRaisesRegex(ConnectionError, "unknown kind"):
            connect(server)
        server.join()

    def test_handshake_timeout(self) -> None:
        release = threading.Event()

        def script(server: ScriptedServer) -> None:
            server.receive()
            release.wait(10)

        server = ScriptedServer(script)
        start = time.monotonic()
        with self.assertRaisesRegex(ConnectionError, "did not answer"):
            connect(server, handshake_timeout=0.2)
        self.assertLess(time.monotonic() - start, 5)
        release.set()
        server.join()

    def test_stall_timeout(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.receive()
            # A response cut short
            server.send_bytes(b"\x0a\x12")
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server, stall_timeout=0.2)
        start = time.monotonic()
        with self.assertRaisesRegex(ConnectionError, "stalled") as context:
            status_request(conn)
        self.assertLess(time.monotonic() - start, 5)
        self.assertIsNone(context.exception.reason)
        conn.close()
        server.join()
        last = server.received[-1]
        self.assertEqual(KRPC.Disconnect.TIMEOUT, last.disconnect.reason)

    def test_close_sends_a_disconnect(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.wait_for_close()

        server = ScriptedServer(script)
        connect(server).close()
        server.join()
        last = server.received[-1]
        self.assertEqual("disconnect", last.WhichOneof("content"))
        self.assertEqual(KRPC.Disconnect.CLOSED, last.disconnect.reason)

    def test_close_returns_once_the_server_closes(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.wait_for_close()
            server.close()

        server = ScriptedServer(script)
        conn = connect(server)
        start = time.monotonic()
        conn.close()
        self.assertLess(time.monotonic() - start, 0.5)
        server.join()

    def test_concurrent_calls_answered_out_of_order(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            requests = [server.receive(), server.receive()]
            for request in reversed(requests):
                assert request is not None
                # Answered with the name of the procedure called
                response = KRPC.Response(id=request.request.id)
                value = request.request.calls[0].procedure.encode()
                response.results.add(value=bytes([len(value)]) + value)
                server.send(KRPC.Message(response=response))
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        results: dict[str, object] = {}

        def call(procedure: str) -> None:
            results[procedure] = conn._invoke(
                "KRPC", procedure, [], [], conn._types.string_type
            )

        procedures = ("GetClientName", "GetStatus")
        threads = [threading.Thread(target=call, args=(x,)) for x in procedures]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join(5)
        conn.close()
        server.join()
        self.assertEqual({x: x for x in procedures}, results)
        ids = {x.request.id: x for x in server.received if x.HasField("request")}
        self.assertEqual({1, 2, 3}, set(ids))

    def test_close_fails_a_call_in_flight(self) -> None:
        received = threading.Event()
        closed = threading.Event()

        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            message = server.receive()
            assert message is not None
            received.set()
            # The disconnect from close, sent while the call holds the reader role
            disconnect = server.receive()
            assert disconnect is not None
            time.sleep(0.1)
            response = KRPC.Response(id=message.request.id)
            response.results.add(value=b"\x01x")
            server.send(KRPC.Message(response=response))
            server.wait_for_close()
            closed.set()
            server.close()

        server = ScriptedServer(script)
        conn = connect(server)
        errors: list[Exception] = []

        def call() -> None:
            try:
                status_request(conn)
            except ConnectionError as exn:
                errors.append(exn)

        thread = threading.Thread(target=call)
        thread.start()
        self.assertTrue(received.wait(5))
        start = time.monotonic()
        conn.close()
        elapsed = time.monotonic() - start
        thread.join(5)
        server.join()
        # The response arrives after the disconnect, so it does not reach the call
        self.assertEqual(1, len(errors))
        self.assertTrue(closed.is_set())
        self.assertLess(elapsed, 0.5)
        last = server.received[-1]
        self.assertEqual(KRPC.Disconnect.CLOSED, last.disconnect.reason)

    def test_stream_update_read_with_a_response_is_delivered(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            message = server.receive()
            assert message is not None
            response = KRPC.Response(id=message.request.id)
            response.results.add(value=b"\x01x")
            update = KRPC.StreamUpdate()
            update.results.add(id=1, result=KRPC.ProcedureResult(value=b"\x01y"))
            # Both in one write, so the caller reads both at once
            server.send_bytes(
                Encoder.encode_message_with_size(KRPC.Message(response=response))
                + Encoder.encode_message_with_size(KRPC.Message(stream_update=update))
            )
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        stream = conn._stream_manager.get_stream(conn._types.string_type, 1)
        self.assertEqual("x", status_request(conn))
        deadline = time.monotonic() + 2
        while stream.announced_version == 0 and time.monotonic() < deadline:
            time.sleep(0.01)
        self.assertEqual("y", stream.value)
        conn.close()
        server.join()

    def test_requests_carry_increasing_ids(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            for _ in range(2):
                message = server.receive()
                assert message is not None
                response = KRPC.Response(id=message.request.id)
                response.results.add(value=b"\x01x")
                server.send(KRPC.Message(response=response))
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        status_request(conn)
        status_request(conn)
        conn.close()
        server.join()
        ids = [x.request.id for x in server.received if x.HasField("request")]
        self.assertEqual([1, 2, 3], ids)

    def test_unknown_case_is_skipped(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            message = server.receive()
            assert message is not None
            # A message holding field 99, a case this client does not know
            server.send_bytes(b"\x04\x9a\x06\x01\x00")
            response = KRPC.Response(id=message.request.id)
            response.results.add(value=b"\x01x")
            server.send(KRPC.Message(response=response))
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        self.assertEqual("x", status_request(conn))
        conn.close()
        server.join()

    def test_disconnect_fails_a_waiting_call(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.receive()
            server.send(
                KRPC.Message(
                    disconnect=KRPC.Disconnect(
                        reason=KRPC.Disconnect.REMOVED, message="Removed"
                    )
                )
            )

        server = ScriptedServer(script)
        conn = connect(server)
        with self.assertRaises(ConnectionError) as context:
            status_request(conn)
        self.assertEqual("REMOVED", context.exception.reason)
        # A later call raises the same error at once
        with self.assertRaises(ConnectionError) as later:
            status_request(conn)
        self.assertIs(context.exception, later.exception)
        conn.close()
        server.join()

    def test_response_with_an_unknown_id_is_a_protocol_error(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.receive()
            server.send(KRPC.Message(response=KRPC.Response(id=99)))
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        with self.assertRaisesRegex(ConnectionError, "unknown id"):
            status_request(conn)
        conn.close()
        server.join()
        last = server.received[-1]
        self.assertEqual(KRPC.Disconnect.PROTOCOL_ERROR, last.disconnect.reason)

    def test_malformed_response_is_a_protocol_error(self) -> None:
        def script(server: ScriptedServer) -> None:
            server.accept_handshake()
            server.answer_get_services()
            server.receive()
            # A message holding a response whose bytes do not parse
            server.send_bytes(b"\x04\x12\x02\xff\xff")
            server.wait_for_close()

        server = ScriptedServer(script)
        conn = connect(server)
        with self.assertRaisesRegex(ConnectionError, "Failed to parse"):
            status_request(conn)
        conn.close()
        server.join()
        last = server.received[-1]
        self.assertEqual(KRPC.Disconnect.MALFORMED_MESSAGE, last.disconnect.reason)

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

    def test_failed_write_ends_the_connection(self) -> None:
        server = ScriptedServer(answer_requests)
        conn = connect(server)
        session = conn._session

        def send_request(request: KRPC.Request) -> None:
            raise OSError("write failed")

        session._connection.send_request = send_request
        with self.assertRaisesRegex(ConnectionError, "write failed"):
            status_request(conn)
        session.dispatcher.join(5)
        self.assertFalse(session.dispatcher.is_alive())
        session.reader.join(5)
        self.assertFalse(session.reader.is_alive())
        conn.close()
        server.join()


def answer_requests(server: ScriptedServer) -> None:
    server.accept_handshake()
    server.answer_get_services()
    server.answer_until_close()


if __name__ == "__main__":
    unittest.main()
