import unittest

from krpctools.clientgen.java import JavaGenerator
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenJava(ClientGenTestCase, unittest.TestCase):
    language = "java"
    generator = JavaGenerator


if __name__ == "__main__":
    unittest.main()
