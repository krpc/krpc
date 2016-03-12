import socket
rpc_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
rpc_conn.connect(('127.0.0.1', 50000))
# Send the 12 byte hello message
rpc_conn.sendall(b'\x48\x45\x4C\x4C\x4F\x2D\x52\x50\x43\x00\x00\x00')
# Send the 32 byte client name 'Jeb' padded with zeroes
name = 'Jeb'.encode('utf-8')
name += (b'\x00' * (32-len(name)))
rpc_conn.sendall(name)
# Receive the 16 byte client identifier
identifier = b''
while len(identifier) < 16:
    identifier += rpc_conn.recv(16 - len(identifier))
# Connection successful. Print out a message along with the client identifier.
import binascii
printable_identifier = binascii.hexlify(bytearray(identifier))
print('Connected to RPC server, client idenfitier = %s' % printable_identifier)

stream_conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
stream_conn.connect(('127.0.0.1', 50001))
# Send the 12 byte hello message
stream_conn.sendall(b'\x48\x45\x4C\x4C\x4F\x2D\x53\x54\x52\x45\x41\x4D')
# Send the 16 byte client identifier
stream_conn.sendall(identifier)
# Receive the 2 byte OK message
ok_message = b''
while len(ok_message) < 2:
    ok_message += stream_conn.recv(2 - len(ok_message))
# Connection successful
print('Connected to stream server')

# import the krpc.proto schema
import krpc.schema

# Utility functions to encode and decode integers to protobuf format
import google.protobuf

def EncodeVarint(value):
    data = []
    def write(x):
        data.append(x)
    google.protobuf.internal.encoder._SignedVarintEncoder()(write, value)
    return b''.join(data)

def DecodeVarint(data):
    return google.protobuf.internal.decoder._DecodeSignedVarint(data, 0)[0]

# Create Request message
request = krpc.schema.KRPC.Request()
request.service = 'KRPC'
request.procedure = 'GetStatus'

# Encode and send the request
data = request.SerializeToString()
header = EncodeVarint(len(data))
rpc_conn.sendall(header + data)

# Receive the size of the response data
data = b''
while True:
    data += rpc_conn.recv(1)
    try:
        size = DecodeVarint(data)
        break
    except IndexError:
        pass

# Receive the response data
data = b''
while len(data) < size:
    data += rpc_conn.recv(size - len(data))

# Decode the response message
response = krpc.schema.KRPC.Response()
response.ParseFromString(data)

# Check for an error response
if response.has_error:
    print('ERROR:', response.error)

# Decode the return value as a Status message
else:
    status = krpc.schema.KRPC.Status()
    assert response.has_return_value
    status.ParseFromString(response.return_value)

    # Print out the version string from the Status message
    print(status.version)
