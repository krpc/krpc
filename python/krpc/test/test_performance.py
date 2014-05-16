#!/usr/bin/env python2

import unittest
import subprocess
import time
import timeit
import krpc

class TestClient(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50001'])
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestClient', port=50001)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_basic(self):
        def wrapper():
            self.ksp.test_service.float_to_string(float(3.14159))
        self.assertGreater(1, timeit.timeit(stmt=wrapper, number=1000))

if __name__ == '__main__':
    unittest.main()
