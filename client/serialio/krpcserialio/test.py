import os
import unittest
import krpc.schema.KRPC_pb2 as KRPC
import google.protobuf
from google.protobuf.internal.decoder import _DecodeVarint, _DecodeSignedVarint
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal.wire_format import ZigZagEncode, ZigZagDecode
import serial


# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
# pylint: disable=invalid-name,protected-access
_pb_VarintEncoder = protobuf_encoder._VarintEncoder()
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()
_pb_version = google.protobuf.__version__.split('.')
if int(_pb_version[0]) >= 3 and int(_pb_version[1]) >= 4:
    # protobuf v3.4.0 and above
    def _VarintEncoder(write, value):
        return _pb_VarintEncoder(write, value, True)

    def _SignedVarintEncoder(write, value):
        return _pb_SignedVarintEncoder(write, value, True)
else:
    # protobuf v3.3.0 and below
    _VarintEncoder = _pb_VarintEncoder
    _SignedVarintEncoder = _pb_SignedVarintEncoder
# pylint: enable=invalid-name,protected-access


def encode_varint(value):
    """ Encode an int as a protobuf varint """
    data = []
    _VarintEncoder(data.append, value)
    return b''.join(data)


def decode_varint(data):
    """ Decode a protobuf varint to an int """
    return _DecodeVarint(data, 0)[0]


class SerialIOTest(unittest.TestCase):
    @staticmethod
    def port_name():
        return os.getenv('PORT', None)

    @classmethod
    def connect(cls, port_name):
        cls.rpc_conn = serial.Serial(port_name)
        request = KRPC.MultiplexedRequest()
        request.connection_request.type = KRPC.ConnectionRequest.RPC
        request.connection_request.client_name = 'krpcserialio'
        cls.send(cls.rpc_conn, request)
        response = cls.recv(cls.rpc_conn, KRPC.ConnectionResponse)
        assert response.status == KRPC.ConnectionResponse.OK

    @staticmethod
    def send(conn, msg):
        data = msg.SerializeToString()
        size = encode_varint(len(data))
        written = conn.write(size + data)
        assert written == len(size + data)

    @staticmethod
    def recv(conn, msg_type):
        """ Receive a message, prefixed with its size,
            from a Serial IO port """
        # Receive the size of the message data
        data = b''
        while True:
            try:
                data += conn.read(1)
                size = decode_varint(data)
                break
            except IndexError:
                pass
        # Receive the message data
        data = conn.read(size)
        # Decode the message
        msg = msg_type()
        msg.ParseFromString(data)
        return msg

    @staticmethod
    def encode_int32(value):
        data = []
        _SignedVarintEncoder(data.append, ZigZagEncode(value))
        return b''.join(data)

    @staticmethod
    def decode_int32(data):
        return ZigZagDecode(_DecodeSignedVarint(data, 0)[0])

    @staticmethod
    def decode_bytes(data):
        return data[_DecodeVarint(data, 0)[1]:]

    @classmethod
    def decode_string(cls, data):
        return cls.decode_bytes(data).decode('utf-8')

    def rpc_send(self, msg):
        return self.send(self.rpc_conn, msg)

    def rpc_recv(self, typ):
        return self.recv(self.rpc_conn, typ)


class TestRPCServer(SerialIOTest):

    @classmethod
    def setUpClass(cls):
        cls.connect(cls.port_name())

    def test_get_client_name(self):
        call = KRPC.ProcedureCall()
        call.service = 'KRPC'
        call.procedure = 'GetClientName'
        request = KRPC.MultiplexedRequest()
        request.request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.MultiplexedResponse).response
        self.assertEqual(
            'krpcserialio', self.decode_string(response.results[0].value))

    def test_procedure_call(self):
        call = KRPC.ProcedureCall()
        call.service = 'KRPC'
        call.procedure = 'GetStatus'
        request = KRPC.MultiplexedRequest()
        request.request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.MultiplexedResponse).response
        msg = KRPC.Status()
        msg.ParseFromString(response.results[0].value)
        self.assertNotEqual('', msg.version)

    def test_procedure_call_with_arg(self):
        call = KRPC.ProcedureCall()
        call.service = 'TestService'
        call.procedure = 'Int32ToString'
        arg = KRPC.Argument()
        arg.position = 0
        arg.value = self.encode_int32(42)
        call.arguments.extend([arg])
        request = KRPC.MultiplexedRequest()
        request.request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.MultiplexedResponse).response
        self.assertEqual(1, len(response.results))
        self.assertEqual('42', self.decode_string(response.results[0].value))

    def test_reconnect(self):
        self.test_procedure_call()
        self.connect(self.port_name())
        self.test_procedure_call()


if __name__ == '__main__':
    unittest.main()
