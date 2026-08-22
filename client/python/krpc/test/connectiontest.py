import os
import shutil
import socket
import tempfile
import threading
import unittest
from typing import Any, List, Optional
import krpc.schema.KRPC_pb2 as KRPC
from krpc.connection import Connection

# The unix domain address family, where the socket module has it. It is absent on Windows,
# so it is read once here rather than reached for through the module, which lets the code
# below name the family on every platform. The tests that listen on one are skipped where
# it is missing.
AF_UNIX = getattr(socket, "AF_UNIX", None)


class EchoServer:
    """A stand-in for a kRPC server, which sends back whatever it is sent and closes the
    connection when it is sent "disconnect". These tests are about how a transport moves
    bytes, so nothing above the transport has to be understood to answer them."""

    def __init__(self, family: int) -> None:
        self._family = family
        self._listener = socket.socket(family, socket.SOCK_STREAM)
        self._directory: Optional[str] = None
        self._address: Any = None

    @property
    def address(self) -> Any:
        """Where a client connects to the server: a host and port pair over TCP/IP, and
        a socket path over a unix domain socket."""
        return self._address

    def start(self) -> None:
        if AF_UNIX is not None and self._family == AF_UNIX:
            # A socket path has to fit in the kernel's address structure, which leaves
            # far less room than a path named after the test would take, so the socket
            # goes in a short temporary directory of its own
            self._directory = tempfile.mkdtemp()
            self._address = os.path.join(self._directory, "rpc")
            self._listener.bind(self._address)
        else:
            self._listener.bind(("localhost", 0))
            self._address = self._listener.getsockname()
        self._listener.listen(1)
        thread = threading.Thread(target=self._run, daemon=True)
        thread.start()

    def stop(self) -> None:
        self._listener.close()
        if self._directory is not None:
            shutil.rmtree(self._directory, ignore_errors=True)

    def _run(self) -> None:
        while True:
            try:
                connection, _ = self._listener.accept()
            except OSError:
                # The listening socket has been closed, so the server is stopping
                return
            try:
                while True:
                    data = connection.recv(4096)
                    if not data:
                        break
                    connection.sendall(data)
                    if data.startswith(b"disconnect"):
                        break
            except OSError:
                pass
            finally:
                connection.close()


class ConnectionTest(unittest.TestCase):
    """What a connection to a server does regardless of what carries it: sending,
    receiving whole and in part, and reporting a connection that has gone. Only opening
    the connection differs between the transports, so each of them supplies that and
    shares these."""

    # A test case in its own right only through a transport, so it is not collected as one
    __test__ = False

    server: EchoServer

    def connect(self) -> Connection:
        raise NotImplementedError

    def server_close_connection(self, conn: Connection) -> None:
        conn.send(b"disconnect")
        self.assertEqual(b"disconnect", conn.receive(10))
        # Wait for the connection to close
        while conn._socket.recv(1) != b"":  # type: ignore[union-attr]
            pass

    def test_send_receive(self) -> None:
        conn = self.connect()
        conn.send(b"foo")
        self.assertEqual(b"foo", conn.receive(3))

    def test_long_send_receive(self) -> None:
        conn = self.connect()
        message = b"foo" * 4096
        conn.send(message)
        self.assertEqual(message, conn.receive(len(message)))

    def test_long_send_partial_receive(self) -> None:
        conn = self.connect()
        message = b"foo" * 4096
        conn.send(message)
        partial = conn.partial_receive(4096)
        self.assertEqual(message[: len(partial)], partial)
        self.assertEqual(
            message[len(partial) :], conn.receive(len(message) - len(partial))
        )

    def test_receive_on_remote_closed_connection(self) -> None:
        conn = self.connect()
        self.server_close_connection(conn)
        self.assertRaises(socket.error, conn.receive, 1)

    def test_partial_receive_on_remote_closed_connection(self) -> None:
        # Reports end of file, as receive does. Returning no data here would be
        # indistinguishable from nothing having arrived yet, and callers retry that
        # immediately and forever.
        conn = self.connect()
        self.server_close_connection(conn)
        self.assertRaises(socket.error, conn.partial_receive, 1)

    def test_receive_message_on_remote_closed_connection(self) -> None:
        # The loop reading the message size must give up rather than retry a closed
        # connection at full speed forever. Run on a thread so a regression fails here
        # instead of hanging the suite.
        conn = self.connect()
        self.server_close_connection(conn)
        raised: List[BaseException] = []

        def run() -> None:
            try:
                conn.receive_message(KRPC.Response)
            except socket.error as exn:
                raised.append(exn)

        thread = threading.Thread(target=run, daemon=True)
        thread.start()
        thread.join(10)
        self.assertEqual(1, len(raised))

    def test_send_on_closed_connection(self) -> None:
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.send, b"foo")

    def test_receive_on_closed_connection(self) -> None:
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.receive, 1)

    def test_partial_receive_on_closed_connection(self) -> None:
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.partial_receive, 1)
