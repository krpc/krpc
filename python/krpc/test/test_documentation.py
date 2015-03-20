#!/usr/bin/env python2

import unittest
import binascii
import krpc
import krpc.test.Test as TestSchema
from krpc.test.servertestcase import ServerTestCase

class TestClient(ServerTestCase, unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        super(TestClient, cls).setUpClass()

    @classmethod
    def tearDownClass(cls):
        super(TestClient, cls).tearDownClass()

    def setUp(self):
        super(TestClient, self).setUp()

    def test_basic(self):
        self.assertEqual('Service documentation string.', self.conn.test_service.__doc__)
        self.assertEqual('Procedure documentation string.', self.conn.test_service.float_to_string.__doc__)

if __name__ == '__main__':
    unittest.main()
