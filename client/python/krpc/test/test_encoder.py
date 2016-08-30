import unittest
from krpc.encoder import Encoder
from krpc.types import Types
from krpc.types import ClassBase
from krpc.platform import hexlify
import krpc.schema.KRPC


class TestEncoder(unittest.TestCase):
    types = Types()

    def test_rpc_hello_message(self):
        message = Encoder.RPC_HELLO_MESSAGE
        self.assertEqual(12, len(message))
        self.assertEqual('48454c4c4f2d525043000000', hexlify(message))

    def test_stream_hello_message(self):
        message = Encoder.STREAM_HELLO_MESSAGE
        self.assertEqual(12, len(message))
        self.assertEqual('48454c4c4f2d53545245414d', hexlify(message))

    def test_client_name(self):
        message = Encoder.client_name('foo')
        self.assertEqual(32, len(message))
        self.assertEqual('666f6f' + '00' * 29, hexlify(message))

    def test_empty_client_name(self):
        message = Encoder.client_name()
        self.assertEqual(32, len(message))
        self.assertEqual('00' * 32, hexlify(message))

    def test_long_client_name(self):
        message = Encoder.client_name('a' * 33)
        self.assertEqual(32, len(message))
        self.assertEqual('61' * 32, hexlify(message))

    def test_encode_message(self):
        request = krpc.schema.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode(request, self.types.as_type('KRPC.Request'))
        expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEqual(expected, hexlify(data))

    def test_encode_value(self):
        data = Encoder.encode(300, self.types.as_type('int32'))
        self.assertEqual('ac02', hexlify(data))

    def test_encode_unicode_string(self):
        data = Encoder.encode(b'\xe2\x84\xa2'.decode('utf-8'), self.types.as_type('string'))
        self.assertEqual('03e284a2', hexlify(data))

    def test_encode_message_delimited(self):
        request = krpc.schema.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode_delimited(request, self.types.as_type('KRPC.Request'))
        expected = '1c' + '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEqual(expected, hexlify(data))

    def test_encode_value_delimited(self):
        data = Encoder.encode_delimited(300, self.types.as_type('int32'))
        self.assertEqual('02' + 'ac02', hexlify(data))

    def test_encode_class(self):
        typ = self.types.as_type('Class(ServiceName.ClassName)')
        class_type = typ.python_type
        self.assertTrue(issubclass(class_type, ClassBase))
        value = class_type(300)
        self.assertEqual(300, value._object_id)
        data = Encoder.encode(value, typ)
        self.assertEqual('ac02', hexlify(data))

    def test_encode_class_none(self):
        typ = self.types.as_type('Class(ServiceName.ClassName)')
        value = None
        data = Encoder.encode(value, typ)
        self.assertEqual('00', hexlify(data))

    def test_encode_tuple_wrong_arity(self):
        typ = self.types.as_type('Tuple(int32,int32,int32)')
        value = (0, 1)
        self.assertRaises(ValueError, Encoder.encode, value, typ)


if __name__ == '__main__':
    unittest.main()
