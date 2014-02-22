#!/usr/bin/env python2

import unittest
import binascii
from krpc import _Encoder as Encoder
import proto.KRPC

class TestEncoder(unittest.TestCase):

    def test_hello_message(self):
        message = Encoder.hello_message()
        self.assertEqual (8 + 32, len(message))
        self.assertEqual ('48454c4c4fbada55', binascii.hexlify(message[:8]))
        self.assertEqual ('00'*32, binascii.hexlify(message[8:]))

    def test_hello_message_with_identifier(self):
        message = Encoder.hello_message(name='foo')
        self.assertEqual (8 + 32, len(message))
        self.assertEqual ('48454c4c4fbada55', binascii.hexlify(message[:8]))
        self.assertEqual ('666f6f'+'00'*29, binascii.hexlify(message[8:]))

    def test_hello_message_with_empty_identifier(self):
        message = Encoder.hello_message(name='')
        self.assertEqual (8 + 32, len(message))
        self.assertEqual ('48454c4c4fbada55', binascii.hexlify(message[:8]))
        self.assertEqual ('00'*32, binascii.hexlify(message[8:]))

    def test_hello_message_with_long_identifier(self):
        message = Encoder.hello_message(name='a'*33)
        self.assertEqual (8 + 32, len(message))
        self.assertEqual ('48454c4c4fbada55', binascii.hexlify(message[:8]))
        self.assertEqual ('61'*32, binascii.hexlify(message[8:]))

    def test_encode_message(self):
        request = proto.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode(request)
        expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_value(self):
        data = Encoder.encode(int(300))
        expected = 'ac02'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_message_delimited(self):
        request = proto.KRPC.Request()
        request.service = 'ServiceName'
        request.procedure = 'ProcedureName'
        data = Encoder.encode_delimited(request)
        expected = '1c'+'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_value_delimited(self):
        data = Encoder.encode_delimited(int(300))
        expected = '02'+'ac02'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_float_value(self):
        data = Encoder.encode(float(3.14159))
        expected = 'd00f4940' # TODO : check this
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_int_value(self):
        data = Encoder.encode(42)
        expected = '2a'
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_long_value(self):
        data = Encoder.encode(1234567890000L)
        expected = 'd088ec8ff723' # TODO: check this
        self.assertEquals(expected, binascii.hexlify(data))

    def test_encode_bool_value(self):
        data = Encoder.encode(True)
        self.assertEquals('01', binascii.hexlify(data))
        data = Encoder.encode(False)
        self.assertEquals('00', binascii.hexlify(data))

    def test_encode_string_value(self):
        data = Encoder.encode('testing')
        expected = '0774657374696e67'
        self.assertEquals(expected, binascii.hexlify(data))

if __name__ == '__main__':
    unittest.main()
