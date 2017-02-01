import unittest
from krpctest import TestCase


class TestTestCase(TestCase):

    def check_fails(self, fn, *args, **kwargs):
        failed = False
        try:
            fn(*args, **kwargs)
        except AssertionError:
            failed = True
        if not failed:
            self.fail('Expected test case to fail, but it passed')

    def test_almost_equals(self):
        self.assertAlmostEqual(0, 0, delta=0)
        self.assertAlmostEqual(0, 1, delta=1)
        self.check_fails(self.assertAlmostEqual, 0, 1, delta=0.5)
        self.check_fails(self.assertAlmostEqual, 0, 1, delta=0)

    def test_not_almost_equal(self):
        self.check_fails(self.assertNotAlmostEqual, 0, 0, delta=0)
        self.check_fails(self.assertNotAlmostEqual, 0, 1, delta=1)
        self.assertNotAlmostEqual(0, 1, delta=0.5)
        self.assertNotAlmostEqual(0, 1, delta=0)

    def test_almost_equal_iterable(self):
        self.assertAlmostEqual((1, 2, 3), (1, 2, 3), delta=0)
        self.assertAlmostEqual((1, 2, 3), [1, 2, 3], delta=0)
        self.assertAlmostEqual([1, 2, 3], (1, 2, 3), delta=0)
        self.assertAlmostEqual([1, 2, 3], [1, 2, 3], delta=0)
        self.assertAlmostEqual((1, 2, 3), (2, 3, 4), delta=1)
        self.check_fails(self.assertAlmostEqual, (1, 2, 3), (3, 3, 4), delta=1)
        self.check_fails(self.assertAlmostEqual, (1, 2, 3), (2, 4, 4), delta=1)
        self.check_fails(self.assertAlmostEqual, (1, 2, 3), (2, 3, 5), delta=1)
        self.check_fails(self.assertAlmostEqual, (1, 2, 3), (1, 2), delta=1)
        self.check_fails(self.assertAlmostEqual, (1, 2), (1, 2, 3), delta=1)

    def test_not_almost_equal_iterable(self):
        self.check_fails(self.assertNotAlmostEqual,
                         (1, 2, 3), (1, 2, 3), delta=0)
        self.assertNotAlmostEqual((1, 2, 3), (3, 3, 4), delta=1)

    def test_almost_equal_dict(self):
        self.assertAlmostEqual(
            {'foo': 1, 'bar': 2}, {'foo': 1, 'bar': 2}, delta=0)
        self.assertAlmostEqual(
            {'foo': 1, 'bar': 3}, {'foo': 1, 'bar': 2}, delta=2)

    def test_not_almost_equal_dict(self):
        self.check_fails(self.assertAlmostEqual,
                         {'foo': 1, 'bar': 2}, {'baz': 3}, delta=0)
        self.assertNotAlmostEqual(
            {'foo': 1, 'bar': 2}, {'foo': 1, 'bar': 4}, delta=1)

    def test_degrees_almost_equal(self):
        cases = [
            (0, 0, 0),
            (43, 44, 1),
            (44, 43, 1),
            (360, 0, 0),
            (0, 360, 0),
            (360, 360, 0),
            (0, 1, 1),
            (1, 0, 1),
            (0, 359, 1),
            (359, 0, 1)
        ]
        for case in cases:
            self.assertDegreesAlmostEqual(case[0], case[1], delta=case[2])
        fail_cases = [
            (0, 2, 1),
            (42, 44, 1),
            (44, 42, 1),
            (0, 358, 1),
            (358, 0, 1)
        ]
        for case in fail_cases:
            self.check_fails(self.assertDegreesAlmostEqual,
                             case[0], case[1], delta=case[2])


if __name__ == '__main__':
    unittest.main()
