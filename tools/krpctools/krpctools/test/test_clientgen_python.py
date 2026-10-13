import unittest

from krpctools.clientgen.python import PythonGenerator
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenPython(ClientGenTestCase, unittest.TestCase):
    language = "python"
    generator = PythonGenerator


if __name__ == "__main__":
    unittest.main()
