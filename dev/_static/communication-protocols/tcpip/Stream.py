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

# Build a KRPC.GetStatus call to be streamed
stream_call = KRPC.ProcedureCall()
stream_call.service = "KRPC"
stream_call.procedure = "GetStatus"

# Call KRPC.AddStream to add the stream, in a request with the id 1
message = KRPC.Message()
message.request.id = 1
call = message.request.calls.add()
call.service = "KRPC"
call.procedure = "AddStream"
call.arguments.add(position=0, value=stream_call.SerializeToString())
send_message(conn, message)

# Receive the response, which carries the id of the request it answers. The stream's first
# update follows it.
response = recv_message(conn).response
assert response.id == 1

# Check for an error in the response
if response.HasField("error"):
    raise RuntimeError("ERROR: " + str(response.error))

# Check for an error in the results
assert len(response.results) == 1
if response.results[0].HasField("error"):
    raise RuntimeError("ERROR: " + str(response.results[0].error))

# Decode the return value as a Stream message
stream = KRPC.Stream()
stream.ParseFromString(response.results[0].value)

# Repeatedly receive stream updates, which arrive on the same connection
while True:
    update = recv_message(conn).stream_update
    assert len(update.results) == 1
    assert stream.id == update.results[0].id
    # Decode the return value, a ServerStatus structure sent as a Tuple of its fields. Its
    # fourth field is the server statistics, another structure, whose second field is the
    # number of bytes the server has written.
    status = KRPC.Tuple.FromString(update.results[0].result.value)
    statistics = KRPC.Tuple.FromString(status.items[3])
    print(decode_varint(statistics.items[1]))
