import os
import unittest

import google.protobuf
import serial
from google.protobuf.internal import encoder as protobuf_encoder
from google.protobuf.internal.decoder import _DecodeSignedVarint, _DecodeVarint
from google.protobuf.internal.wire_format import ZigZagDecode, ZigZagEncode

import krpc.schema.KRPC_pb2 as KRPC

# The following unpacks the internal protobuf decoders, whose signature
# depends on the version of protobuf installed
_pb_VarintEncoder = protobuf_encoder._VarintEncoder()
_pb_SignedVarintEncoder = protobuf_encoder._SignedVarintEncoder()
_pb_version = google.protobuf.__version__.split(".")
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


def encode_varint(value):
    """Encode an int as a protobuf varint"""
    data = []
    _VarintEncoder(data.append, value)
    return b"".join(data)


def decode_varint(data):
    """Decode a protobuf varint to an int"""
    return _DecodeVarint(data, 0)[0]


# Seconds a client on a serial line waits for its connection response before it retries. Longer
# than the server's stall timeout, so the server has dropped a stale partial message by then.
HANDSHAKE_TIMEOUT = 3.5


class SerialIOTest(unittest.TestCase):
    @staticmethod
    def port_name():
        return os.getenv("KRPC_TEST_SERIALIO_PORT", None)

    @classmethod
    def connect(cls, port_name):
        cls.conn = serial.Serial(port_name)
        cls.next_id = 1
        cls.send(cls.conn, cls.connection_request())
        response = cls.recv(cls.conn).connection_response
        assert response.status == KRPC.ConnectionResponse.OK
        assert response.protocol_version == 2

    @staticmethod
    def connection_request():
        message = KRPC.Message()
        message.connection_request.client_name = "krpcserialio"
        message.connection_request.protocol_version = 2
        return message

    @staticmethod
    def send(conn, msg):
        data = msg.SerializeToString()
        size = encode_varint(len(data))
        written = conn.write(size + data)
        assert written == len(size + data)

    @classmethod
    def recv(cls, conn):
        """Receive a Message, prefixed with its size, from a Serial IO port."""
        message = cls.recv_within(conn, None)
        assert message is not None
        return message

    @staticmethod
    def recv_within(conn, timeout):
        """Receive a Message, prefixed with its size, from a Serial IO port. Returns None if
        none arrives within the timeout."""
        conn.timeout = timeout
        try:
            # Receive the size of the message data
            data = b""
            while True:
                byte = conn.read(1)
                if not byte:
                    return None
                data += byte
                try:
                    size = decode_varint(data)
                    break
                except IndexError:
                    pass
            # Receive the message data
            data = conn.read(size)
            assert len(data) == size
        finally:
            conn.timeout = None
        # Decode the message
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

    def resume(self):
        """Connect over a line an earlier session may have left messages on, as a client that
        resets does. Messages from the earlier session are skipped, and a refusal for a
        request the server did not read is treated as no answer, so the request is sent again
        once the handshake times out. Returns the messages skipped."""
        skipped = []
        while True:
            self.rpc_send(self.connection_request())
            while True:
                message = self.recv_within(self.conn, HANDSHAKE_TIMEOUT)
                if message is None:
                    break
                case = message.WhichOneof("content")
                if case == "connection_response":
                    status = message.connection_response.status
                    if status == KRPC.ConnectionResponse.OK:
                        self.next_id = 1
                        return skipped
                    self.assertIn(
                        status,
                        (
                            KRPC.ConnectionResponse.MALFORMED_MESSAGE,
                            KRPC.ConnectionResponse.TIMEOUT,
                        ),
                    )
                skipped.append(message)


class TestServer(SerialIOTest):
    @classmethod
    def setUpClass(cls):
        cls.connect(cls.port_name())

    def test_get_client_name(self):
        response = self.call("KRPC", "GetClientName")
        self.assertEqual("krpcserialio", self.decode_string(response.results[0].value))

    def test_procedure_call(self):
        response = self.call("KRPC", "GetStatus")
        msg = KRPC.Status()
        msg.ParseFromString(response.results[0].value)
        self.assertNotEqual("", msg.version)

    def test_procedure_call_with_arg(self):
        response = self.call("TestService", "Int32ToString", self.encode_int32(42))
        self.assertEqual(1, len(response.results))
        self.assertEqual("42", self.decode_string(response.results[0].value))

    def test_reconnect(self):
        self.test_procedure_call()
        self.rpc_send(self.connection_request())
        response = self.rpc_recv().connection_response
        self.assertEqual(KRPC.ConnectionResponse.OK, response.status)
        self.next_id = 1
        self.test_procedure_call()

    def test_disconnect_then_connect(self):
        self.test_procedure_call()
        message = KRPC.Message()
        message.disconnect.reason = KRPC.Disconnect.CLOSED
        self.rpc_send(message)
        self.rpc_send(self.connection_request())
        response = self.rpc_recv().connection_response
        self.assertEqual(KRPC.ConnectionResponse.OK, response.status)
        self.next_id = 1
        self.test_procedure_call()

    def test_reset_with_a_request_short_of_the_stale_message(self):
        # A reset after sending the size of a message and part of it. The new request ends
        # inside the stale message, so the server answers nothing until the stale message
        # stalls, and ends the session.
        self.conn.write(b"\x40\x0a\x02")
        skipped = self.resume()
        cases = [x.WhichOneof("content") for x in skipped]
        self.assertIn("disconnect", cases)
        self.test_procedure_call()

    def test_reset_with_a_request_completing_the_stale_message(self):
        # A reset after sending the size of a message and part of it. The new request completes
        # the stale message, which fails to parse, and the rest of the request is read as a new
        # message, which the server refuses.
        self.conn.write(b"\x05\x00\x00")
        self.resume()
        self.test_procedure_call()

    def test_queued_handshake_retries(self):
        # Retries that queue while the server is not reading are each answered with a new
        # session. The client keeps reading connection responses until its first response.
        self.rpc_send(self.connection_request())
        self.rpc_send(self.connection_request())
        response = self.rpc_recv().connection_response
        self.assertEqual(KRPC.ConnectionResponse.OK, response.status)
        self.next_id = 1
        message = KRPC.Message()
        message.request.id = self.next_id
        message.request.calls.add(service="KRPC", procedure="GetClientName")
        self.rpc_send(message)
        while True:
            received = self.rpc_recv()
            if received.WhichOneof("content") == "connection_response":
                self.assertEqual(
                    KRPC.ConnectionResponse.OK, received.connection_response.status
                )
                continue
            break
        self.assertEqual(1, received.response.id)
        self.assertEqual(
            "krpcserialio", self.decode_string(received.response.results[0].value)
        )
        self.next_id = 2


if __name__ == "__main__":
    unittest.main()
