#!/usr/bin/env python2

import unittest
import binascii
from krpc import _Decoder as Decoder
import proto.KRPC

class TestDecoder(unittest.TestCase):

    def test_decode_message(self):
        typ = proto.KRPC.Request
        message = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        request = Decoder.decode(typ, binascii.unhexlify(message))
        self.assertEquals('ServiceName', request.service)
        self.assertEquals('ProcedureName', request.procedure)

    def test_decode_value(self):
        value = Decoder.decode(int, binascii.unhexlify('ac02'))
        self.assertEquals(int(300), value)

    def test_decode_message_delimited(self):
        typ = proto.KRPC.Request
        message = '1c'+'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        request = Decoder.decode_delimited(typ, binascii.unhexlify(message))
        self.assertEquals('ServiceName', request.service)
        self.assertEquals('ProcedureName', request.procedure)

    def test_decode_value_delimited(self):
        value = Decoder.decode_delimited(int, binascii.unhexlify('02'+'ac02'))
        self.assertEquals(300, value)

    def test_decode_float_value(self):
        value = Decoder.decode(float, binascii.unhexlify('d00f4940'))
        self.assertTrue(str(value).startswith('3.14159'))

    def test_decode_int_value(self):
        value = Decoder.decode(int, binascii.unhexlify('2a'))
        self.assertEquals(42, value)

    def test_decode_long_value(self):
        value = Decoder.decode(long, binascii.unhexlify('d088ec8ff723'))
        self.assertEquals(1234567890000L, value)

    def test_decode_bool_value(self):
        value = Decoder.decode(bool, binascii.unhexlify('01'))
        self.assertEquals(True, value)
        value = Decoder.decode(bool, binascii.unhexlify('00'))
        self.assertEquals(False, value)

    def test_decode_string_value(self):
        value = Decoder.decode(str, binascii.unhexlify('0774657374696e67'))
        self.assertEquals('testing', value)

if __name__ == '__main__':
    unittest.main()
