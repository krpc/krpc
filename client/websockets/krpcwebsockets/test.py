import base64
import os
import unittest
import websocket
import krpc.schema.KRPC_pb2 as KRPC
import google.protobuf
from google.protobuf.internal.decoder import _DecodeVarint, _DecodeSignedVarint
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal.wire_format import ZigZagEncode, ZigZagDecode


# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
# pylint: disable=invalid-name,protected-access
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()
_pb_version = google.protobuf.__version__.split('.')
if int(_pb_version[0]) >= 3 and int(_pb_version[1]) >= 4:
    # protobuf v3.4.0 and above
    def _SignedVarintEncoder(write, value):
        return _pb_SignedVarintEncoder(write, value, True)
else:
    # protobuf v3.3.0 and below
    _SignedVarintEncoder = _pb_SignedVarintEncoder
# pylint: enable=invalid-name,protected-access


class WebSocketsTest(unittest.TestCase):

    @staticmethod
    def address():
        return '127.0.0.1'

    @staticmethod
    def rpc_port():
        return int(os.getenv('RPC_PORT', 50000))

    @staticmethod
    def stream_port():
        return int(os.getenv('STREAM_PORT', 50001))

    @classmethod
    def connect(cls, address, rpc_port, stream_port, name=None):
        if name is not None:
            name = '?name='+name
        else:
            name = ''
        cls.rpc_conn = websocket.create_connection(
            'ws://%s:%d/%s' % (address, rpc_port, name))
        if stream_port is None:
            cls.stream_conn = None
        else:
            call = KRPC.ProcedureCall()
            call.service = 'KRPC'
            call.procedure = 'GetClientID'
            request = KRPC.Request()
            request.calls.extend([call])
            cls.send(cls.rpc_conn, request)
            response = cls.recv(cls.rpc_conn, KRPC.Response)
            client_identifier = cls.decode_bytes(response.results[0].value)
            client_identifier = base64.b64encode(client_identifier)
            cls.stream_conn = websocket.create_connection(
                'ws://%s:%d/?id=%s' %
                (address, stream_port, client_identifier))

    @staticmethod
    def send(conn, msg):
        data = msg.SerializeToString()
        conn.send_binary(data)

    @staticmethod
    def recv(conn, msg_type):
        data = conn.recv()
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
        return str(cls.decode_bytes(data))

    def rpc_send(self, msg):
        return self.send(self.rpc_conn, msg)

    def rpc_recv(self, typ):
        return self.recv(self.rpc_conn, typ)

    def stream_recv(self, typ):
        return self.recv(self.stream_conn, typ)


class TestConnection(WebSocketsTest):

    def test_named_client(self):
        self.connect(self.address(), self.rpc_port(), None,
                     name='TheClientName')
        call = KRPC.ProcedureCall()
        call.service = 'KRPC'
        call.procedure = 'GetClientName'
        request = KRPC.Request()
        request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.Response)
        self.assertEqual(
            'TheClientName', self.decode_string(response.results[0].value))

    def test_no_client_id(self):
        self.assertRaises(
            websocket.WebSocketBadStatusException,
            websocket.create_connection,
            'ws://%s:%d/' % (self.address(), self.stream_port()))

    def test_invalid_client_id(self):
        self.assertRaises(
            websocket.WebSocketBadStatusException,
            websocket.create_connection,
            'ws://%s:%d/?id=abc' % (self.address(), self.stream_port()))


class TestRPCServer(WebSocketsTest):

    @classmethod
    def setUpClass(cls):
        cls.connect(cls.address(), cls.rpc_port(), None)

    def test_procedure_call(self):
        call = KRPC.ProcedureCall()
        call.service = 'KRPC'
        call.procedure = 'GetStatus'
        request = KRPC.Request()
        request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.Response)
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
        request = KRPC.Request()
        request.calls.extend([call])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.Response)
        self.assertEqual('42', self.decode_string(response.results[0].value))


class TestStreamServer(WebSocketsTest):

    @classmethod
    def setUpClass(cls):
        cls.connect(address=cls.address(),
                    rpc_port=cls.rpc_port(),
                    stream_port=cls.stream_port())

    def test_procedure_call(self):
        # Create call to be streamed
        call = KRPC.ProcedureCall()
        call.service = 'TestService'
        call.procedure = 'Counter'

        # Call AddStream
        add_stream = KRPC.ProcedureCall()
        add_stream.service = 'KRPC'
        add_stream.procedure = 'AddStream'
        add_stream_arg = KRPC.Argument()
        add_stream_arg.position = 0
        add_stream_arg.value = call.SerializeToString()
        add_stream.arguments.extend([add_stream_arg])
        request = KRPC.Request()
        request.calls.extend([add_stream])
        self.rpc_send(request)
        response = self.rpc_recv(KRPC.Response)
        self.assertEqual(1, len(response.results))
        stream = KRPC.Stream()
        stream.ParseFromString(response.results[0].value)

        # Receive some stream updates
        for expected in range(1, 10):
            update = self.stream_recv(KRPC.StreamUpdate)
            self.assertEqual(1, len(update.results))
            self.assertEqual(stream.id, update.results[0].id)
            actual = self.decode_int32(update.results[0].result.value)
            self.assertEqual(expected, actual)


if __name__ == '__main__':
    unittest.main()
