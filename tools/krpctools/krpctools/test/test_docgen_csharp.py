import unittest

from krpctools.docgen.csharp import CsharpDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenCsharp(DocGenTestCase, unittest.TestCase):
    language = "csharp"
    domain = CsharpDomain


if __name__ == "__main__":
    unittest.main()
