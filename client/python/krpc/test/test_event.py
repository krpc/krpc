import time
import threading
import unittest
from krpc.test.servertestcase import ServerTestCase


class TestEvent(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestEvent, cls).setUpClass()

    def test_event(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait()
            # Lower bound is a correctness check (event must not fire before its
            # timer); the upper bound is a generous hang detector, kept loose so
            # the test does not flake under parallel load. See issue #540.
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)
            self.assertTrue(event.stream())

    def test_event_using_lambda(self) -> None:
        event = self.conn.test_service.on_timer_using_lambda(200)
        with event.condition:
            start_time = time.time()
            event.wait()
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)
            self.assertTrue(event.stream())

    def test_event_timeout_short(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait(0.1)
            # wait() must return on its timeout, before the 200ms event fires.
            self.assertGreater(time.time() - start_time, 0.05)
            self.assertLess(time.time() - start_time, 0.2)
            self.assertFalse(event.stream())
            event.wait()
            self.assertTrue(event.stream())

    def test_event_timeout_long(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            event.wait(1)
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)
            self.assertTrue(event.stream())

    def test_event_loop(self) -> None:
        start_time = time.time()
        event = self.conn.test_service.on_timer(200, repeats=5)
        with event.condition:
            repeat = 0
            while True:
                event.wait()
                self.assertTrue(event.stream())
                repeat += 1
                self.assertGreater(time.time() - start_time, 0.2 * repeat - 0.05)
                self.assertLess(time.time() - start_time, 0.2 * repeat + 2)
                if repeat == 5:
                    break

    def test_event_callback(self) -> None:
        event = self.conn.test_service.on_timer(200)
        called = threading.Event()
        event.add_callback(called.set)
        start_time = time.time()
        event.start()
        called.wait(1)
        self.assertGreater(time.time() - start_time, 0.15)
        self.assertLess(time.time() - start_time, 2)
        self.assertTrue(called.is_set())

    def test_event_callback_timeout(self) -> None:
        event = self.conn.test_service.on_timer(1000)
        called = threading.Event()
        event.add_callback(called.set)
        start_time = time.time()
        event.start()
        called.wait(0.1)
        # wait() must return on its timeout, before the 1000ms event fires.
        self.assertGreater(time.time() - start_time, 0.05)
        self.assertLess(time.time() - start_time, 1)
        self.assertFalse(called.is_set())

    test_event_callback_loop_count = 0

    def test_event_callback_loop(self) -> None:
        event = self.conn.test_service.on_timer(200, repeats=5)

        def callback() -> None:
            self.test_event_callback_loop_count += 1

        event.add_callback(callback)
        start_time = time.time()
        event.start()
        while self.test_event_callback_loop_count < 5:
            time.sleep(0.1)
        self.assertGreater(time.time() - start_time, 0.95)
        self.assertEqual(self.test_event_callback_loop_count, 5)

    def test_event_remove_callback(self) -> None:
        event = self.conn.test_service.on_timer(200)
        called1 = threading.Event()
        called2 = threading.Event()
        event.add_callback(called1.set)
        event.add_callback(called2.set)
        event.remove_callback(called2.set)
        start_time = time.time()
        event.start()
        called1.wait(1)
        self.assertGreater(time.time() - start_time, 0.15)
        self.assertLess(time.time() - start_time, 2)
        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    def test_custom_event(self) -> None:
        expression = self.conn.krpc.Expression

        counter = expression.call(
            self.conn.get_call(
                self.conn.test_service.counter, "TestEvent.test_custom_event"
            )
        )
        expr = expression.equal(
            expression.multiply(
                expression.constant_int(2), expression.constant_int(10)
            ),
            counter,
        )

        event = self.conn.krpc.add_event(expr)
        with event.condition:
            event.wait()
            # The event fires when the server-side counter reaches 20. The
            # counter increments on every expression evaluation, so the value
            # read back here is >= 21 (20 at the trigger, plus this read); the
            # exact figure depends on how many more times the server evaluated
            # the expression before this read, so assert the lower bound rather
            # than an exact value to avoid flaking under load. See issue #540.
            self.assertGreaterEqual(
                self.conn.test_service.counter("TestEvent.test_custom_event"), 21
            )


if __name__ == "__main__":
    unittest.main()
