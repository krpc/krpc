from __future__ import annotations
import getpass
import os
import sys
import tempfile
from typing import cast, Optional
from krpc.connection import Connection, LocalConnection
from krpc.client import Client
from krpc.encoder import Encoder
from krpc.error import ConnectionError  # pylint: disable=redefined-builtin
from krpc.decoder import Decoder
from krpc.schema.KRPC_pb2 import ConnectionRequest, ConnectionResponse

from krpc.version import __version__

DEFAULT_ADDRESS = "127.0.0.1"
DEFAULT_RPC_PORT = 50000
DEFAULT_STREAM_PORT = 50001
# An empty path stands for the path the server uses unless it was configured with
# another, which is worked out when the connection is made rather than on import
DEFAULT_RPC_PATH = ""
DEFAULT_STREAM_PATH = ""


def _default_path(name: str) -> str:
    """A default path for a socket of the given name, matching the one the server uses
    unless it was configured with another. Windows has no runtime directory for this, so
    its per-user application data directory stands in."""
    variable = "LOCALAPPDATA" if sys.platform == "win32" else "XDG_RUNTIME_DIR"
    directory = os.environ.get(variable)
    if directory:
        return os.path.join(directory, "krpc", name)
    return os.path.join(tempfile.gettempdir(), "krpc-" + getpass.getuser(), name)


def connect(
    name: Optional[str] = None,
    address: str = DEFAULT_ADDRESS,
    rpc_port: int = DEFAULT_RPC_PORT,
    stream_port: int = DEFAULT_STREAM_PORT,
    use_pregenerated_stubs: bool = True,
    timeout: Optional[float] = None,
) -> Client:
    """
    Connect to a kRPC server on the specified IP address and port numbers.
    If stream_port is None, does not connect to the stream server.
    Optionally give the kRPC server the supplied name to identify the client.
    If timeout is given, gives up after that many seconds of waiting for a
    connection, rather than waiting indefinitely.
    """

    rpc_connection = Connection(address, rpc_port, timeout)
    stream_connection = (
        Connection(address, stream_port, timeout) if stream_port is not None else None
    )
    return _connect(name, rpc_connection, stream_connection, use_pregenerated_stubs)


def connect_local(
    name: Optional[str] = None,
    rpc_path: str = DEFAULT_RPC_PATH,
    stream_path: Optional[str] = DEFAULT_STREAM_PATH,
    use_pregenerated_stubs: bool = True,
) -> Client:
    """
    Connect to a kRPC server on the same machine, over unix domain sockets named by
    the given paths; an empty path stands for the one the server uses by default. If
    stream_path is None, does not connect to the stream server. Optionally give the
    kRPC server the supplied name to identify the client.
    """

    rpc_connection = LocalConnection(rpc_path or _default_path("rpc"))
    stream_connection = (
        LocalConnection(stream_path or _default_path("stream"))
        if stream_path is not None
        else None
    )
    return _connect(name, rpc_connection, stream_connection, use_pregenerated_stubs)


def _connect(
    name: Optional[str],
    rpc_connection: Connection,
    stream_connection: Optional[Connection],
    use_pregenerated_stubs: bool,
) -> Client:
    """Perform the connection handshake over already built connections. The handshake
    is the same whatever carries it."""

    # Connect to RPC server
    rpc_connection.connect()
    request = ConnectionRequest()
    request.type = ConnectionRequest.RPC
    if name is not None:
        request.client_name = name
    rpc_connection.send_message(request)
    response = cast(
        ConnectionResponse, rpc_connection.receive_message(ConnectionResponse)
    )
    if response.status != ConnectionResponse.OK:
        raise ConnectionError(response.message)
    client_identifier = response.client_identifier

    # Connect to Stream server
    if stream_connection is not None:
        stream_connection.connect()
        request = ConnectionRequest()
        request.type = ConnectionRequest.STREAM
        request.client_identifier = client_identifier
        stream_connection.send_message(request)
        response = cast(
            ConnectionResponse, stream_connection.receive_message(ConnectionResponse)
        )
        if response.status != ConnectionResponse.OK:
            raise ConnectionError(response.message)

    return Client(rpc_connection, stream_connection, use_pregenerated_stubs)
