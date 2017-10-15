import unittest
from krpc.encoder import Encoder
from krpc.error import EncodingError
from krpc.types import Types
from krpc.types import ClassBase
from krpc.platform import hexlify


class TestEncoder(unittest.TestCase):
    types = Types()

    def test_encode_message(self):
        call = self.types.procedure_call_type.python_type()
        call.service = 'ServiceName'
        call.procedure = 'ProcedureName'
        data = Encoder.encode(call, self.types.procedure_call_type)
        expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEqual(expected, hexlify(data))

    def test_encode_value(self):
        data = Encoder.encode(300, self.types.uint32_type)
        self.assertEqual('ac02', hexlify(data))

    def test_encode_unicode_string(self):
        data = Encoder.encode(b'\xe2\x84\xa2'.decode('utf-8'),
                              self.types.string_type)
        self.assertEqual('03e284a2', hexlify(data))

    def test_encode_message_with_size(self):
        call = self.types.procedure_call_type.python_type()
        call.service = 'ServiceName'
        call.procedure = 'ProcedureName'
        data = Encoder.encode_message_with_size(call)
        expected = '1c' + \
                   '0a0b536572766963654e616d6512' + \
                   '0d50726f6365647572654e616d65'
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
        typ = self.types.tuple_type(
            self.types.uint32_type,
            self.types.uint32_type,
            self.types.uint32_type)
        value = (0, 1)
        self.assertRaises(EncodingError, Encoder.encode, value, typ)


if __name__ == '__main__':
    unittest.main()
