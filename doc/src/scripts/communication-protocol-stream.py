import binascii
import socket
from google.protobuf.internal.encoder import _VarintEncoder
from google.protobuf.internal.decoder import _DecodeVarint
from krpc.schema import KRPC_pb2 as KRPC


def encode_varint(value):
    """ Encode an int as a protobuf varint """
    data = []
    _VarintEncoder()(data.append, value, False)
    return b''.join(data)


def decode_varint(data):
    """ Decode a protobuf varint to an int """
    return _DecodeVarint(data, 0)[0]


def send_message(conn, msg):
    """ Send a message, prefixed with its size, to a TPC/IP socket """
    data = msg.SerializeToString()
    size = encode_varint(len(data))
    conn.sendall(size + data)


def recv_message(conn, msg_type):
    """ Receive a message, prefixed with its size, from a TCP/IP socket """
    # Receive the size of the message data
    data = b''
    while True:
        try:
            data += conn.recv(1)
            size = decode_varint(data)
            break
        except IndexError:
            pass
    # Receive the message data
    data = conn.recv(size)
    # Decode the message
    msg = msg_type()
    msg.ParseFromString(data)
    return msg


# Open a TCP/IP socket to the RPC server
rpc_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
rpc_conn.connect(('127.0.0.1', 50000))

# Send an RPC connection request
request = KRPC.ConnectionRequest()
request.type = KRPC.ConnectionRequest.RPC
request.client_name = 'Jeb'
send_message(rpc_conn, request)

# Receive the connection response
response = recv_message(rpc_conn, KRPC.ConnectionResponse)

# Check the connection was successful
if response.status != KRPC.ConnectionResponse.OK:
    raise RuntimeError('Connection failed: ' + response.message)
print('Connected to RPC server')

# Print out the clients identifier
print('RPC client idenfitier = %s' % binascii.hexlify(
    bytearray(response.client_identifier)).decode('utf8'))

# Open a TCP/IP socket to the Stream server
stream_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
stream_conn.connect(('127.0.0.1', 50001))

# Send a stream connection request, containing the clients identifier
request = KRPC.ConnectionRequest()
request.type = KRPC.ConnectionRequest.STREAM
request.client_identifier = response.client_identifier
send_message(stream_conn, request)

# Receive the connection response
response = recv_message(stream_conn, KRPC.ConnectionResponse)

# Check the connection was successful
if response.status != KRPC.ConnectionResponse.OK:
    raise RuntimeError("Connection failed: " + response.message)
print('Connected to stream server')

# Build a KRPC.GetStatus call to be streamed
stream_call = KRPC.ProcedureCall()
stream_call.service = 'KRPC'
stream_call.procedure = 'GetStatus'

# Call KRPC.AddStream to add the stream
call = KRPC.ProcedureCall()
call.service = 'KRPC'
call.procedure = 'AddStream'
arg = KRPC.Argument()
arg.position = 0
arg.value = stream_call.SerializeToString()
call.arguments.extend([arg])
request = KRPC.Request()
request.calls.extend([call])
send_message(rpc_conn, request)

# Receive the response
response = recv_message(rpc_conn, KRPC.Response)

# Check for an error in the response
if response.HasField('error'):
    raise RuntimeError('ERROR: ' + str(response.error))

# Check for an error in the results
assert(len(response.results) == 1)
if response.results[0].HasField('error'):
    raise RuntimeError('ERROR: ' + str(response.results[0].error))

# Decode the return value as a Stream message
stream = KRPC.Stream()
stream.ParseFromString(response.results[0].value)

# Repeatedly receive stream updates from the stream server
while True:
    update = recv_message(stream_conn, KRPC.StreamUpdate)
    assert(len(update.results) == 1)
    assert(stream.id == update.results[0].id)
    # Decode and print the return value
    status = KRPC.Status()
    status.ParseFromString(update.results[0].result.value)
    print(status)
