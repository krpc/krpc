import os
import unittest

import google.protobuf
import websocket
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal.decoder import _DecodeSignedVarint, _DecodeVarint
from google.protobuf.internal.wire_format import ZigZagDecode, ZigZagEncode

import krpc.schema.KRPC_pb2 as KRPC

# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()
_pb_version = google.protobuf.__version__.split(".")
if int(_pb_version[0]) >= 3 and int(_pb_version[1]) >= 4:
    # protobuf v3.4.0 and above
    def _SignedVarintEncoder(write, value):
        return _pb_SignedVarintEncoder(write, value, True)

else:
    # protobuf v3.3.0 and below
    _SignedVarintEncoder = _pb_SignedVarintEncoder


class WebSocketsTest(unittest.TestCase):
    @staticmethod
    def address():
        return "127.0.0.1"

    @staticmethod
    def tcpip_port():
        return int(os.getenv("KRPC_TEST_TCPIP_PORT", "50000"))

    @classmethod
    def connect(cls, address, port, name="krpcwebsockets"):
        """Upgrade to a websocket, then make the handshake in the first binary message."""
        cls.conn = websocket.create_connection("ws://%s:%d/" % (address, port))
        cls.next_id = 1
        message = KRPC.Message()
        message.connection_request.client_name = name
        message.connection_request.protocol_version = 2
        cls.send(cls.conn, message)
        response = cls.recv(cls.conn).connection_response
        assert response.status == KRPC.ConnectionResponse.OK
        assert response.protocol_version == 2

    @staticmethod
    def send(conn, msg):
        data = msg.SerializeToString()
        conn.send_binary(data)

    @staticmethod
    def recv(conn):
        data = conn.recv()
        msg = KRPC.Message()
        msg.ParseFromString(data)
        return msg

    @staticmethod
    def encode_int32(value):
        data = []
        _SignedVarintEncoder(data.append, ZigZagEncode(value))
        return b"".join(data)

    @staticmethod
    def decode_int32(data):
        return ZigZagDecode(_DecodeSignedVarint(data, 0)[0])

    @staticmethod
    def decode_bytes(data):
        return data[_DecodeVarint(data, 0)[1] :]

    @classmethod
    def decode_string(cls, data):
        return cls.decode_bytes(data).decode("utf-8")

    def rpc_send(self, msg):
        return self.send(self.conn, msg)

    def rpc_recv(self):
        return self.recv(self.conn)

    def call(self, service, procedure, *arguments):
        """Make a call, and return the response to it."""
        message = KRPC.Message()
        message.request.id = self.next_id
        self.next_id += 1
        call = message.request.calls.add()
        call.service = service
        call.procedure = procedure
        for position, value in enumerate(arguments):
            call.arguments.add(position=position, value=value)
        self.rpc_send(message)
        response = self.rpc_recv().response
        self.assertEqual(message.request.id, response.id)
        return response


class TestConnection(WebSocketsTest):
    def test_named_client(self):
        self.connect(self.address(), self.tcpip_port(), name="TheClientName")
        response = self.call("KRPC", "GetClientName")
        self.assertEqual("TheClientName", self.decode_string(response.results[0].value))

    def test_subprotocol_is_not_returned(self):
        # The server ignores a subprotocol and returns none, which a client asking for one,
        # such as a client from kRPC 0.7, rejects
        self.assertRaises(
            websocket.WebSocketException,
            websocket.create_connection,
            "ws://%s:%d/" % (self.address(), self.tcpip_port()),
            subprotocols=["krpc.1"],
        )

    def test_unsupported_protocol_version(self):
        conn = websocket.create_connection(
            "ws://%s:%d/" % (self.address(), self.tcpip_port())
        )
        message = KRPC.Message()
        message.connection_request.protocol_version = 1
        self.send(conn, message)
        response = self.recv(conn).connection_response
        self.assertEqual(
            KRPC.ConnectionResponse.WRONG_PROTOCOL_VERSION, response.status
        )
        conn.close()


class TestServer(WebSocketsTest):
    @classmethod
    def setUpClass(cls):
        cls.connect(cls.address(), cls.tcpip_port())

    def test_procedure_call(self):
        response = self.call("KRPC", "GetStatus")
        msg = KRPC.Status()
        msg.ParseFromString(response.results[0].value)
        self.assertNotEqual("", msg.version)
        self.assertEqual(2, msg.protocol_version)

    def test_procedure_call_with_arg(self):
        response = self.call("TestService", "Int32ToString", self.encode_int32(42))
        self.assertEqual("42", self.decode_string(response.results[0].value))


class TestStreams(WebSocketsTest):
    @classmethod
    def setUpClass(cls):
        cls.connect(cls.address(), cls.tcpip_port())

    def test_stream_updates_arrive_on_the_same_connection(self):
        # Create call to be streamed
        call = KRPC.ProcedureCall()
        call.service = "TestService"
        call.procedure = "Counter"

        response = self.call("KRPC", "AddStream", call.SerializeToString())
        self.assertEqual(1, len(response.results))
        stream = KRPC.Stream()
        stream.ParseFromString(response.results[0].value)

        # Receive some stream updates, which follow the response to the call that added it
        for expected in range(1, 10):
            update = self.rpc_recv().stream_update
            self.assertEqual(1, len(update.results))
            self.assertEqual(stream.id, update.results[0].id)
            actual = self.decode_int32(update.results[0].result.value)
            self.assertEqual(expected, actual)


if __name__ == "__main__":
    unittest.main()
