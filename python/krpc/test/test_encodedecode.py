#!/usr/bin/env python2

import unittest
import binascii
from krpc.encoder import _Encoder as Encoder
from krpc.decoder import _Decoder as Decoder
from krpc.types import _Types as Types
import krpc.schema.KRPC

class TestEncoder(unittest.TestCase):

    def _run_test_encode_value(self, typ, cases):
        for decoded, encoded in cases:
            data = Encoder.encode(decoded, Types().as_type(typ))
            self.assertEquals(encoded, binascii.hexlify(data))

    def _run_test_decode_value(self, typ, cases):
        for decoded, encoded in cases:
            value = Decoder.decode(binascii.unhexlify(encoded), Types().as_type(typ))
            if typ in ('float','double'):
                self.assertEqual(str(decoded)[0:8], str(value)[0:8])
            else:
                self.assertEqual(decoded, value)

    def test_encode_double_value(self):
        cases = [
            (0.0, '0000000000000000'),
            (-1.0, '000000000000f0bf'),
            (3.14159265359, 'ea2e4454fb210940')
            # TODO: test infinities
        ]
        self._run_test_encode_value('double', cases)
        self._run_test_decode_value('double', cases)

    def test_encode_float_value(self):
        cases = [
            (3.14159265359, 'db0f4940'),
            (-1.0, '000080bf'),
            (0.0, '00000000')
        ]
        self._run_test_encode_value('float', cases)
        self._run_test_decode_value('float', cases)

    def test_encode_int32_value(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (-33, 'dfffffffffffffffff01')
            # TODO: test max/min int
        ]
        self._run_test_encode_value('int32', cases)
        self._run_test_decode_value('int32', cases)

    def test_encode_int64_value(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (1234567890000L, 'd088ec8ff723'),
            (-33, 'dfffffffffffffffff01')
        ]
        self._run_test_encode_value('int64', cases)
        self._run_test_decode_value('int64', cases)

    def test_encode_uint32_value(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02')
            # TODO: test max/min int
        ]
        self._run_test_encode_value('uint32', cases)
        self._run_test_decode_value('uint32', cases)

        self.assertRaises(ValueError,Encoder.encode, -1, Types().as_type('uint32'))
        self.assertRaises(ValueError,Encoder.encode, -849, Types().as_type('uint32'))

    def test_encode_uint64_value(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (1234567890000L, 'd088ec8ff723')
            # TODO: test max/min int
        ]
        self._run_test_encode_value('uint64', cases)
        self._run_test_decode_value('uint64', cases)

        self.assertRaises(ValueError,Encoder.encode, -1, Types().as_type('uint64'))
        self.assertRaises(ValueError,Encoder.encode, -849, Types().as_type('uint64'))

    def test_encode_bool_value(self):
        cases = [
            (True,  '01'),
            (False, '00')
        ]
        self._run_test_encode_value('bool', cases)
        self._run_test_decode_value('bool', cases)

    def test_encode_string_value(self):
        cases = [
            ('', '00'),
            ('testing', '0774657374696e67'),
            ('One small step for Kerbal-kind!', '1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421')
        ]
        self._run_test_encode_value('string', cases)
        self._run_test_decode_value('string', cases)

    def test_encode_bytearray_value(self):
        cases = [
            (b'', '00'),
            (b'\xba\xda\x55', '03bada55'),
            (b'\xde\xad\xbe\xef', '04deadbeef')
        ]
        self._run_test_encode_value('bytes', cases)
        self._run_test_decode_value('bytes', cases)

if __name__ == '__main__':
    unittest.main()
