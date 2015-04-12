import unittest
import timeit
from krpc.test.servertestcase import ServerTestCase

class TestPerformance(ServerTestCase, unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        super(TestPerformance, cls).setUpClass()

    @classmethod
    def tearDownClass(cls):
        super(TestPerformance, cls).tearDownClass()

    def setUp(self):
        super(TestPerformance, self).setUp()

    def test_performance(self):
        n = 100
        def wrapper():
            self.conn.test_service.float_to_string(float(3.14159))
        t = timeit.timeit(stmt=wrapper, number=n)
        print
        print 'Total execution time: %.2f seconds' % t
        print 'RPC execution rate: %d per second' % (n/t)
        print 'Latency: %.3f milliseconds' % ((t*1000)/n)

if __name__ == '__main__':
    unittest.main()
