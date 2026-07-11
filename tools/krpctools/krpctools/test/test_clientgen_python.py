import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.python import PythonGenerator


class TestClientGenPython(ClientGenTestCase, unittest.TestCase):
    language = "python"
    generator = PythonGenerator


if __name__ == "__main__":
    unittest.main()
