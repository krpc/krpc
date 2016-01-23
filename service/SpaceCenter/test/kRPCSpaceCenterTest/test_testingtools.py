import unittest
import testingtools

class TestTestingTools(testingtools.TestCase):

    def checkFails(self, f, *args):
        failed = False
        try:
            f(*args)
        except AssertionError as e:
            failed = True
        if not failed:
            self.fail('Expected test case to fail, but it passed')

    def test_close(self):
        self.assertClose(0, 0, 0)
        self.assertClose(0, 1, 1)
        self.checkFails(self.assertClose, 0, 1, 0.5)
        self.checkFails(self.assertClose, 0, 1, 0)

    def test_not_close(self):
        self.checkFails(self.assertNotClose, 0, 0, 0)
        self.checkFails(self.assertNotClose, 0, 1, 1)
        self.assertNotClose(0, 1, 0.5)
        self.assertNotClose(0, 1, 0)

    def test_close_iterable(self):
        self.assertClose((1,2,3), (1,2,3), 0)
        self.assertClose((1,2,3), [1,2,3], 0)
        self.assertClose([1,2,3], (1,2,3), 0)
        self.assertClose([1,2,3], [1,2,3], 0)
        self.assertClose((1,2,3), (2,3,4), 1)
        self.checkFails(self.assertClose, (1,2,3), (3,3,4), 1)
        self.checkFails(self.assertClose, (1,2,3), (2,4,4), 1)
        self.checkFails(self.assertClose, (1,2,3), (2,3,5), 1)
        self.checkFails(self.assertClose, (1,2,3), (1,2), 1)
        self.checkFails(self.assertClose, (1,2), (1,2,3), 1)

    def test_not_close_iterable(self):
        self.checkFails(self.assertNotClose, (1,2,3), (1,2,3), 0)
        self.assertNotClose((1,2,3), (3,3,4), 1)

    def test_close_dict(self):
        self.assertClose({'foo': 1, 'bar': 2}, {'foo': 1, 'bar': 2}, 0)
        self.assertClose({'foo': 1, 'bar': 3}, {'foo': 1, 'bar': 2}, 2)

    def test_not_close_dict(self):
        self.checkFails(self.assertClose, {'foo': 1, 'bar': 2}, {'baz': 3}, 0)
        self.assertNotClose({'foo': 1, 'bar': 2}, {'foo': 1, 'bar': 4}, 1)

    def test_close_degrees(self):
        self.assertCloseDegrees(0, 0, 0)
        self.assertCloseDegrees(43, 44, 1)
        self.assertCloseDegrees(44, 43, 1)
        self.assertCloseDegrees(360, 0, 0)
        self.assertCloseDegrees(0, 360, 0)
        self.assertCloseDegrees(360, 360, 0)
        self.assertCloseDegrees(0, 1, 1)
        self.assertCloseDegrees(1, 0, 1)
        self.assertCloseDegrees(0, 359, 1)
        self.assertCloseDegrees(359, 0, 1)
        self.checkFails(self.assertCloseDegrees, 0, 2, 1)
        self.checkFails(self.assertCloseDegrees, 42, 44, 1)
        self.checkFails(self.assertCloseDegrees, 44, 42, 1)
        self.checkFails(self.assertCloseDegrees, 0, 358, 1)
        self.checkFails(self.assertCloseDegrees, 358, 0, 1)

if __name__ == "__main__":
    unittest.main()
