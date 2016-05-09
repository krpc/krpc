import unittest
import krpctest
import krpc
import timeit

class TestFlight(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect()
        cls.control = cls.conn.space_center.active_vessel.control

    def test_performance(self):
        n = 1000
        def wrapper():
            self.control.throttle = 1
        times = [timeit.timeit(stmt=wrapper, number=1) for i in range(n)]
        print('Running %d RPCs' % n)
        print('Total execution time: %.2f seconds' % sum(times))
        print('Execution rate:       %d RPCs per second' % (n/sum(times)))
        print('Avg. execution time:  %.3f milliseconds per RPC' % ((sum(times)*1000)/n))
        print('Max. execution time:  %.3f milliseconds per RPC' % (max(times)*1000))
        print('Min. execution time:  %.3f milliseconds per RPC' % (min(times)*1000))

if __name__ == '__main__':
    unittest.main()
