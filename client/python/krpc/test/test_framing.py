import socket
import unittest
from typing import List
import krpc.schema.KRPC_pb2 as KRPC
from krpc.connection import Connection
from krpc.encoder import Encoder


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
    the socket hands over. Nothing here opens a socket, so it holds for every
    transport rather than for the one it was run over."""

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
