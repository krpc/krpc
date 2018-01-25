import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.cnano import CnanoGenerator


class TestClientGenCNano(ClientGenTestCase, unittest.TestCase):
    language = 'cnano'
    generator = CnanoGenerator


if __name__ == '__main__':
    unittest.main()
