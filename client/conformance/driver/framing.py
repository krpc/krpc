"""Varint size prefixed messages, the framing of kRPC over a socket, and the values the
driver gives in a handshake."""

from __future__ import annotations

from typing import Protocol

# The protocol version the driver speaks
PROTOCOL_VERSION = 2

# The client identifier a scripted server gives, which any 16 bytes are
CLIENT_IDENTIFIER = b"\x01" * 16


def encode_size(size: int) -> bytes:
    """A message size, or another unsigned integer, as a varint."""
    result = bytearray()
    while size >= 0x80:
        result.append((size & 0x7F) | 0x80)
        size >>= 7
    result.append(size)
    return bytes(result)


def encode_sint32(value: int) -> bytes:
    """A sint32 as the kRPC encoder writes it: a zigzag varint."""
    return encode_size(((value << 1) ^ (value >> 31)) & 0xFFFFFFFF)


def frame(data: bytes) -> bytes:
    return encode_size(len(data)) + data


class Receiver(Protocol):
    """A socket, or a serial line read as one."""

    def recv(self, size: int, /) -> bytes: ...


def receive_frame(sock: Receiver) -> bytes | None:
    """The next message from a socket, or None once the peer closes its write side."""
    size = 0
    shift = 0
    while True:
        byte = sock.recv(1)
        if not byte:
            return None
        size |= (byte[0] & 0x7F) << shift
        shift += 7
        if byte[0] & 0x80 == 0:
            break
    data = b""
    while len(data) < size:
        chunk = sock.recv(size - len(data))
        if not chunk:
            return None
        data += chunk
    return data
