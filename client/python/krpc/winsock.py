"""Opening a unix domain socket on Windows.

Windows has carried the unix domain address family since Windows 10 1803, but CPython does not
expose it: there is no ``socket.AF_UNIX`` there, so a socket cannot be given the address of one
through the socket module. Winsock itself is willing, so the socket is opened by calling it
directly and the resulting handle is then handed to a socket object, which carries it from there.
"""

from __future__ import annotations
import ctypes
import socket
import sys

# The winsock address family, and the size of the path in the address structure. Both are as
# they are on every other platform: Windows kept the layout so that the same code works.
AF_UNIX = 1
UNIX_PATH_MAX = 108

# The value winsock returns from a call that failed.
SOCKET_ERROR = -1


class SockaddrUn(ctypes.Structure):
    """The address of a unix domain socket: the address family, then the path."""

    _fields_ = [
        ("sun_family", ctypes.c_ushort),
        ("sun_path", ctypes.c_char * UNIX_PATH_MAX),
    ]


def _winsock() -> ctypes.CDLL:
    """The winsock library, with the calls this module makes declared.

    Winsock is already started: importing the socket module is what does it, and it stays
    started for the life of the process.
    """
    library = ctypes.WinDLL("ws2_32", use_last_error=True)  # type: ignore[attr-defined]
    library.socket.argtypes = [ctypes.c_int, ctypes.c_int, ctypes.c_int]
    library.socket.restype = ctypes.c_void_p
    library.connect.argtypes = [
        ctypes.c_void_p,
        ctypes.POINTER(SockaddrUn),
        ctypes.c_int,
    ]
    library.connect.restype = ctypes.c_int
    library.closesocket.argtypes = [ctypes.c_void_p]
    library.closesocket.restype = ctypes.c_int
    return library


def _error(message: str) -> OSError:
    """The last winsock error, as the error type the other platforms raise."""
    code = ctypes.get_last_error()
    return OSError(0, "%s: %s" % (message, ctypes.WinError(code).strerror), None, code)


def connect(path: str) -> socket.socket:
    """Connect to the unix domain socket at the given path.

    Returns a socket object holding the connected socket. The family it is given is not the one
    the socket has, which the socket module documents as affecting only how python represents
    the socket, such as what getpeername returns, "but not the actual OS resource". Sending and
    receiving therefore go to the socket that was opened here, and nothing above this reads the
    address a connection was made to.
    """
    if sys.platform != "win32":
        raise RuntimeError("this is only for Windows, which has no socket.AF_UNIX")

    encoded = path.encode("utf-8")
    # The path is copied into a field of a fixed size, so one that does not fit has to be
    # reported rather than truncated to name a socket the caller did not ask for.
    if len(encoded) >= UNIX_PATH_MAX:
        raise ValueError(
            "socket path is %d bytes, which is longer than the %d a socket path may be"
            % (len(encoded), UNIX_PATH_MAX - 1)
        )

    library = _winsock()
    # A socket handle is a pointer-sized value, so it is held as one rather than as an int,
    # which would truncate it on a 64-bit build. Winsock reports a failure to make one as
    # every bit set.
    invalid = ctypes.c_void_p(-1).value
    handle = library.socket(AF_UNIX, socket.SOCK_STREAM, 0)
    if handle is None or handle == invalid:
        raise _error("failed to create socket")

    address = SockaddrUn(sun_family=AF_UNIX, sun_path=encoded)
    connected = library.connect(handle, ctypes.byref(address), ctypes.sizeof(address))
    if connected == SOCKET_ERROR:
        error = _error("failed to connect to %s" % path)
        library.closesocket(handle)
        raise error

    try:
        # The socket object takes the handle over, so closing it closes the socket, and the
        # handle is not closed here as well.
        return socket.socket(
            socket.AF_INET,
            socket.SOCK_STREAM,
            0,
            fileno=handle,  # type: ignore[arg-type]
        )
    except OSError:
        library.closesocket(handle)
        raise
