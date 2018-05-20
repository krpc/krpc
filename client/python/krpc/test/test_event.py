import time
import threading
import unittest
from krpc.test.servertestcase import ServerTestCase


class TestEvent(ServerTestCase, unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        super(TestEvent, cls).setUpClass()

    def test_event(self):
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait()
            self.assertAlmostEqual(time.time()-start_time, 0.2, delta=0.05)
            self.assertTrue(event.stream())

    def test_event_using_lambda(self):
        event = self.conn.test_service.on_timer_using_lambda(200)
        with event.condition:
            start_time = time.time()
            event.wait()
            self.assertAlmostEqual(time.time()-start_time, 0.2, delta=0.05)
            self.assertTrue(event.stream())

    def test_event_timeout_short(self):
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait(0.1)
            self.assertLess(time.time()-start_time, 0.2)
            self.assertAlmostEqual(time.time()-start_time, 0.1, delta=0.05)
            self.assertFalse(event.stream())
            event.wait()
            self.assertTrue(event.stream())

    def test_event_timeout_long(self):
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait(1)
            self.assertAlmostEqual(time.time()-start_time, 0.2, delta=0.05)
            self.assertTrue(event.stream())

    def test_event_loop(self):
        start_time = time.time()
        event = self.conn.test_service.on_timer(200, repeats=5)
        with event.condition:
            repeat = 0
            while True:
                event.wait()
                self.assertTrue(event.stream())
                repeat += 1
                self.assertAlmostEqual(
                    time.time()-start_time, 0.2*repeat, delta=0.05)
                if repeat == 5:
                    break

    def test_event_callback(self):
        event = self.conn.test_service.on_timer(200)
        called = threading.Event()
        event.add_callback(called.set)
        start_time = time.time()
        event.start()
        called.wait(1)
        self.assertAlmostEqual(time.time()-start_time, 0.2, delta=0.05)
        self.assertTrue(called.is_set())

    def test_event_callback_timeout(self):
        event = self.conn.test_service.on_timer(1000)
        called = threading.Event()
        event.add_callback(called.set)
        start_time = time.time()
        event.start()
        called.wait(0.1)
        self.assertAlmostEqual(time.time()-start_time, 0.1, delta=0.05)
        self.assertFalse(called.is_set())

    test_event_callback_loop_count = 0

    def test_event_callback_loop(self):
        event = self.conn.test_service.on_timer(200, repeats=5)

        def callback():
            self.test_event_callback_loop_count += 1

        event.add_callback(callback)
        start_time = time.time()
        event.start()
        while self.test_event_callback_loop_count < 5:
            time.sleep(0.1)
        self.assertGreater(time.time()-start_time, 0.95)
        self.assertEqual(self.test_event_callback_loop_count, 5)

    def test_event_remove_callback(self):
        event = self.conn.test_service.on_timer(200)
        called1 = threading.Event()
        called2 = threading.Event()
        event.add_callback(called1.set)
        event.add_callback(called2.set)
        event.remove_callback(called2.set)
        start_time = time.time()
        event.start()
        called1.wait(1)
        self.assertAlmostEqual(time.time()-start_time, 0.2, delta=0.05)
        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    def test_custom_event(self):
        expression = self.conn.krpc.Expression

        counter = expression.call(
            self.conn.get_call(
                self.conn.test_service.counter,
                "TestEvent.test_custom_event"))
        expr = expression.equal(
            expression.multiply(
                expression.constant_int(2),
                expression.constant_int(10)),
            counter)

        event = self.conn.krpc.add_event(expr)
        with event.condition:
            event.wait()
            self.assertEqual(
                self.conn.test_service.counter(
                    "TestEvent.test_custom_event"),
                21)


if __name__ == '__main__':
    unittest.main()
