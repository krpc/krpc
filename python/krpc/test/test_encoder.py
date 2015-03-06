#!/usr/bin/env python2

import unittest
import binascii
from krpc.encoder import _Encoder as Encoder
from krpc.types import _Types as Types
from krpc.types import _BaseClass as BaseClass
import krpc.schema.KRPC

class TestEncoder(unittest.TestCase):

    def test_rpc_hello_message(self):
        message = Encoder.RPC_HELLO_MESSAGE
        self.assertEqual (12, len(message))
        self.assertEqual ('48454c4c4f2d525043000000', binascii.hexlify(message))

    def test_stream_hello_message(self):
        message = Encoder.STREAM_HELLO_MESSAGE
        self.assertEqual (12, len(message))
        self.assertEqual ('48454c4c4f2d53545245414d', binascii.hexlify(message))

    def test_client_name(self):
        message = Encoder.client_name('foo')
        self.assertEqual (32, len(message))
        self.assertEqual ('666f6f'+'00'*29, binascii.hexlify(message))

    def test_empty_client_name(self):
        message = Encoder.client_name()
        self.assertEqual (32, len(message))
        self.assertEqual ('00'*32, binascii.hexlify(message))

    def test_long_client_name(self):
        message = Encoder.client_name('a'*33)
        self.assertEqual (32, len(message))
        self.assertEqual ('61'*32, binascii.hexlify(message))

    def test_encode_message(self):
        request = krpc.schema.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode(request, Types().as_type('KRPC.Request'))
        expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_value(self):
        data = Encoder.encode(300, Types().as_type('int32'))
        self.assertEquals('ac02', binascii.hexlify(data))

    def test_encode_message_delimited(self):
        request = krpc.schema.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode_delimited(request, Types().as_type('KRPC.Request'))
        expected = '1c'+'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_value_delimited(self):
        data = Encoder.encode_delimited(300, Types().as_type('int32'))
        self.assertEquals('02'+'ac02', binascii.hexlify(data))

    def test_encode_class(self):
        typ = Types().as_type('Class(ServiceName.ClassName)')
        class_type = typ.python_type
        self.assertTrue(issubclass(class_type, BaseClass))
        value = class_type(300)
        self.assertEquals(300, value._object_id)
        data = Encoder.encode(value, typ)
        self.assertEquals('ac02', binascii.hexlify(data))

    def test_encode_class_none(self):
        typ = Types().as_type('Class(ServiceName.ClassName)')
        value = None
        data = Encoder.encode(value, typ)
        self.assertEquals('00', binascii.hexlify(data))

if __name__ == '__main__':
    unittest.main()
