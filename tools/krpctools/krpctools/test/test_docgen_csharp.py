import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.csharp import CsharpDomain


class TestDocGenCsharp(DocGenTestCase, unittest.TestCase):
    language = 'csharp'
    domain = CsharpDomain


if __name__ == '__main__':
    unittest.main()
