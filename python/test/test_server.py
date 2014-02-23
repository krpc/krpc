#!/usr/bin/env python2

import unittest
import binascii
import krpc

class TestServer(unittest.TestCase):

    def test_connect(self):
        ksp = krpc.connect(name='TestServer')
        self.assertEqual('42', ksp.TestService.Int32ToString(42))

if __name__ == '__main__':
    unittest.main()
