import unittest
from krpc.encoder import Encoder
from krpc.types import Types
from krpc.types import ClassBase
from krpc.platform import hexlify


class TestEncoder(unittest.TestCase):
    types = Types()

    def test_rpc_hello_message(self):
        message = Encoder.RPC_HELLO_MESSAGE
        self.assertEqual(8, len(message))
        self.assertEqual('4b5250432d525043', hexlify(message))

    def test_stream_hello_message(self):
        message = Encoder.STREAM_HELLO_MESSAGE
        self.assertEqual(8, len(message))
        self.assertEqual('4b5250432d535452', hexlify(message))

    def test_encode_message(self):
        request = self.types.request_type.python_type()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode(request, self.types.request_type)
        expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEqual(expected, hexlify(data))

    def test_encode_value(self):
        data = Encoder.encode(300, self.types.uint32_type)
        self.assertEqual('ac02', hexlify(data))

    def test_encode_unicode_string(self):
        data = Encoder.encode(b'\xe2\x84\xa2'.decode('utf-8'), self.types.string_type)
        self.assertEqual('03e284a2', hexlify(data))

    def test_encode_message_with_size(self):
        request = self.types.request_type.python_type()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode_message_with_size(request)
        expected = '1c' + '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEqual(expected, hexlify(data))

    def test_encode_class(self):
        typ = self.types.class_type('ServiceName', 'ClassName')
        class_type = typ.python_type
        self.assertTrue(issubclass(class_type, ClassBase))
        value = class_type(300)
        self.assertEqual(300, value._object_id)
        data = Encoder.encode(value, typ)
        self.assertEqual('ac02', hexlify(data))

    def test_encode_class_none(self):
        typ = self.types.class_type('ServiceName', 'ClassName')
        value = None
        data = Encoder.encode(value, typ)
        self.assertEqual('00', hexlify(data))

    def test_encode_tuple_wrong_arity(self):
        typ = self.types.tuple_type(self.types.uint32_type, self.types.uint32_type, self.types.uint32_type)
        value = (0, 1)
        self.assertRaises(ValueError, Encoder.encode, value, typ)


if __name__ == '__main__':
    unittest.main()
