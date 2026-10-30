"""Unix domain sockets for the driver. CPython on Windows has no socket.AF_UNIX, so there the
sockets are made through the client's winsock module."""

from __future__ import annotations

import socket
import sys

from krpc import winsock


def listen(path: str) -> socket.socket:
    """A socket listening on a unix domain socket path."""
    if sys.platform == "win32":
        return winsock.listen(path)
    sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    try:
        sock.bind(path)
        sock.listen(1)
    except OSError:
        sock.close()
        raise
    return sock


def connect(path: str) -> socket.socket:
    """A socket connected to a unix domain socket path."""
    if sys.platform == "win32":
        return winsock.connect(path)
    sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    try:
        sock.connect(path)
    except OSError:
        sock.close()
        raise
    return sock
