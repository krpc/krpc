import unittest

from krpctools.docgen.python import PythonDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenPython(DocGenTestCase, unittest.TestCase):
    language = "python"
    domain = PythonDomain


if __name__ == "__main__":
    unittest.main()
