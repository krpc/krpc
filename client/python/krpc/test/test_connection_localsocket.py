import getpass
import os
import socket
import sys
import unittest
from unittest import mock
from krpc import _default_path
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


@unittest.skipIf(
    sys.platform == "win32", "the fallback is to a directory only POSIX has"
)
class TestDefaultPath(unittest.TestCase):
    """The path the client looks for a server on when it is given none. The server works
    the same path out in a process of its own, so the two only meet if the rule they each
    follow is the same one."""

    def test_runtime_directory_is_used_when_there_is_one(self) -> None:
        with mock.patch.dict(os.environ, {"XDG_RUNTIME_DIR": "/run/user/1000"}):
            self.assertEqual("/run/user/1000/krpc/rpc", _default_path("rpc"))

    def test_fixed_directory_is_fallen_back_to(self) -> None:
        # TMPDIR is set per process, and the server's is not the client's to read
        environment = {"TMPDIR": "/somewhere/else"}
        with mock.patch.dict(os.environ, environment, clear=True):
            self.assertEqual(
                "/tmp/krpc-" + getpass.getuser() + "/stream", _default_path("stream")
            )


if __name__ == "__main__":
    unittest.main()
