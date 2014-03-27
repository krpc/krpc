import socket
from krpc.client import Client
from krpc.encoder import _Encoder

DEFAULT_ADDRESS = '127.0.0.1'
DEFAULT_PORT = 50000

def connect(address=DEFAULT_ADDRESS, port=DEFAULT_PORT, name=None):
    """
    Connect to a kRPC server on the specified IP address and port number,
    and optionally give the kRPC server the supplied name to identify the client
    (up to 32 bytes of UTF-8 encoded text)
    """
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect((address, port))
    connection.send(_Encoder.hello_message(name))
    return Client(connection)
