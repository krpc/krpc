import unittest
import timeit
import krpctest


class TestPerformance(krpctest.TestCase):

    def test_performance(self):
        control = self.connect().space_center.active_vessel.control
        samples = 1000

        def wrapper():
            control.throttle = 1

        times = [timeit.timeit(stmt=wrapper, number=1) for _ in range(samples)]
        print 'Running %d RPCs' % samples
        print 'Total execution time: %.2f seconds' % sum(times)
        print 'Execution rate:       %d RPCs per second' % (samples/sum(times))
        print 'Avg. execution time:  %.3f milliseconds per RPC' % \
            ((sum(times)*1000)/samples)
        print 'Max. execution time:  %.3f milliseconds per RPC' % \
            (max(times)*1000)
        print 'Min. execution time:  %.3f milliseconds per RPC' % \
            (min(times)*1000)


if __name__ == '__main__':
    unittest.main()
