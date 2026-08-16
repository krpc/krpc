import unittest
import threading
import socket
from typing import List
import krpc.schema.KRPC_pb2 as KRPC
from krpc.connection import Connection
from krpc.encoder import Encoder


class ServerThread:
    def __init__(self) -> None:
        self.port = 0

    def __call__(self, started: threading.Event) -> None:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.bind(("", 0))
        self.port = sock.getsockname()[1]
        sock.listen(1)
        started.set()

        while True:
            # Wait for a connection
            connection, _ = sock.accept()

            # Client connected
            disconnect = False
            sock.settimeout(0.1)
            try:
                # Receive then resend data back to client
                while not disconnect:
                    data = connection.recv(16)
                    if data:
                        if data.startswith(b"disconnect"):
                            disconnect = True
                        connection.sendall(data)
                    else:
                        break
            finally:
                connection.close()
                sock.settimeout(None)


class TestConnection(unittest.TestCase):
    _started_server = threading.Event()
    _server_thread = ServerThread()

    @classmethod
    def setUpClass(cls) -> None:
        server = threading.Thread(
            target=cls._server_thread, args=(cls._started_server,)
        )
        server.daemon = True
        server.start()
        cls._started_server.wait()

    def server_close_connection(self, conn: Connection) -> None:
        conn.send(b"disconnect")
        self.assertEqual(b"disconnect", conn.receive(10))
        # Wait for the connection to close
        while conn._socket.recv(1) != b"":
            pass

    @classmethod
    def connect(cls) -> Connection:
        conn = Connection("localhost", cls._server_thread.port)
        conn.connect()
        return conn

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
        raised = []

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


class ScriptedSocket:
    """A socket that hands over a fixed script of reads, so that a message can be
    made to arrive in whatever pieces a test needs. An exhausted script reads as
    end of file, as a closed connection does."""

    def __init__(self, chunks: List[bytes]) -> None:
        self.chunks = list(chunks)

    def recv(self, length: int) -> bytes:  # pylint: disable=unused-argument
        if not self.chunks:
            return b""
        return self.chunks.pop(0)

    def close(self) -> None:
        pass


class TestReceiveMessage(unittest.TestCase):
    """Reads are made a block at a time, and a block has nothing to do with where a
    message starts or ends, so receive_message has to reassemble one out of whatever
    the socket hands over."""

    @staticmethod
    def encoded(value: bytes) -> bytes:
        """A response carrying the given value, encoded with its size prefix"""
        response = KRPC.Response()
        response.results.add(value=value)
        return Encoder.encode_message_with_size(response)

    @staticmethod
    def connection(*chunks: bytes) -> Connection:
        conn = Connection("localhost", 0)
        conn._socket = ScriptedSocket(list(chunks))  # type: ignore[assignment]
        return conn

    def receive(self, conn: Connection) -> bytes:
        message = conn.receive_message(KRPC.Response)
        return message.results[0].value  # type: ignore[attr-defined,no-any-return]

    def test_whole_message_in_one_read(self) -> None:
        conn = self.connection(self.encoded(b"foo"))
        self.assertEqual(b"foo", self.receive(conn))
        self.assertEqual(b"", conn._buffer)

    def test_message_split_across_reads(self) -> None:
        data = self.encoded(b"foo")
        conn = self.connection(data[:1], data[1:3], data[3:])
        self.assertEqual(b"foo", self.receive(conn))
        self.assertEqual(b"", conn._buffer)

    def test_size_prefix_split_across_reads(self) -> None:
        # A payload this size needs a two byte size prefix, so the read can end
        # part way through the prefix itself
        value = b"x" * 300
        data = self.encoded(value)
        conn = self.connection(data[:1], data[1:])
        self.assertEqual(value, self.receive(conn))
        self.assertEqual(b"", conn._buffer)

    def test_two_messages_in_one_read(self) -> None:
        conn = self.connection(self.encoded(b"foo") + self.encoded(b"bar"))
        self.assertEqual(b"foo", self.receive(conn))
        self.assertEqual(b"bar", self.receive(conn))
        self.assertEqual(b"", conn._buffer)

    def test_message_and_the_start_of_the_next_in_one_read(self) -> None:
        second = self.encoded(b"bar")
        conn = self.connection(self.encoded(b"foo") + second[:2], second[2:])
        self.assertEqual(b"foo", self.receive(conn))
        self.assertEqual(b"bar", self.receive(conn))
        self.assertEqual(b"", conn._buffer)

    def test_connection_closed_partway_through_a_message(self) -> None:
        data = self.encoded(b"foo")
        conn = self.connection(data[:2])
        self.assertRaises(socket.error, conn.receive_message, KRPC.Response)


if __name__ == "__main__":
    unittest.main()
