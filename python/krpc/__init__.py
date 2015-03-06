import socket
from krpc.connection import Connection
from krpc.client import Client
from krpc.encoder import _Encoder
from krpc.decoder import _Decoder

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_RPC_PORT = 50000
DEFAULT_STREAM_PORT = 50001

def connect(address=DEFAULT_ADDRESS, rpc_port=DEFAULT_RPC_PORT, stream_port=DEFAULT_STREAM_PORT, name=None):
    """
    Connect to a kRPC server on the specified IP address and port numbers,
    and optionally give the kRPC server the supplied name to identify the client
    (up to 32 bytes of UTF-8 encoded text)
    """
    assert rpc_port != stream_port

    # Connect to RPC server
    rpc_connection = Connection(address, rpc_port)
    rpc_connection.send(_Encoder.RPC_HELLO_MESSAGE)
    rpc_connection.send(_Encoder.client_name(name))
    client_identifier = rpc_connection.receive(_Decoder.GUID_LENGTH)

    # Connect to Stream server
    stream_connection = Connection(address, stream_port)
    stream_connection.send(_Encoder.STREAM_HELLO_MESSAGE)
    stream_connection.send(client_identifier)
    ok_message = stream_connection.receive(_Decoder.OK_LENGTH)
    assert ok_message == _Decoder.OK_MESSAGE

    return Client(rpc_connection, stream_connection)
