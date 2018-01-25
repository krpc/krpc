import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.java import JavaDomain


class TestDocGenJava(DocGenTestCase, unittest.TestCase):
    language = 'java'
    domain = JavaDomain


if __name__ == '__main__':
    unittest.main()
