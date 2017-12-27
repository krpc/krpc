import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.cpp import CppDomain


class TestDocGenCpp(DocGenTestCase, unittest.TestCase):
    language = 'cpp'
    domain = CppDomain


if __name__ == '__main__':
    unittest.main()
