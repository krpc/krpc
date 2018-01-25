import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.java import JavaGenerator


class TestClientGenJava(ClientGenTestCase, unittest.TestCase):
    language = 'java'
    generator = JavaGenerator


if __name__ == '__main__':
    unittest.main()
