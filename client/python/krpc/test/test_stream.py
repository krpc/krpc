import unittest
import time
from krpc.test.servertestcase import ServerTestCase


class TestStream(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super(TestStream, cls).setUpClass()

    @staticmethod
    def wait():
        time.sleep(0.01)

    def test_method(self):
        with self.conn.stream(self.conn.test_service.float_to_string, 3.14159) as x:
            for _ in range(5):
                self.assertEqual('3.14159', x())
                self.wait()

    def test_property(self):
        self.conn.test_service.string_property = 'foo'
        with self.conn.stream(getattr, self.conn.test_service, 'string_property') as x:
            for _ in range(5):
                self.assertEqual('foo', x())
                self.wait()

    def test_class_method(self):
        obj = self.conn.test_service.create_test_object('bob')
        with self.conn.stream(obj.float_to_string, 3.14159) as x:
            for _ in range(5):
                self.assertEqual('bob3.14159', x())
                self.wait()

    def test_class_static_method(self):
        with self.conn.stream(self.conn.test_service.TestClass.static_method, 'foo') as x:
            for _ in range(5):
                self.assertEqual('jebfoo', x())
                self.wait()

    def test_class_property(self):
        obj = self.conn.test_service.create_test_object('jeb')
        obj.int_property = 42
        with self.conn.stream(getattr, obj, 'int_property') as x:
            for _ in range(5):
                self.assertEqual(42, x())
                self.wait()

    def test_property_setters_are_invalid(self):
        self.assertRaises(ValueError, self.conn.add_stream, setattr, self.conn.test_service, 'string_property')
        obj = self.conn.test_service.create_test_object('bill')
        self.assertRaises(ValueError, self.conn.add_stream, setattr, obj.int_property, 42)

    def test_counter(self):
        count = -1
        with self.conn.stream(self.conn.test_service.counter) as x:
            for _ in range(5):
                self.assertLess(count, x())
                count = x()
                i = 0
                while count == x():
                    self.wait()
                    if i > 1000:
                        self.fail('Timed out waiting for stream to update')
                    i += 1

    def test_nested(self):
        with self.conn.stream(self.conn.test_service.float_to_string, 0.123) as x0:
            with self.conn.stream(self.conn.test_service.float_to_string, 1.234) as x1:
                for _ in range(5):
                    self.assertEqual('0.123', x0())
                    self.assertEqual('1.234', x1())
                    self.wait()

    def test_interleaved(self):
        s0 = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual('0', s0())

        self.wait()
        self.assertEqual('0', s0())

        s1 = self.conn.add_stream(self.conn.test_service.int32_to_string, 1)
        self.assertEqual('0', s0())
        self.assertEqual('1', s1())

        self.wait()
        self.assertEqual('0', s0())
        self.assertEqual('1', s1())

        s1.remove()
        self.assertEqual('0', s0())
        self.assertRaises(RuntimeError, s1)

        self.wait()
        self.assertEqual('0', s0())
        self.assertRaises(RuntimeError, s1)

        s2 = self.conn.add_stream(self.conn.test_service.int32_to_string, 2)
        self.assertEqual('0', s0())
        self.assertRaises(RuntimeError, s1)
        self.assertEqual('2', s2())

        self.wait()
        self.assertEqual('0', s0())
        self.assertRaises(RuntimeError, s1)
        self.assertEqual('2', s2())

        s0.remove()
        self.assertRaises(RuntimeError, s0)
        self.assertRaises(RuntimeError, s1)
        self.assertEqual('2', s2())

        self.wait()
        self.assertRaises(RuntimeError, s0)
        self.assertRaises(RuntimeError, s1)
        self.assertEqual('2', s2())

        s2.remove()
        self.assertRaises(RuntimeError, s0)
        self.assertRaises(RuntimeError, s1)
        self.assertRaises(RuntimeError, s2)

        self.wait()
        self.assertRaises(RuntimeError, s0)
        self.assertRaises(RuntimeError, s1)
        self.assertRaises(RuntimeError, s2)

    def test_remove_stream_twice(self):
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual('0', stream())

        self.wait()
        self.assertEqual('0', stream())

        stream.remove()
        self.assertRaises(RuntimeError, stream)
        stream.remove()
        self.assertRaises(RuntimeError, stream)

    def test_add_stream_twice(self):
        s0 = self.conn.add_stream(self.conn.test_service.int32_to_string, 42)
        stream_id = s0._stream_id
        self.assertEqual('42', s0())
        self.wait()
        self.assertEqual('42', s0())

        s1 = self.conn.add_stream(self.conn.test_service.int32_to_string, 42)
        self.assertEqual(stream_id, s1._stream_id)
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())
        self.wait()
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())

if __name__ == '__main__':
    unittest.main()
