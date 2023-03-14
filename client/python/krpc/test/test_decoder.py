import unittest
from krpc.decoder import Decoder
from krpc.types import Types
from krpc.platform import unhexlify


class TestDecoder(unittest.TestCase):
    types = Types()

    def test_guid(self) -> None:
        self.assertEqual(
            '6f271b39-00dd-4de4-9732-f0d3a68838df',
            Decoder.guid(unhexlify('391b276fdd00e44d9732f0d3a68838df')))

    def test_decode_value(self) -> None:
        value = Decoder.decode(None, unhexlify('ac02'), self.types.uint32_type)
        self.assertEqual(int(300), value)

    def test_decode_unicode_string(self) -> None:
        value = Decoder.decode(
            None, unhexlify('03e284a2'), self.types.string_type
        )
        self.assertEqual(b'\xe2\x84\xa2'.decode('utf-8'), value)

    def test_message_size(self) -> None:
        message = '1c'
        size = Decoder.decode_message_size(unhexlify(message))
        self.assertEqual(28, size)

    def test_decode_message(self) -> None:
        message = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
        call = Decoder.decode(
            None, unhexlify(message), self.types.procedure_call_type)
        self.assertEqual('ServiceName', call.service)  # type: ignore[attr-defined]
        self.assertEqual('ProcedureName', call.procedure)  # type: ignore[attr-defined]

    def test_decode_class(self) -> None:
        typ = self.types.class_type('ServiceName', 'ClassName')
        value = Decoder.decode(None, unhexlify('ac02'), typ)
        self.assertTrue(isinstance(value, typ.python_type))
        self.assertEqual(300, value._object_id)  # type: ignore[attr-defined]

    def test_decode_class_none(self) -> None:
        typ = self.types.class_type('ServiceName', 'ClassName')
        value = Decoder.decode(None, unhexlify('00'), typ)
        self.assertIsNone(value)


if __name__ == '__main__':
    unittest.main()
