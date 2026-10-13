import unittest

from krpctools.docgen.cnano import CnanoDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenCnano(DocGenTestCase, unittest.TestCase):
    language = "cnano"
    domain = CnanoDomain


if __name__ == "__main__":
    unittest.main()
