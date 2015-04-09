import socket
from error import NetworkError

class Connection(object):
    def __init__(self, address, port):
        self._address = address
        self._port = port
        self._socket = None

    def connect(self):
        try:
            socket.getaddrinfo(self._address, self._port)
        except socket.gaierror as e:
            raise NetworkError(self._address, self._port, str(e))
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            self._socket.connect((self._address, self._port))
        except socket.error as e:
            raise NetworkError(self._address, self._port, str(e))

    def close(self):
        if self._socket is not None:
            self._socket.close()

    def __del__(self):
        self.close()

    def send(self, data):
        """ Send data to the connection. Blocks until all data has been sent. """
        assert len(data) > 0
        while len(data) > 0:
            sent = self._socket.send(data)
            if sent == 0:
                raise socket.error("Connection closed")
            data = data[sent:]

    def receive(self, length):
        """ Receive data from the connection. Blocks until length bytes have been received. """
        assert length > 0
        data = b''
        while len(data) < length:
            remaining = length - len(data)
            result = self._socket.recv(min(4096, remaining))
            if len(result) == 0:
                raise socket.error("Connection closed")
            data += result
        return data

    def partial_receive(self, length):
        """ Receive up to length bytes of data from the connection.
            Blocks until at least 1 byte has been received. """
        assert length > 0
        data = self._socket.recv(length)
        if len(data) == 0:
            raise socket.error("Connection closed")
        return data
