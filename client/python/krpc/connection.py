import socket
import select
import time
from krpc.error import NetworkError


class Connection(object):
    def __init__(self, address, port):
        self._address = address
        self._port = port
        self._socket = None

    def connect(self, retries=0, timeout=0):
        try:
            socket.getaddrinfo(self._address, self._port)
        except socket.gaierror as ex:
            raise NetworkError(self._address, self._port, str(ex))
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        while True:
            try:
                self._socket.connect((self._address, self._port))
                break
            except socket.error as ex:
                if retries <= 0:
                    raise NetworkError(self._address, self._port, str(ex))
                retries -= 1
                time.sleep(timeout)

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
        if length == 0:
            return b''
        assert length > 0
        data = b''
        while len(data) < length:
            remaining = length - len(data)
            result = self._socket.recv(min(4096, remaining))
            if len(result) == 0:
                raise socket.error("Connection closed")
            data += result
        return data

    def partial_receive(self, length, timeout=0.01):
        """ Receive up to length bytes of data from the connection. """
        assert length > 0
        try:
            ready = select.select([self._socket], [], [], timeout)
        except ValueError:
            raise socket.error("Connection closed")
        if ready[0]:
            return self._socket.recv(length)
        return b''
