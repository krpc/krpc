import serial
from google.protobuf.internal.encoder import _VarintEncoder
from google.protobuf.internal.decoder import _DecodeVarint
from krpc.schema import KRPC_pb2 as KRPC


def encode_varint(value):
    """Encode an int as a protobuf varint"""
    data = []
    _VarintEncoder()(data.append, value, False)
    return b"".join(data)


def decode_varint(data):
    """Decode a protobuf varint to an int"""
    return _DecodeVarint(data, 0)[0]


def send_message(port, msg):
    """Send a message, prefixed with its size, to a serial port"""
    data = msg.SerializeToString()
    size = encode_varint(len(data))
    port.write(size + data)


def recv_message(port, msg_type):
    """Receive a message, prefixed with its size, from a serial port"""
    # Receive the size of the message data
    data = b""
    while True:
        try:
            data += port.read(1)
            size = decode_varint(data)
            break
        except IndexError:
            pass
    # Receive the message data
    data = port.read(size)
    # Decode the message
    msg = msg_type()
    msg.ParseFromString(data)
    return msg


# Open a connection to the serial port
conn = serial.Serial("/dev/ttyUSB0")

# Send a connection request wrapped in a MultiplexedRequest
request = KRPC.MultiplexedRequest()
request.connection_request.type = KRPC.ConnectionRequest.RPC
request.connection_request.client_name = "Jeb"
send_message(conn, request)

# Receive the connection response
response = recv_message(conn, KRPC.ConnectionResponse)

# Check the connection was successful
if response.status != KRPC.ConnectionResponse.OK:
    raise RuntimeError("Connection failed: " + response.message)
print("Connected to server")

# Invoke the KRPC.GetStatus RPC, wrapped in a MultiplexedRequest
call = KRPC.ProcedureCall()
call.service = "KRPC"
call.procedure = "GetStatus"
request = KRPC.MultiplexedRequest()
request.request.calls.extend([call])
send_message(conn, request)

# Receive the MultiplexedResponse
multiplexed_response = recv_message(conn, KRPC.MultiplexedResponse)
response = multiplexed_response.response

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
