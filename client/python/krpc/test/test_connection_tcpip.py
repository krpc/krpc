import socket
import unittest
from krpc.connection import Connection
from krpc.test.connectiontest import ConnectionTest, EchoServer


class TestConnectionTCPIP(ConnectionTest):
    """The connection carried over TCP/IP, against a server the test listens on itself
    rather than a kRPC server."""

    __test__ = True

    @classmethod
    def setUpClass(cls) -> None:
        cls.server = EchoServer(socket.AF_INET)
        cls.server.start()

    @classmethod
    def tearDownClass(cls) -> None:
        cls.server.stop()

    def connect(self) -> Connection:
        address, port = self.server.address
        conn = Connection(address, port)
        conn.connect()
        return conn


if __name__ == "__main__":
    unittest.main()
