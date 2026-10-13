import unittest

from krpctools.docgen.cpp import CppDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenCpp(DocGenTestCase, unittest.TestCase):
    language = "cpp"
    domain = CppDomain


if __name__ == "__main__":
    unittest.main()
