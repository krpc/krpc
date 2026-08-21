import socket
import unittest
from krpc.connection import Connection, LocalConnection
from krpc.test.connectiontest import ConnectionTest, EchoServer


@unittest.skipUnless(
    hasattr(socket, "AF_UNIX"), "unix domain sockets are not available here"
)
class TestConnectionLocalSocket(ConnectionTest):
    """The connection carried over a unix domain socket, against a server the test
    listens on itself rather than a kRPC server. The tests are those of the TCP/IP
    connection: what differs is only how the socket is opened."""

    __test__ = True

    @classmethod
    def setUpClass(cls) -> None:
        cls.server = EchoServer(socket.AF_UNIX)
        cls.server.start()

    @classmethod
    def tearDownClass(cls) -> None:
        cls.server.stop()

    def connect(self) -> Connection:
        conn = LocalConnection(self.server.address)
        conn.connect()
        return conn

    def test_connect_to_a_path_nothing_is_listening_on(self) -> None:
        conn = LocalConnection(self.server.address + "-does-not-exist")
        self.assertRaises(socket.error, conn.connect)


if __name__ == "__main__":
    unittest.main()
