"""Unix domain sockets for the driver. Windows has the address family, but CPython does not
expose it there, so on Windows the sockets are made by calling winsock directly."""

from __future__ import annotations

import ctypes
import socket
import sys

from krpc import winsock


def listen(path: str) -> socket.socket:
    """A socket listening on a unix domain socket path."""
    if hasattr(socket, "AF_UNIX"):
        sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        try:
            sock.bind(path)
            sock.listen(1)
        except OSError:
            sock.close()
            raise
        return sock
    library = _winsock()
    handle = library.socket(winsock.AF_UNIX, socket.SOCK_STREAM, 0)
    if handle is None or handle == ctypes.c_void_p(-1).value:
        raise _error("failed to create socket")
    address = winsock.SockaddrUn(
        sun_family=winsock.AF_UNIX, sun_path=path.encode("utf-8")
    )
    if (
        library.bind(handle, ctypes.byref(address), ctypes.sizeof(address))
        == winsock.SOCKET_ERROR
        or library.listen(handle, 1) == winsock.SOCKET_ERROR
    ):
        error = _error("failed to listen on %s" % path)
        library.closesocket(handle)
        raise error
    # The family only sets how Python shows addresses, as in winsock.connect
    return socket.socket(socket.AF_INET, socket.SOCK_STREAM, 0, fileno=handle)


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


def _winsock() -> ctypes.WinDLL:
    library = ctypes.WinDLL("ws2_32", use_last_error=True)
    library.socket.argtypes = [ctypes.c_int, ctypes.c_int, ctypes.c_int]
    library.socket.restype = ctypes.c_void_p
    library.bind.argtypes = [
        ctypes.c_void_p,
        ctypes.POINTER(winsock.SockaddrUn),
        ctypes.c_int,
    ]
    library.bind.restype = ctypes.c_int
    library.listen.argtypes = [ctypes.c_void_p, ctypes.c_int]
    library.listen.restype = ctypes.c_int
    library.closesocket.argtypes = [ctypes.c_void_p]
    library.closesocket.restype = ctypes.c_int
    return library


def _error(message: str) -> OSError:
    code = ctypes.get_last_error()
    return OSError(0, "%s: %s" % (message, ctypes.WinError(code).strerror), None, code)
