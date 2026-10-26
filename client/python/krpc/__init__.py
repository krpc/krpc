from __future__ import annotations

import getpass
import os
import sys
import tempfile

from krpc.client import Client
from krpc.connection import Connection, LocalConnection, MalformedSize, ReadTimeout
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.error import ConnectionError
from krpc.schema.KRPC_pb2 import (
    ConnectionRequest,
    ConnectionResponse,
    Message,
)
from krpc.session import DEFAULT_STALL_TIMEOUT, disconnect_reason_name
from krpc.types import ClassBase
from krpc.version import __version__

DEFAULT_ADDRESS = "127.0.0.1"
DEFAULT_PORT = 50000
# An empty path stands for the server's own default path, which is worked out when the
# connection is made
DEFAULT_PATH = ""
DEFAULT_HANDSHAKE_TIMEOUT = 5.0
PROTOCOL_VERSION = 2


def _default_path() -> str:
    """The default socket path, matching the one the server uses unless it was configured
    with another. Windows has no runtime directory for this, so its per-user application
    data directory stands in. The fallback is a fixed path in /tmp, as TMPDIR moves the
    temporary directory for the client and not the server."""
    windows = sys.platform == "win32"
    directory = os.environ.get("LOCALAPPDATA" if windows else "XDG_RUNTIME_DIR")
    if directory:
        return os.path.join(directory, "krpc.sock")
    temporary = tempfile.gettempdir() if windows else "/tmp"
    return os.path.join(temporary, "krpc-" + getpass.getuser() + ".sock")


def connect(
    name: str | None = None,
    address: str = DEFAULT_ADDRESS,
    port: int = DEFAULT_PORT,
    use_pregenerated_stubs: bool = True,
    timeout: float | None = None,
    handshake_timeout: float | None = DEFAULT_HANDSHAKE_TIMEOUT,
    stall_timeout: float | None = DEFAULT_STALL_TIMEOUT,
) -> Client:
    """
    Connect to a kRPC server on the specified IP address and port number.
    Optionally give the kRPC server the supplied name to identify the client.
    If timeout is given, gives up after that many seconds of waiting for a
    connection. Gives up after handshake_timeout seconds of waiting for the server
    to accept the connection, or waits indefinitely if it is None, such as for a
    server that asks the player to approve each connection. Fails the connection when a message from the server
    receives no new byte for stall_timeout seconds once it has started to arrive,
    or never if it is None.
    """

    _check_timeouts(handshake_timeout, stall_timeout)
    connection = Connection(address, port, timeout)
    return _connect(
        name, connection, use_pregenerated_stubs, handshake_timeout, stall_timeout
    )


def connect_local(
    name: str | None = None,
    path: str = DEFAULT_PATH,
    use_pregenerated_stubs: bool = True,
    handshake_timeout: float | None = DEFAULT_HANDSHAKE_TIMEOUT,
    stall_timeout: float | None = DEFAULT_STALL_TIMEOUT,
) -> Client:
    """
    Connect to a kRPC server on the same machine, over a unix domain socket named by
    the given path; an empty path stands for the one the server uses by default.
    Optionally give the kRPC server the supplied name to identify the client. Gives
    up after handshake_timeout seconds of waiting for the server to accept the
    connection, or waits indefinitely if it is None. See connect for the stall
    timeout.
    """

    _check_timeouts(handshake_timeout, stall_timeout)
    connection = LocalConnection(path or _default_path())
    return _connect(
        name, connection, use_pregenerated_stubs, handshake_timeout, stall_timeout
    )


def get_object_id(obj: ClassBase) -> int:
    """
    The identifier of a remote object on the server, e.g. to pass to
    Expression.constant_object. Raises ValueError if the object is None.
    """
    if obj is None:
        raise ValueError("The object is None")
    return obj._object_id


def _check_timeouts(
    handshake_timeout: float | None, stall_timeout: float | None
) -> None:
    """Raise ValueError for a timeout that is not positive, including NaN."""
    for name, timeout in (
        ("handshake_timeout", handshake_timeout),
        ("stall_timeout", stall_timeout),
    ):
        if timeout is not None and not timeout > 0:
            raise ValueError("%s must be positive, or None, not %r" % (name, timeout))


def _connect(
    name: str | None,
    connection: Connection,
    use_pregenerated_stubs: bool,
    handshake_timeout: float | None,
    stall_timeout: float | None,
) -> Client:
    """Perform the connection handshake over an already built connection. The
    handshake is the same whatever carries it."""
    try:
        connection.connect()
    except OSError as exn:
        raise ConnectionError("Failed to connect to the server: %s" % exn) from exn
    try:
        response = _handshake(name, connection, handshake_timeout)
    except BaseException:
        connection.close()
        raise
    if response.status != ConnectionResponse.OK:
        connection.close()
        try:
            status = ConnectionResponse.Status.Name(response.status)
        except ValueError:
            status = str(response.status)
        message = "The server refused the connection (%s)" % status
        if response.message:
            message += ": " + response.message
        raise ConnectionError(message, status=status)
    if response.protocol_version != PROTOCOL_VERSION:
        connection.close()
        raise ConnectionError(
            f"The server does not support protocol version {PROTOCOL_VERSION}. Use a "
            "client from the same kRPC release as the server."
        )
    try:
        return Client(connection, use_pregenerated_stubs, stall_timeout)
    except BaseException:
        connection.close()
        raise


def _handshake(
    name: str | None,
    connection: Connection,
    timeout: float | None,
) -> ConnectionResponse:
    """Send the connection request, and read the server's response to it."""
    request = ConnectionRequest()
    request.protocol_version = PROTOCOL_VERSION
    if name is not None:
        request.client_name = name
    connection.send_message(Message(connection_request=request))
    # The handshake timeout covers the whole response, and the stall timeout applies
    # from the next message
    try:
        data = connection.receive_message_data(timeout)
    except ReadTimeout as exn:
        raise ConnectionError(
            "The server did not answer the connection request within %g seconds"
            % timeout
        ) from exn
    except (OSError, MalformedSize) as exn:
        raise ConnectionError("Connection failed: %s" % exn) from exn
    try:
        message = Message.FromString(data)
    except Exception as exn:
        raise ConnectionError(
            "Failed to parse the server's response to the connection request"
        ) from exn
    case = message.WhichOneof("content")
    if case == "connection_response":
        return message.connection_response
    if case == "disconnect":
        reason = disconnect_reason_name(message.disconnect.reason)
        text = "The server ended the connection (%s)" % reason
        if message.disconnect.message:
            text += ": " + message.disconnect.message
        raise ConnectionError(text, reason=reason)
    raise ConnectionError(
        "Expected a response to the connection request, got %s"
        % (case or "a message of an unknown kind")
    )
