from __future__ import annotations
from typing import Optional
import socket
import select
import google.protobuf
from krpc import winsock
from krpc.encoder import Encoder
from krpc.decoder import Decoder

# Size of a socket read. Reads are made a block at a time and buffered, so that a message
# costs one read and not one per byte of its size prefix plus one for its body.
_READ_SIZE = 8192


class Connection:
    def __init__(
        self, address: str, port: int, timeout: Optional[float] = None
    ) -> None:
        self._address = address
        self._port = port
        self._timeout = timeout
        self._socket: socket.socket = None  # type: ignore[assignment]
        # Data read from the socket that has not been consumed yet
        self._buffer = bytearray()

    def connect(self) -> None:
        self._socket = self._open()

    def _open(self) -> socket.socket:
        """Open a socket to the server. Everything above this is transport agnostic,
        so a different transport only has to replace this."""
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # A network that drops a connection attempt instead of refusing it leaves the
        # client waiting, so bound the wait where one was asked for.
        sock.settimeout(self._timeout)
        sock.connect((self._address, self._port))
        # Whatever bound was put on connecting, sending and receiving block
        sock.settimeout(None)
        # The protocol is strictly request/response, so waiting for more data to
        # coalesce with can only add latency
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        return sock

    def close(self) -> None:
        if self._socket is not None:
            self._socket.close()

    def __del__(self) -> None:
        self.close()

    def send_message(self, message: google.protobuf.message.Message) -> None:
        """Send a protobuf message"""
        self.send(Encoder.encode_message_with_size(message))

    def receive_message(self, typ: type) -> google.protobuf.message.Message:
        """Receive a protobuf message and decode it"""

        # Read until the buffer holds the size prefix and the whole message it
        # describes, then take the message out in one go. An incomplete prefix
        # runs off the end of the buffer, which is an IndexError
        buffer = self._buffer
        while True:
            try:
                size, prefix_length = Decoder.decode_size_prefix(buffer)
            except IndexError:
                self._fill()
                continue
            end = prefix_length + size
            if len(buffer) >= end:
                break
            self._fill()
        data = bytes(buffer[prefix_length:end])
        del buffer[:end]
        return Decoder.decode_message(data, typ)

    def send(self, data: bytes) -> None:
        """Send data to the connection.
        Blocks until all data has been sent."""
        assert data
        self._socket.sendall(data)

    def _fill(self) -> None:
        """Read a block from the socket into the buffer.
        Blocks until at least one byte has been read."""
        data = self._socket.recv(_READ_SIZE)
        if not data:
            raise socket.error("Connection closed")
        self._buffer += data

    def receive(self, length: int) -> bytes:
        """Receive data from the connection.
        Blocks until length bytes have been received."""
        if length == 0:
            return b""
        assert length > 0
        buffer = self._buffer
        while len(buffer) < length:
            self._fill()
        data = bytes(buffer[:length])
        del buffer[:length]
        return data

    def partial_receive(self, length: int, timeout: float = 0.01) -> bytes:
        """Receive up to length bytes of data from the connection.
        Returns no data if none arrived within the timeout."""
        assert length > 0
        buffer = self._buffer
        if buffer:
            length = min(length, len(buffer))
            data = bytes(buffer[:length])
            del buffer[:length]
            return data
        try:
            ready = select.select([self._socket], [], [], timeout)
        except ValueError as exn:
            raise socket.error("Connection closed") from exn
        if ready[0]:
            data = self._socket.recv(length)
            # A readable socket that yields nothing has reached end of file. Reporting it
            # as no data would be indistinguishable from nothing having arrived yet, and
            # callers retry that immediately and forever.
            if not data:
                raise socket.error("Connection closed")
            return data
        return b""


class LocalConnection(Connection):
    """A connection to a server on the same machine, over a unix domain socket."""

    def __init__(self, path: str) -> None:
        super().__init__(path, 0)

    def _open(self) -> socket.socket:
        # Windows has the address family but the socket module does not expose it, so there
        # the socket is opened by calling winsock directly.
        if not hasattr(socket, "AF_UNIX"):
            return winsock.connect(self._address)
        sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        sock.connect(self._address)
        return sock
