import unittest

from krpctools.docgen.js import JsDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenJs(DocGenTestCase, unittest.TestCase):
    language = "js"
    domain = JsDomain
    maxDiff = None


if __name__ == "__main__":
    unittest.main()
