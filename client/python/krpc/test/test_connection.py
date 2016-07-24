import unittest
import threading
import socket
from krpc.connection import Connection


def server_thread(started):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.bind(('', 0))
    server_thread.port = sock.getsockname()[1]
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
                    if data.startswith(b'disconnect'):
                        disconnect = True
                    connection.sendall(data)
                else:
                    break
        finally:
            connection.close()
            sock.settimeout(None)

server_thread.port = None


class TestConnection(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls._started_server = threading.Event()
        server = threading.Thread(target=server_thread, args=(cls._started_server,))
        server.daemon = True
        server.start()
        cls._started_server.wait()

    def server_close_connection(self, conn):
        conn.send(b'disconnect')
        self.assertEqual(b'disconnect', conn.receive(10))
        # Wait for the connection to close
        while conn._socket.recv(1) != b'':
            pass

    @staticmethod
    def connect():
        conn = Connection('localhost', server_thread.port)
        conn.connect()
        return conn

    def test_send_receive(self):
        conn = self.connect()
        conn.send(b'foo')
        self.assertEqual(b'foo', conn.receive(3))

    def test_long_send_receive(self):
        conn = self.connect()
        message = b'foo' * 4096
        conn.send(message)
        self.assertEqual(message, conn.receive(len(message)))

    def test_long_send_partial_receive(self):
        conn = self.connect()
        message = b'foo' * 4096
        conn.send(message)
        partial = conn.partial_receive(4096)
        self.assertEqual(message[:len(partial)], partial)
        self.assertEqual(message[len(partial):], conn.receive(len(message) - len(partial)))

    def test_receive_on_remote_closed_connection(self):
        conn = self.connect()
        self.server_close_connection(conn)
        self.assertRaises(socket.error, conn.receive, 1)

    def test_partial_receive_on_remote_closed_connection(self):
        conn = self.connect()
        self.server_close_connection(conn)
        self.assertEquals(b'', conn.partial_receive(1))

    def test_send_on_closed_connection(self):
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.send, b'foo')

    def test_receive_on_closed_connection(self):
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.receive, 1)

    def test_partial_receive_on_closed_connection(self):
        conn = self.connect()
        conn.close()
        self.assertRaises(socket.error, conn.partial_receive, 1)

if __name__ == '__main__':
    unittest.main()
