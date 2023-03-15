from __future__ import annotations
from typing import Optional
import socket
import select
import google.protobuf
from krpc.encoder import Encoder
from krpc.decoder import Decoder


class Connection:
    def __init__(self, address: str, port: int) -> None:
        self._address = address
        self._port = port
        self._socket: socket.socket = None  # type: ignore[assignment]

    def connect(self) -> None:
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._socket.connect((self._address, self._port))

    def close(self) -> None:
        if self._socket is not None:
            self._socket.close()

    def __del__(self) -> None:
        self.close()

    def send_message(self, message: google.protobuf.message.Message) -> None:
        """ Send a protobuf message """
        self.send(Encoder.encode_message_with_size(message))

    def receive_message(self, typ: type) -> google.protobuf.message.Message:
        """ Receive a protobuf message and decode it """

        # Read the size and position of the response message
        data = b''
        while True:
            try:
                data += self.partial_receive(1)
                size = Decoder.decode_message_size(data)
                break
            except IndexError:
                pass

        # Read and decode the response message
        data = self.receive(size)
        return Decoder.decode_message(data, typ)

    def send(self, data: bytes) -> None:
        """ Send data to the connection.
            Blocks until all data has been sent. """
        assert data
        while data:
            sent = self._socket.send(data)
            if sent == 0:
                raise socket.error("Connection closed")
            data = data[sent:]

    def receive(self, length: int) -> bytes:
        """ Receive data from the connection.
            Blocks until length bytes have been received. """
        if length == 0:
            return b''
        assert length > 0
        data = b''
        while len(data) < length:
            remaining = length - len(data)
            result = self._socket.recv(min(4096, remaining))
            if not result:
                raise socket.error("Connection closed")
            data += result
        return data

    def partial_receive(self, length: int, timeout: float = 0.01) -> bytes:
        """ Receive up to length bytes of data from the connection. """
        assert length > 0
        try:
            ready = select.select([self._socket], [], [], timeout)
        except ValueError as exn:
            raise socket.error("Connection closed") from exn
        if ready[0]:
            return self._socket.recv(length)
        return b''
