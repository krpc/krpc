import socket

class Connection(object):
    def __init__(self, address, port):
        self._address = address
        self._port = port
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._socket.connect((self._address, self._port))

    def __del__(self):
        self._socket.close()

    def send(self, data):
        """ Send data to the connection. Blocks until all data has been sent. """
        self._socket.sendall(data)

    def receive(self, length):
        """ Receive data from the connection. Blocks until length bytes have been received. """
        data = b''
        while len(data) < length:
            remaining = length - len(data)
            result = self._socket.recv(min(4096, remaining))
            if len(result) == 0:
                raise IOError("Connection closed")
            data += result
        return data

    def partial_receive(self, length):
        """ Receive up to length bytes of data from the connection.
            Blocks until at least 1 byte has been received. """
        return self._socket.recv(length)
