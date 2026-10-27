import socket

from google.protobuf.internal.decoder import _DecodeVarint
from google.protobuf.internal.encoder import _VarintEncoder

from krpc.schema import KRPC_pb2 as KRPC


def encode_varint(value):
    """Encode an int as a protobuf varint"""
    data = []
    _VarintEncoder()(data.append, value, False)
    return b"".join(data)


def decode_varint(data):
    """Decode a protobuf varint to an int"""
    return _DecodeVarint(data, 0)[0]


def send_message(conn, msg):
    """Send a message, prefixed with its size, to a TCP/IP socket"""
    data = msg.SerializeToString()
    size = encode_varint(len(data))
    conn.sendall(size + data)


def recv_message(conn):
    """Receive a Message, prefixed with its size, from a TCP/IP socket"""
    # Receive the size of the message data
    data = b""
    while True:
        try:
            data += conn.recv(1)
            size = decode_varint(data)
            break
        except IndexError:
            pass
    # Receive the message data
    data = b""
    while len(data) < size:
        data += conn.recv(size - len(data))
    # Decode the message
    msg = KRPC.Message()
    msg.ParseFromString(data)
    return msg


# Open a TCP/IP socket to the server
conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
conn.connect(("127.0.0.1", 50000))

# Send a connection request
message = KRPC.Message()
message.connection_request.protocol_version = 2
message.connection_request.client_name = "Example"
send_message(conn, message)

# Receive the connection response
response = recv_message(conn).connection_response

# Check the connection was successful
if response.status != KRPC.ConnectionResponse.OK:
    raise RuntimeError("Connection failed: " + response.message)
if response.protocol_version != 2:
    raise RuntimeError("The server does not support protocol version 2")
print("Connected to the server")

# Invoke the KRPC.GetStatus RPC, in a request with the id 1
message = KRPC.Message()
message.request.id = 1
call = message.request.calls.add()
call.service = "KRPC"
call.procedure = "GetStatus"
send_message(conn, message)

# Receive the response, which carries the id of the request it answers
response = recv_message(conn).response
assert response.id == 1

# Check for an error in the response
if response.HasField("error"):
    raise RuntimeError("ERROR: " + str(response.error))

# Check for an error in the results
assert len(response.results) == 1
if response.results[0].HasField("error"):
    raise RuntimeError("ERROR: " + str(response.results[0].error))

# Decode the return value as a Status message
status = KRPC.Status()
status.ParseFromString(response.results[0].value)

# Print out the Status message
print(status)

# End the connection with a disconnect, then close the write side
disconnect = KRPC.Disconnect(reason=KRPC.Disconnect.CLOSED)
send_message(conn, KRPC.Message(disconnect=disconnect))
conn.shutdown(socket.SHUT_WR)

# Discard anything still arriving until the server closes its side, or a second passes with nothing
conn.settimeout(1)
try:
    while conn.recv(4096):
        pass
except TimeoutError:
    pass
conn.close()
