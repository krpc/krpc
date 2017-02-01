import unittest
import sys
from krpc.encoder import Encoder
from krpc.decoder import Decoder
from krpc.types import Types
from krpc.platform import hexlify, unhexlify


class TestEncodeDecode(unittest.TestCase):
    types = Types()

    def _run_test_encode_value(self, typ, cases):
        for decoded, encoded in cases:
            data = Encoder.encode(decoded, self.types.as_type(typ))
            self.assertEqual(encoded, hexlify(data))

    def _run_test_decode_value(self, typ, cases):
        for decoded, encoded in cases:
            value = Decoder.decode(unhexlify(encoded), self.types.as_type(typ))
            if typ in ('float', 'double'):
                self.assertEqual(str(decoded)[0:8], str(value)[0:8])
            else:
                self.assertEqual(decoded, value)

    def test_float(self):
        cases = [
            (3.14159265359, 'db0f4940'),
            (-1.0, '000080bf'),
            (0.0, '00000000'),
            (float('inf'), '0000807f'),
            (-float('inf'), '000080ff'),
            (float('nan'), '0000c07f')
        ]
        self._run_test_encode_value('float', cases)
        self._run_test_decode_value('float', cases)

    def test_double(self):
        cases = [
            (0.0, '0000000000000000'),
            (-1.0, '000000000000f0bf'),
            (3.14159265359, 'ea2e4454fb210940'),
            (float('inf'), '000000000000f07f'),
            (-float('inf'), '000000000000f0ff'),
            (float('nan'), '000000000000f87f')
        ]
        self._run_test_encode_value('double', cases)
        self._run_test_decode_value('double', cases)

    def test_int32(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (-33, 'dfffffffffffffffff01'),
            (sys.maxint, 'ffffffffffffffff7f'),
            (-sys.maxint - 1, '80808080808080808001')
        ]
        self._run_test_encode_value('int32', cases)
        self._run_test_decode_value('int32', cases)

    def test_int64(self):
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

    def test_uint32(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (sys.maxint, 'ffffffffffffffff7f')
        ]
        self._run_test_encode_value('uint32', cases)
        self._run_test_decode_value('uint32', cases)

        self.assertRaises(ValueError, Encoder.encode,
                          -1, self.types.as_type('uint32'))
        self.assertRaises(ValueError, Encoder.encode,
                          -849, self.types.as_type('uint32'))

    def test_uint64(self):
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (1234567890000L, 'd088ec8ff723')
        ]
        self._run_test_encode_value('uint64', cases)
        self._run_test_decode_value('uint64', cases)

        self.assertRaises(ValueError, Encoder.encode,
                          -1, self.types.as_type('uint64'))
        self.assertRaises(ValueError, Encoder.encode,
                          -849, self.types.as_type('uint64'))

    def test_bool(self):
        cases = [
            (True, '01'),
            (False, '00')
        ]
        self._run_test_encode_value('bool', cases)
        self._run_test_decode_value('bool', cases)

    def test_string(self):
        cases = [
            ('', '00'),
            ('testing', '0774657374696e67'),
            ('One small step for Kerbal-kind!',
             '1f4f6e6520736d616c6c207374657020' +
             '666f72204b657262616c2d6b696e6421'),
            (b'\xe2\x84\xa2'.decode('utf-8'), '03e284a2'),
            (b'Mystery Goo\xe2\x84\xa2 Containment Unit'.decode('utf-8'),
             '1f4d79737465727920476f6fe284a220' +
             '436f6e7461696e6d656e7420556e6974')
        ]
        self._run_test_encode_value('string', cases)
        self._run_test_decode_value('string', cases)

    def test_bytes(self):
        cases = [
            (b'', '00'),
            (b'\xba\xda\x55', '03bada55'),
            (b'\xde\xad\xbe\xef', '04deadbeef')
        ]
        self._run_test_encode_value('bytes', cases)
        self._run_test_decode_value('bytes', cases)

    def test_list(self):
        cases = [
            ([], ''),
            ([1], '0a0101'),
            ([1, 2, 3, 4], '0a01010a01020a01030a0104')
        ]
        self._run_test_encode_value('List(int32)', cases)
        self._run_test_decode_value('List(int32)', cases)

    def test_dictionary(self):
        cases = [
            ({}, ''),
            ({'': 0}, '0a060a0100120100'),
            ({'foo': 42, 'bar': 365, 'baz': 3},
             '0a0a0a04036261721202ed020a090a0403' +
             '62617a1201030a090a0403666f6f12012a')
        ]
        self._run_test_encode_value('Dictionary(string,int32)', cases)
        self._run_test_decode_value('Dictionary(string,int32)', cases)

    def test_set(self):
        cases = [
            (set(), ''),
            (set([1]), '0a0101'),
            (set([1, 2, 3, 4]), '0a01010a01020a01030a0104')
        ]
        self._run_test_encode_value('Set(int32)', cases)
        self._run_test_decode_value('Set(int32)', cases)

    def test_tuple(self):
        cases = [((1,), '0a0101')]
        self._run_test_encode_value('Tuple(int32)', cases)
        self._run_test_decode_value('Tuple(int32)', cases)
        cases = [((1, 'jeb', False), '0a01010a04036a65620a0100')]
        self._run_test_encode_value('Tuple(int32,string,bool)', cases)
        self._run_test_decode_value('Tuple(int32,string,bool)', cases)


if __name__ == '__main__':
    unittest.main()
