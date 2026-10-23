import unittest

import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder, decode_response_message
from krpc.platform import unhexlify
from krpc.types import Types


class TestDecoder(unittest.TestCase):
    types = Types()

    def test_guid(self) -> None:
        self.assertEqual(
            "6f271b39-00dd-4de4-9732-f0d3a68838df",
            Decoder.guid(unhexlify("391b276fdd00e44d9732f0d3a68838df")),
        )

    def test_decode_value(self) -> None:
        value = Decoder.decode(None, unhexlify("ac02"), self.types.uint32_type)
        self.assertEqual(300, value)

    def test_decode_unicode_string(self) -> None:
        value = Decoder.decode(None, unhexlify("03e284a2"), self.types.string_type)
        self.assertEqual(b"\xe2\x84\xa2".decode("utf-8"), value)

    def test_message_size(self) -> None:
        message = "1c"
        size = Decoder.decode_message_size(unhexlify(message))
        self.assertEqual(28, size)

    def test_decode_message(self) -> None:
        message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65"
        call = Decoder.decode(None, unhexlify(message), self.types.procedure_call_type)
        self.assertEqual("ServiceName", call.service)  # pyright: ignore[reportAttributeAccessIssue]
        self.assertEqual("ProcedureName", call.procedure)  # pyright: ignore[reportAttributeAccessIssue]

    def test_decode_class(self) -> None:
        typ = self.types.class_type("ServiceName", "ClassName")
        value = Decoder.decode(None, unhexlify("ac02"), typ)
        self.assertTrue(isinstance(value, typ.python_type))
        self.assertEqual(300, value._object_id)  # pyright: ignore[reportAttributeAccessIssue]

    def test_decode_response_message(self) -> None:
        # Short and long responses, whose lengths take one and two bytes
        for length in (0, 10, 200, 20000):
            response = KRPC.Response(id=3)
            if length:
                response.results.add(value=b"x" * length)
            data = KRPC.Message(response=response).SerializeToString()
            self.assertEqual(response, decode_response_message(data))

    def test_decode_response_message_without_a_response(self) -> None:
        for message in (
            KRPC.Message(),
            KRPC.Message(disconnect=KRPC.Disconnect(message="x")),
            KRPC.Message(stream_update=KRPC.StreamUpdate()),
        ):
            self.assertIsNone(decode_response_message(message.SerializeToString()))


if __name__ == "__main__":
    unittest.main()
