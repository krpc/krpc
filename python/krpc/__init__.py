import socket
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
    # TODO: add checks that the connection is established correctly
    rpc_connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    rpc_connection.connect((address, port))
    rpc_connection.send(_Encoder.hello_message)
    rpc_connection.send(_Encoder.client_name(name))
    client_id = rpc_connection.recv(_Decoder.GUID_LENGTH)
    stream_connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    stream_connection.connect((address, port+1))
    stream_connection.send(_Encoder.hello_message)
    stream_connection.send(client_id)
    ok_message = stream_connection.recv(2)
    assert ok_message == b'\x4F\x4B'
    return Client(rpc_connection, stream_connection)
