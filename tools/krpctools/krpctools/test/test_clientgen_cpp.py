import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.cpp import CppGenerator


class TestClientGenCpp(ClientGenTestCase, unittest.TestCase):
    language = 'cpp'
    generator = CppGenerator


if __name__ == '__main__':
    unittest.main()
