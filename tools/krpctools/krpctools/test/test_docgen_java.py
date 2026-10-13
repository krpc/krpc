import unittest

from krpctools.docgen.java import JavaDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenJava(DocGenTestCase, unittest.TestCase):
    language = "java"
    domain = JavaDomain


if __name__ == "__main__":
    unittest.main()
