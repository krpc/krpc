import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.csharp import CsharpGenerator


class TestClientGenCsharp(ClientGenTestCase, unittest.TestCase):
    language = 'csharp'
    generator = CsharpGenerator


if __name__ == '__main__':
    unittest.main()
