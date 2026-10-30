import unittest

import krpc.schema.KRPC_pb2 as KRPC
from krpc.encoder import Encoder, encode_request_message
from krpc.error import EncodingError
from krpc.platform import hexlify
from krpc.types import ClassBase, Types


class TestEncoder(unittest.TestCase):
    types = Types()

    def test_encode_message(self) -> None:
        call = self.types.procedure_call_type.python_type()
        call.service = "ServiceName"
        call.procedure = "ProcedureName"
        data = Encoder.encode(call, self.types.procedure_call_type)
        expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65"
        self.assertEqual(expected, hexlify(data))

    def test_encode_value(self) -> None:
        data = Encoder.encode(300, self.types.uint32_type)
        self.assertEqual("ac02", hexlify(data))

    def test_encode_unicode_string(self) -> None:
        data = Encoder.encode(b"\xe2\x84\xa2".decode("utf-8"), self.types.string_type)
        self.assertEqual("03e284a2", hexlify(data))

    def test_encode_message_with_size(self) -> None:
        call = self.types.procedure_call_type.python_type()
        call.service = "ServiceName"
        call.procedure = "ProcedureName"
        data = Encoder.encode_message_with_size(call)
        expected = (
            "1c" + "0a0b536572766963654e616d6512" + "0d50726f6365647572654e616d65"
        )
        self.assertEqual(expected, hexlify(data))

    def test_encode_class(self) -> None:
        typ = self.types.class_type("ServiceName", "ClassName")
        class_type = typ.python_type
        self.assertTrue(issubclass(class_type, ClassBase))
        value = class_type(None, 300)
        self.assertEqual(300, value._object_id)
        data = Encoder.encode(value, typ)
        self.assertEqual("ac02", hexlify(data))

    def test_encode_tuple_wrong_arity(self) -> None:
        typ = self.types.tuple_type(
            self.types.uint32_type, self.types.uint32_type, self.types.uint32_type
        )
        value = (0, 1)
        self.assertRaises(EncodingError, Encoder.encode, value, typ)

    def test_encode_request_message(self) -> None:
        # Short and long requests, whose lengths take one and two bytes
        for length in (0, 10, 125, 126, 200, 20000):
            request = KRPC.Request(id=7)
            if length:
                request.calls.add(service="x" * length)
            expected = Encoder.encode_message_with_size(KRPC.Message(request=request))
            self.assertEqual(expected, encode_request_message(request))


if __name__ == "__main__":
    unittest.main()
