#!/usr/bin/env python2

import unittest
import subprocess
import time
import timeit
import krpc

class TestClient(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50001', '50002'])
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestClient', rpc_port=50001, stream_port=50002)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_basic(self):
        n = 1000
        def wrapper():
            self.ksp.test_service.float_to_string(float(3.14159))
        t = timeit.timeit(stmt=wrapper, number=n)
        print 'Total execution time: %.2f seconds' % t
        print 'RPC execution rate: %d per second' % (n/t)
        print 'Latency: %.3f milliseconds' % ((t*1000)/n)

if __name__ == '__main__':
    unittest.main()
