import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.cnano import CnanoDomain


class TestDocGenCnano(DocGenTestCase, unittest.TestCase):
    language = 'cnano'
    domain = CnanoDomain


if __name__ == '__main__':
    unittest.main()
