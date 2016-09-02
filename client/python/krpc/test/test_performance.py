from __future__ import print_function
import unittest
import timeit
from krpc.test.servertestcase import ServerTestCase


class TestPerformance(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super(TestPerformance, cls).setUpClass()

    def test_performance(self):
        samples = 100

        def wrapper():
            self.conn.test_service.float_to_string(float(3.14159))

        delta_t = timeit.timeit(stmt=wrapper, number=samples)
        print()
        print('Total execution time: %.2f seconds' % delta_t)
        print('RPC execution rate: %d per second' % (samples/delta_t))
        print('Latency: %.3f milliseconds' % ((delta_t*1000)/samples))

if __name__ == '__main__':
    unittest.main()
