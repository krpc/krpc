import unittest
from krpc.platform import bytelength, hexlify, unhexlify


class TestPlatform(unittest.TestCase):
    def test_bytelength(self):
        self.assertEqual(0, bytelength(''))
        self.assertEqual(3, bytelength('foo'))
        self.assertEqual(3, bytelength(b'\xe2\x84\xa2'.decode('utf-8')))
        self.assertEqual(3, bytelength(u'\u2122'))

    def test_hexlify(self):
        self.assertEqual(hexlify(b''), '')
        self.assertEqual(hexlify(b'\x00\x01\x02'), '000102')
        self.assertEqual(hexlify(b'\xFF'), 'ff')

    def test_unhexlify(self):
        self.assertEqual(unhexlify(''), b'')
        self.assertEqual(unhexlify('000102'), b'\x00\x01\x02')
        self.assertEqual(unhexlify('ff'), b'\xFF')

if __name__ == '__main__':
    unittest.main()
