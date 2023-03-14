from __future__ import annotations
from typing import Iterable, List, Tuple
import unittest
import sys
from krpc.encoder import Encoder
from krpc.error import EncodingError
from krpc.decoder import Decoder
from krpc.types import Types, TypeBase
from krpc.platform import hexlify, unhexlify


class TestEncodeDecode(unittest.TestCase):
    types = Types()

    def _run_test_encode_value(self, typ: TypeBase,
                               cases: Iterable[Tuple[object, str]]) -> None:
        for decoded, encoded in cases:
            data = Encoder.encode(decoded, typ)
            self.assertEqual(encoded, hexlify(data))

    def _run_test_decode_value(self, typ: TypeBase,
                               cases: Iterable[Tuple[object, str]]) -> None:
        for decoded, encoded in cases:
            value = Decoder.decode(None, unhexlify(encoded), typ)
            if typ.python_type == float:
                self.assertEqual(str(decoded)[0:8], str(value)[0:8])
            else:
                self.assertEqual(decoded, value)

    def test_double(self) -> None:
        cases: List[Tuple[object, str]] = [
            (0.0, '0000000000000000'),
            (-1.0, '000000000000f0bf'),
            (3.14159265359, 'ea2e4454fb210940'),
            (float('inf'), '000000000000f07f'),
            (-float('inf'), '000000000000f0ff'),
            (float('nan'), '000000000000f87f')
        ]
        self._run_test_encode_value(self.types.double_type, cases)
        self._run_test_decode_value(self.types.double_type, cases)

    def test_float(self) -> None:
        cases: List[Tuple[object, str]] = [
            (3.14159265359, 'db0f4940'),
            (-1.0, '000080bf'),
            (0.0, '00000000'),
            (float('inf'), '0000807f'),
            (-float('inf'), '000080ff'),
            (float('nan'), '0000c07f')
        ]
        self._run_test_encode_value(self.types.float_type, cases)
        self._run_test_decode_value(self.types.float_type, cases)

    def test_sint32(self) -> None:
        cases: List[Tuple[object, str]] = [
            (0, '00'),
            (1, '02'),
            (42, '54'),
            (300, 'd804'),
            (-33, '41'),
            (2147483647, 'feffffff0f'),
            (-2147483648, 'ffffffff0f')
        ]
        self._run_test_encode_value(self.types.sint32_type, cases)
        self._run_test_decode_value(self.types.sint32_type, cases)

    def test_sint64(self) -> None:
        cases: List[Tuple[object, str]] = [
            (0, '00'),
            (1, '02'),
            (42, '54'),
            (300, 'd804'),
            (1234567890000, 'a091d89fee47'),
            (-33, '41')
        ]
        self._run_test_encode_value(self.types.sint64_type, cases)
        self._run_test_decode_value(self.types.sint64_type, cases)

    def test_uint32(self) -> None:
        cases = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (sys.maxsize, 'ffffffffffffffff7f')
        ]
        self._run_test_encode_value(self.types.uint32_type, cases)
        self._run_test_decode_value(self.types.uint32_type, cases)

        self.assertRaises(EncodingError, Encoder.encode,
                          -1, self.types.uint32_type)
        self.assertRaises(EncodingError, Encoder.encode,
                          -849, self.types.uint32_type)

    def test_uint64(self) -> None:
        cases: List[Tuple[object, str]] = [
            (0, '00'),
            (1, '01'),
            (42, '2a'),
            (300, 'ac02'),
            (1234567890000, 'd088ec8ff723')
        ]
        self._run_test_encode_value(self.types.uint64_type, cases)
        self._run_test_decode_value(self.types.uint64_type, cases)

        self.assertRaises(EncodingError, Encoder.encode,
                          -1, self.types.uint64_type)
        self.assertRaises(EncodingError, Encoder.encode,
                          -849, self.types.uint64_type)

    def test_bool(self) -> None:
        cases: List[Tuple[object, str]] = [
            (True, '01'),
            (False, '00')
        ]
        self._run_test_encode_value(self.types.bool_type, cases)
        self._run_test_decode_value(self.types.bool_type, cases)

    def test_string(self) -> None:
        cases: List[Tuple[object, str]] = [
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
        self._run_test_encode_value(self.types.string_type, cases)
        self._run_test_decode_value(self.types.string_type, cases)

    def test_bytes(self) -> None:
        cases: List[Tuple[object, str]] = [
            (b'', '00'),
            (b'\xba\xda\x55', '03bada55'),
            (b'\xde\xad\xbe\xef', '04deadbeef')
        ]
        self._run_test_encode_value(self.types.bytes_type, cases)
        self._run_test_decode_value(self.types.bytes_type, cases)

    def test_tuple(self) -> None:
        cases: List[Tuple[object, str]] = [((1,), '0a0101')]
        self._run_test_encode_value(
            self.types.tuple_type(self.types.uint32_type), cases)
        self._run_test_decode_value(
            self.types.tuple_type(self.types.uint32_type), cases)
        cases = [((1, 'jeb', False), '0a01010a04036a65620a0100')]
        typ = self.types.tuple_type(
            self.types.uint32_type,
            self.types.string_type,
            self.types.bool_type)
        self._run_test_encode_value(typ, cases)
        self._run_test_decode_value(typ, cases)

    def test_list(self) -> None:
        cases: List[Tuple[object, str]] = [
            ([], ''),
            ([1], '0a0101'),
            ([1, 2, 3, 4], '0a01010a01020a01030a0104')
        ]
        typ = self.types.list_type(self.types.uint32_type)
        self._run_test_encode_value(typ, cases)
        self._run_test_decode_value(typ, cases)

    def test_set(self) -> None:
        cases: List[Tuple[object, str]] = [
            (set(), ''),
            (set([1]), '0a0101'),
            (set([1, 2, 3, 4]), '0a01010a01020a01030a0104')
        ]
        typ = self.types.set_type(self.types.uint32_type)
        self._run_test_encode_value(typ, cases)
        self._run_test_decode_value(typ, cases)

    def test_dictionary(self) -> None:
        cases: List[Tuple[object, str]] = [
            ({}, ''),
            ({'': 0}, '0a060a0100120100'),
            ({'foo': 42, 'bar': 365, 'baz': 3},
             '0a0a0a04036261721202ed020a090a0403' +
             '62617a1201030a090a0403666f6f12012a')
        ]
        typ = self.types.dictionary_type(
            self.types.string_type, self.types.uint32_type)
        self._run_test_encode_value(typ, cases)
        self._run_test_decode_value(typ, cases)


if __name__ == '__main__':
    unittest.main()
