import unittest

from krpctools.docgen.http import HttpDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenHttp(DocGenTestCase, unittest.TestCase):
    language = "http"
    domain = HttpDomain
    maxDiff = None


if __name__ == "__main__":
    unittest.main()
