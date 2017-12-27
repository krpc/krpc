import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.python import PythonDomain


class TestDocGenPython(DocGenTestCase, unittest.TestCase):
    language = 'python'
    domain = PythonDomain


if __name__ == '__main__':
    unittest.main()
