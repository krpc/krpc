from __future__ import annotations

import math
import select
import socket
import time

import google.protobuf.message

import krpc.schema.KRPC_pb2 as KRPC
from krpc import winsock
from krpc.decoder import Decoder
from krpc.encoder import Encoder, encode_request_message

# Size of a socket read. Reads are made a block at a time and buffered, so that a message
# costs one read and not one per byte of its size prefix plus one for its body.
_READ_SIZE = 8192


# The longest wait poll and select accept, in seconds. A longer one waits indefinitely.
_MAXIMUM_WAIT = (2**31 - 1) / 1000


class ReadTimeout(TimeoutError):
    """No data arrived from the server within a timeout the client set. Told apart from a
    TimeoutError the socket raises, such as a TCP retransmission timeout."""


# The longest size prefix, which holds a 32-bit size
_MAXIMUM_PREFIX_LENGTH = 5


class MalformedSize(Exception):
    """A size prefix from the server that has not ended within five bytes, or whose value
    has more than 32 bits."""


def _readable(sock: socket.socket, timeout: float | None) -> bool:
    """Whether the socket has data to read, or has closed, within the timeout. poll takes a
    socket of any number, where select takes one below FD_SETSIZE. Windows has no poll."""
    if timeout is not None and timeout > _MAXIMUM_WAIT:
        timeout = None
    if not hasattr(select, "poll"):
        try:
            return bool(select.select([sock], [], [], timeout)[0])
        except ValueError as exn:
            raise OSError("Connection closed") from exn
    poller = select.poll()
    try:
        poller.register(sock, select.POLLIN)
    except ValueError as exn:
        raise OSError("Connection closed") from exn
    return bool(poller.poll(None if timeout is None else math.ceil(timeout * 1000)))


class Connection:
    def __init__(self, address: str, port: int, timeout: float | None = None) -> None:
        self._address = address
        self._port = port
        self._timeout = timeout
        self._socket: socket.socket = None  # pyright: ignore[reportAttributeAccessIssue]
        # Data read from the socket that has not been consumed yet
        self._buffer = bytearray()
        # When a read last received a byte
        self._progress = time.monotonic()

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
        # A message is sent as soon as it is written, so waiting for more data to
        # coalesce with can only add latency
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        return sock

    def close(self) -> None:
        """Close the socket. A thread blocked reading it returns."""
        if self._socket is not None:
            try:
                self._socket.shutdown(socket.SHUT_RDWR)
            except OSError:
                pass
            self._socket.close()

    def half_close(self) -> None:
        """Close the write side of the socket, leaving the read side open."""
        self._socket.shutdown(socket.SHUT_WR)

    def __del__(self) -> None:
        self.close()

    def send_request(self, request: KRPC.Request) -> None:
        """Send a Message holding a request"""
        self._socket.sendall(encode_request_message(request))

    def send_message(self, message: google.protobuf.message.Message) -> None:
        """Send a protobuf message"""
        self.send(Encoder.encode_message_with_size(message))

    def receive_message_data(
        self, timeout: float | None = None, stall_timeout: float | None = None
    ) -> bytes:
        """Receive the encoded body of a length prefixed message. Raises ReadTimeout
        if no whole message arrives within the timeout, or if a message that has
        started to arrive receives no new byte within the stall timeout. Raises
        MalformedSize for a size prefix over five bytes or 32 bits."""

        # Read until the buffer holds the size prefix and the whole message it
        # describes, then take the message out in one go. An incomplete prefix
        # runs off the end of the buffer, which is an IndexError
        deadline = None if timeout is None else time.monotonic() + timeout
        buffer = self._buffer
        while True:
            if len(buffer) >= _MAXIMUM_PREFIX_LENGTH and all(
                x & 0x80 for x in buffer[:_MAXIMUM_PREFIX_LENGTH]
            ):
                raise MalformedSize("The size of a message has more than five bytes")
            try:
                size, prefix_length = Decoder.decode_size_prefix(buffer)
                if size > 0xFFFFFFFF:
                    raise MalformedSize("The size of a message has more than 32 bits")
                end = prefix_length + size
                if len(buffer) >= end:
                    break
            except IndexError:
                pass
            # The stall clock runs from the last byte received, which an earlier call
            # may have read
            wait = None
            if buffer and stall_timeout is not None:
                wait = self.stall_remaining(stall_timeout)
            if deadline is not None:
                remaining = max(0.0, deadline - time.monotonic())
                wait = remaining if wait is None else min(wait, remaining)
            self._fill(wait)
        data = bytes(buffer[prefix_length:end])
        del buffer[:end]
        return data

    def send(self, data: bytes) -> None:
        """Send data to the connection.
        Blocks until all data has been sent."""
        assert data
        self._socket.sendall(data)

    def _fill(self, timeout: float | None = None) -> None:
        """Read a block from the socket into the buffer. Blocks until at least one
        byte has been read, or raises ReadTimeout once the timeout passes."""
        if timeout is not None and not _readable(self._socket, timeout):
            raise ReadTimeout("Timed out waiting for data from the server")
        data = self._socket.recv(_READ_SIZE)
        if not data:
            raise OSError("Connection closed")
        self._buffer += data
        self._progress = time.monotonic()

    def has_message(self) -> bool:
        """Whether the buffer holds a whole message."""
        buffer = self._buffer
        if not buffer:
            return False
        try:
            size, prefix_length = Decoder.decode_size_prefix(buffer)
        except IndexError:
            return False
        return len(buffer) >= prefix_length + size

    def has_buffered(self) -> bool:
        """Whether the buffer holds part of a message."""
        return bool(self._buffer)

    def stall_remaining(self, stall_timeout: float) -> float:
        """Seconds left before a partial message in the buffer stalls."""
        return max(0.0, stall_timeout - (time.monotonic() - self._progress))

    def wait_readable(self, timeout: float | None = None) -> bool:
        """Wait until the socket has data to read, or the connection has closed. Returns
        False if neither happens within the timeout."""
        return _readable(self._socket, timeout)

    def discard(self, timeout: float) -> None:
        """Read and discard what arrives within the timeout. Raises OSError once the
        server has closed the connection, and ReadTimeout if nothing arrives."""
        self._buffer.clear()
        self._fill(timeout)
        self._buffer.clear()


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
