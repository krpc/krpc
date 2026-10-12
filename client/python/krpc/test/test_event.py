import threading
import time
import unittest

import krpc.error

from krpc.test.servertestcase import ServerTestCase


class TestEvent(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestEvent, cls).setUpClass()

    def test_event(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            self.assertTrue(event.wait())
            # Lower bound is a correctness check (event must not fire before its
            # timer); the upper bound is a generous hang detector, kept loose so
            # the test does not flake under parallel load.
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)

    def test_event_using_lambda(self) -> None:
        event = self.conn.test_service.on_timer_using_lambda(200)
        with event.condition:
            start_time = time.time()
            self.assertTrue(event.wait())
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)

    def test_event_timeout_short(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            self.assertFalse(event.wait(0.1))
            # wait() must return on its timeout, before the 200ms event fires.
            self.assertGreater(time.time() - start_time, 0.05)
            self.assertLess(time.time() - start_time, 0.2)
            self.assertTrue(event.wait())

    def test_event_timeout_long(self) -> None:
        event = self.conn.test_service.on_timer(200)
        with event.condition:
            start_time = time.time()
            self.assertTrue(event.wait(1))
            self.assertGreater(time.time() - start_time, 0.15)
            self.assertLess(time.time() - start_time, 2)

    def test_event_loop(self) -> None:
        start_time = time.time()
        event = self.conn.test_service.on_timer(200, repeats=5)
        with event.condition:
            repeat = 0
            while True:
                self.assertTrue(event.wait())
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
            # than an exact value to avoid flaking under load.
            self.assertGreaterEqual(
                self.conn.test_service.counter("TestEvent.test_custom_event"), 21
            )

    def test_server_removed_event(self) -> None:
        event = self.conn.test_service.on_timer(100)
        ended = []
        event.add_end_callback(ended.append)
        with event.condition:
            self.assertTrue(event.wait())
            with self.assertRaises(self.conn.krpc.StreamRemovedException):
                event.wait()
            with self.assertRaises(self.conn.krpc.StreamRemovedException):
                event.wait(0.1)
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], self.conn.krpc.StreamRemovedException)

    def test_firing_before_wait(self) -> None:
        event = self.conn.test_service.on_timer(100)
        event.start()
        time.sleep(0.5)
        with event.condition:
            start_time = time.time()
            self.assertTrue(event.wait(1))
            self.assertLess(time.time() - start_time, 0.1)

    def test_firings_coalesce(self) -> None:
        event = self.conn.test_service.on_timer(50, repeats=3)
        event.start()
        time.sleep(0.5)
        with event.condition:
            self.assertTrue(event.wait())
            with self.assertRaises(self.conn.krpc.StreamRemovedException):
                event.wait()

    def test_two_threads_wait_on_one_event(self) -> None:
        event = self.conn.test_service.on_timer(1000)
        event.start()
        woke = []

        def wait() -> None:
            with event.condition:
                if event.wait(5):
                    woke.append(True)

        threads = [threading.Thread(target=wait, daemon=True) for _ in range(2)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join(10)
        self.assertEqual([True, True], woke)

    def test_error_ends_the_event(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.equal(
            expression.call(
                self.conn.get_call(
                    self.conn.test_service.throw_invalid_operation_exception
                )
            ),
            expression.constant_int(0),
        )
        event = self.conn.krpc.add_event(expr)
        with event.condition:
            with self.assertRaises(RuntimeError) as cm:
                event.wait()
            self.assertTrue(str(cm.exception).startswith("Invalid operation"))
            self.assertRaises(RuntimeError, event.wait)

    def test_remove_wakes_an_event_wait(self) -> None:
        event = self.conn.test_service.on_timer(10000)
        event.start()
        raised = threading.Event()

        def wait() -> None:
            with event.condition:
                try:
                    event.wait()
                except krpc.error.StreamError:
                    raised.set()

        thread = threading.Thread(target=wait, daemon=True)
        thread.start()
        time.sleep(0.2)
        event.remove()
        self.assertTrue(raised.wait(10))

    def test_close_wakes_an_event_wait(self) -> None:
        conn = self.connect()
        event = conn.test_service.on_timer(10000)
        event.start()
        raised = threading.Event()

        def wait() -> None:
            with event.condition:
                try:
                    event.wait()
                except krpc.error.ConnectionError:
                    raised.set()

        thread = threading.Thread(target=wait, daemon=True)
        thread.start()
        time.sleep(0.2)
        conn.close()
        self.assertTrue(raised.wait(10))


if __name__ == "__main__":
    unittest.main()
