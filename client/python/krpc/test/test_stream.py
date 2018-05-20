import unittest
import threading
import time
from krpc.error import StreamError
from krpc.test.servertestcase import ServerTestCase


class TestStream(ServerTestCase, unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        super(TestStream, cls).setUpClass()

    @staticmethod
    def wait():
        time.sleep(0.01)

    def test_method(self):
        with self.conn.stream(self.conn.test_service.float_to_string,
                              3.14159) as x:
            for _ in range(5):
                self.assertEqual('3.14159', x())
                self.wait()

    def test_property(self):
        self.conn.test_service.string_property = 'foo'
        with self.conn.stream(getattr, self.conn.test_service,
                              'string_property') as x:
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
        with self.conn.stream(self.conn.test_service.TestClass.static_method,
                              'foo') as x:
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
        self.assertRaises(StreamError, self.conn.add_stream,
                          setattr, self.conn.test_service, 'string_property')
        obj = self.conn.test_service.create_test_object('bill')
        self.assertRaises(StreamError, self.conn.add_stream,
                          setattr, obj.int_property, 42)

    def test_counter(self):
        count = -1
        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_counter") as x:
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
        with self.conn.stream(self.conn.test_service.float_to_string,
                              0.123) as x0:
            with self.conn.stream(self.conn.test_service.float_to_string,
                                  1.234) as x1:
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
        self.assertRaises(StreamError, s1)

        self.wait()
        self.assertEqual('0', s0())
        self.assertRaises(StreamError, s1)

        s2 = self.conn.add_stream(self.conn.test_service.int32_to_string, 2)
        self.assertEqual('0', s0())
        self.assertRaises(StreamError, s1)
        self.assertEqual('2', s2())

        self.wait()
        self.assertEqual('0', s0())
        self.assertRaises(StreamError, s1)
        self.assertEqual('2', s2())

        s0.remove()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertEqual('2', s2())

        self.wait()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertEqual('2', s2())

        s2.remove()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertRaises(StreamError, s2)

        self.wait()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertRaises(StreamError, s2)

    def test_remove_stream_twice(self):
        stream = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 0)
        self.assertEqual('0', stream())

        self.wait()
        self.assertEqual('0', stream())

        stream.remove()
        self.assertRaises(StreamError, stream)
        stream.remove()
        self.assertRaises(StreamError, stream)

    def test_add_stream_twice(self):
        s0 = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 42)
        stream_impl = s0._stream
        self.assertEqual('42', s0())
        self.wait()
        self.assertEqual('42', s0())

        s1 = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 42)
        self.assertEqual(stream_impl, s1._stream)
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())
        self.wait()
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())

        s2 = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 43)
        self.assertNotEqual(stream_impl, s2._stream)
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())
        self.assertEqual('43', s2())
        self.wait()
        self.assertEqual('42', s0())
        self.assertEqual('42', s1())
        self.assertEqual('43', s2())

    def test_remove_then_add_stream(self):
        stream = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 0)
        self.assertEqual('0', stream())
        self.wait()
        self.assertEqual('0', stream())
        stream.remove()
        stream = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 0)
        self.assertEqual('0', stream())

    def test_restart_stream(self):
        stream = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 0)
        stream.start()
        stream.remove()
        stream = self.conn.add_stream(
            self.conn.test_service.int32_to_string, 0)
        stream.start()

    def test_invalid_operation_exception_immediately(self):
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception)
        with self.assertRaises(RuntimeError) as cm:
            stream()
        self.assertTrue(str(cm.exception).startswith('Invalid operation'))

    def test_invalid_operation_exception_later(self):
        self.conn.test_service.reset_invalid_operation_exception_later()
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception_later)
        self.assertEqual(0, stream())
        with self.assertRaises(RuntimeError) as cm:
            while True:
                self.wait()
                stream()
        self.assertTrue(
            str(cm.exception).startswith('Invalid operation'))

    def test_custom_exception_immediately(self):
        stream = self.conn.add_stream(
            self.conn.test_service.throw_custom_exception)
        with self.assertRaises(RuntimeError) as cm:
            stream()
        self.assertTrue(
            str(cm.exception).startswith('A custom kRPC exception'))

    def test_custom_exception_later(self):
        self.conn.test_service.reset_custom_exception_later()
        stream = self.conn.add_stream(
            self.conn.test_service.throw_custom_exception_later)
        self.assertEqual(0, stream())
        with self.assertRaises(RuntimeError) as cm:
            while True:
                self.wait()
                stream()
        self.assertTrue(
            str(cm.exception).startswith('A custom kRPC exception'))

    def test_yield_exception(self):
        stream = self.conn.add_stream(
            self.conn.test_service.blocking_procedure, 10)
        for _ in range(100):
            self.assertEqual(55, stream())
            self.wait()

    def test_wait(self):
        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_wait", 10) as x:
            with x.condition:
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    x.wait()
                    count += 1
                    self.assertEqual(count, x())

    def test_wait_timeout_short(self):
        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_wait_timeout_short", 10) as x:
            with x.condition:
                count = x()
                x.wait(timeout=0)
                self.assertEqual(count, x())

    def test_wait_timeout_long(self):
        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_wait_timeout_long", 10) as x:
            with x.condition:
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    x.wait(timeout=10)
                    count += 1
                    self.assertEqual(count, x())

    test_callback_value = -1

    def test_callback(self):
        error = threading.Event()
        stop = threading.Event()

        def callback(x):
            if x > 5:
                stop.set()
            elif self.test_callback_value+1 != x:
                error.set()
                stop.set()
            else:
                self.test_callback_value += 1

        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_callback", 10) as x:
            x.add_callback(callback)
            x.start()
            stop.wait(3)

        self.assertTrue(stop.is_set())
        self.assertFalse(error.is_set())
        self.assertEquals(self.test_callback_value, 5)

    def test_remove_callback(self):
        called1 = threading.Event()
        called2 = threading.Event()

        def callback1(x):  # pylint: disable=unused-argument
            called1.set()

        def callback2(x):  # pylint: disable=unused-argument
            called2.set()

        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_remove_callback", 10) as x:
            x.add_callback(callback1)
            x.add_callback(callback2)
            x.remove_callback(callback2)
            x.start()
            called1.wait(3)

        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    test_rate_value = 0

    def test_rate(self):
        error = threading.Event()
        stop = threading.Event()

        def callback(x):
            if x > 5:
                stop.set()
            elif self.test_rate_value+1 != x:
                error.set()
                stop.set()
            else:
                self.test_rate_value += 1

        with self.conn.stream(self.conn.test_service.counter,
                              "TestStream.test_rate") as x:
            x.add_callback(callback)
            x.rate = 5
            x.start()
            start = time.time()
            stop.wait(3)
            elapsed = time.time() - start

        self.assertGreater(elapsed, 1)
        self.assertLess(elapsed, 1.2)
        self.assertTrue(stop.is_set())
        self.assertFalse(error.is_set())
        self.assertEquals(self.test_rate_value, 5)


if __name__ == '__main__':
    unittest.main()
