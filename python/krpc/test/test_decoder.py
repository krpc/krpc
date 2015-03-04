#!/usr/bin/env python2

import unittest
import binascii
from krpc.decoder import _Decoder as Decoder
from krpc.types import _Types as Types
import krpc.schema.KRPC

class TestDecoder(unittest.TestCase):

    def test_decode_message(self):
        typ = krpc.schema.KRPC.Request
        message = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        request = Decoder.decode(binascii.unhexlify(message), Types().as_type('KRPC.Request'))
        self.assertEquals('ServiceName', request.service)
        self.assertEquals('ProcedureName', request.procedure)

    def test_decode_value(self):
        value = Decoder.decode(binascii.unhexlify('ac02'), Types().as_type('int32'))
        self.assertEquals(int(300), value)

    def test_decode_size_and_position(self):
        message = '1c'
        size,position = Decoder.decode_size_and_position(binascii.unhexlify(message))
        self.assertEquals(28, size)
        self.assertEquals(1, position)

    def test_decode_message_delimited(self):
        typ = krpc.schema.KRPC.Request
        message = '1c'+'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        request = Decoder.decode_delimited(binascii.unhexlify(message), Types().as_type('KRPC.Request'))
        self.assertEquals('ServiceName', request.service)
        self.assertEquals('ProcedureName', request.procedure)

    def test_decode_value_delimited(self):
        value = Decoder.decode_delimited(binascii.unhexlify('02'+'ac02'), Types().as_type('int32'))
        self.assertEquals(300, value)

    def test_decode_class(self):
        typ = Types().as_type('Class(ServiceName.ClassName)')
        value = Decoder.decode(binascii.unhexlify('ac02'), typ)
        self.assertTrue(isinstance(value, typ.python_type))
        self.assertEqual(300, value._object_id)

    def test_decode_class_none(self):
        typ = Types().as_type('Class(ServiceName.ClassName)')
        value = Decoder.decode(binascii.unhexlify('00'), typ)
        self.assertIsNone(value)


if __name__ == '__main__':
    unittest.main()
