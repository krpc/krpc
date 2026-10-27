"""The server side of a websockets connection, for a scripted server. Each kRPC message is one
binary websockets message."""

from __future__ import annotations

import base64
import hashlib
import socket
import struct

# The key a server appends to the client's key to accept the upgrade, from RFC 6455
GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"

CONTINUATION = 0x0
TEXT = 0x1
BINARY = 0x2
CLOSE = 0x8
PING = 0x9
PONG = 0xA


class Upgrade:
    """The client's upgrade request."""

    def __init__(self, data: bytes) -> None:
        lines = data.decode("latin-1").split("\r\n")
        self.method, self.path, _ = lines[0].split(" ", 2)
        self.headers: dict[str, str] = {}
        for line in lines[1:]:
            if line:
                name, _, value = line.partition(":")
                self.headers[name.strip().lower()] = value.strip()


class WebSocket:
    """A websockets connection a server accepted. It reads and writes kRPC messages as
    websockets messages."""

    def __init__(self, sock: socket.socket) -> None:
        self._socket = sock
        # Whether the client sent a close frame
        self.close_received = False
        self.upgrade = self._accept()

    def _accept(self) -> Upgrade:
        data = b""
        while b"\r\n\r\n" not in data:
            chunk = self._socket.recv(4096)
            if not chunk:
                raise ConnectionError("The client closed during the upgrade")
            data += chunk
        upgrade = Upgrade(data.split(b"\r\n\r\n", 1)[0])
        key = upgrade.headers["sec-websocket-key"] + GUID
        accept = base64.b64encode(hashlib.sha1(key.encode()).digest()).decode()  # noqa: S324
        self._socket.sendall(
            (
                "HTTP/1.1 101 Switching Protocols\r\n"
                "Upgrade: websocket\r\n"
                "Connection: Upgrade\r\n"
                "Sec-WebSocket-Accept: %s\r\n\r\n" % accept
            ).encode()
        )
        return upgrade

    def fileno(self) -> int:
        return self._socket.fileno()

    def shutdown(self, how: int) -> None:
        self._socket.shutdown(how)

    def close(self) -> None:
        self._socket.close()

    def receive(self) -> bytes | None:
        """The next message, or None at a close frame or the end of the stream. A ping is
        answered with a pong."""
        payload = b""
        while True:
            frame = self._receive_frame()
            if frame is None:
                return None
            fin, opcode, data = frame
            if opcode == CLOSE:
                self.close_received = True
                return None
            if opcode == PING:
                self._send_frame(True, PONG, data)
                continue
            if opcode == PONG:
                continue
            assert opcode in (BINARY, CONTINUATION), "The client sent a text message"
            payload += data
            if fin:
                return payload

    def send(self, data: bytes, fragments: int = 1) -> None:
        """Send a binary message, split across the given number of frames."""
        size = -(-len(data) // fragments)
        for i in range(fragments):
            chunk = data[i * size : (i + 1) * size]
            self._send_frame(
                i == fragments - 1, BINARY if i == 0 else CONTINUATION, chunk
            )

    def send_together(self, messages: list[bytes]) -> None:
        """Send binary messages of one frame each, in one write."""
        self._socket.sendall(b"".join(_frame(True, BINARY, x) for x in messages))

    def send_close(self) -> None:
        self._send_frame(True, CLOSE, struct.pack(">H", 1000))

    def _send_frame(self, fin: bool, opcode: int, data: bytes) -> None:
        self._socket.sendall(_frame(fin, opcode, data))

    def _receive_frame(self) -> tuple[bool, int, bytes] | None:
        header = self._read(2)
        if header is None:
            return None
        fin = bool(header[0] & 0x80)
        opcode = header[0] & 0x0F
        size = header[1] & 0x7F
        if size == 126:
            extended = self._read(2)
            if extended is None:
                return None
            size = struct.unpack(">H", extended)[0]
        elif size == 127:
            extended = self._read(8)
            if extended is None:
                return None
            size = struct.unpack(">Q", extended)[0]
        # A client masks every frame it sends
        if not header[1] & 0x80:
            raise AssertionError("The client sent an unmasked frame")
        mask = self._read(4)
        data = self._read(size)
        if mask is None or data is None:
            return None
        return fin, opcode, bytes(x ^ mask[i % 4] for i, x in enumerate(data))

    def _read(self, size: int) -> bytes | None:
        data = b""
        while len(data) < size:
            try:
                chunk = self._socket.recv(size - len(data))
            except OSError:
                return None
            if not chunk:
                return None
            data += chunk
        return data


def _frame(fin: bool, opcode: int, data: bytes) -> bytes:
    """An unmasked frame, as a server sends it."""
    header = bytes([(0x80 if fin else 0) | opcode])
    if len(data) < 126:
        header += bytes([len(data)])
    elif len(data) < 0x10000:
        header += bytes([126]) + struct.pack(">H", len(data))
    else:
        header += bytes([127]) + struct.pack(">Q", len(data))
    return header + data
