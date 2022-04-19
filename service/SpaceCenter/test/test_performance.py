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
        print(('Running', samples, 'RPCs'))
        print(('Total execution time:',
              sum(times), 'seconds'))
        print(('Execution rate:      ',
              samples/sum(times), 'RPCs per second'))
        print(('Avg. execution time: ',
              (sum(times)*1000)/samples, 'milliseconds per RPC'))
        print(('Max. execution time: ',
              max(times)*1000, 'milliseconds per RPC'))
        print(('Min. execution time: ',
              min(times)*1000, 'milliseconds per RPC'))


if __name__ == '__main__':
    unittest.main()
