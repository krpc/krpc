import socket
import threading
import time
import unittest
from unittest import mock

import krpc.error
import krpc.event
import krpc.schema.KRPC_pb2 as KRPC
from krpc.streammanager import StreamManager
from krpc.test.servertestcase import ServerTestCase


class TestEvent(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super().setUpClass()

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
        called.wait(1)
        self.assertGreater(time.time() - start_time, 0.15)
        self.assertLess(time.time() - start_time, 2)
        self.assertTrue(called.is_set())

    def test_event_callback_timeout(self) -> None:
        event = self.conn.test_service.on_timer(1000)
        called = threading.Event()
        event.add_callback(called.set)
        start_time = time.time()
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
        called1.wait(1)
        self.assertGreater(time.time() - start_time, 0.15)
        self.assertLess(time.time() - start_time, 2)
        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    def wait_for_firing(self, event: krpc.event.Event) -> None:
        deadline = time.monotonic() + 10
        while event._stream._stream.fired == 0:
            if time.monotonic() > deadline:
                self.fail("Timed out waiting for the event to fire")
            time.sleep(0.01)

    def test_callback_added_after_a_wait_is_not_called(self) -> None:
        # Fires a second time, so the event has not ended when the callback is added
        event = self.conn.test_service.on_timer(500, repeats=2)
        with event.condition:
            self.assertTrue(event.wait())
        called = []
        event.add_callback(lambda: called.append(True))
        self.assertEqual([], called)

    def test_callback_added_after_an_unseen_firing_is_called(self) -> None:
        event = self.conn.test_service.on_timer(500, repeats=2)
        self.wait_for_firing(event)
        called = []
        event.add_callback(lambda: called.append(True))
        self.assertEqual([True], called)

    def test_callback_added_after_a_delivered_firing_is_not_called(self) -> None:
        event = self.conn.test_service.on_timer(500, repeats=2)
        first = threading.Event()
        event.add_callback(first.set)
        self.assertTrue(first.wait(10))
        called = []
        event.add_callback(lambda: called.append(True))
        self.assertEqual([], called)

    def unissued_event(self, stream_id: int) -> krpc.event.Event:
        """An event over an id the server has not issued, so no update arrives for
        it."""
        message = KRPC.Event()
        message.stream.id = stream_id
        return krpc.event.Event(self.conn, message)

    def fire(self, stream_id: int) -> None:
        result = KRPC.StreamResult(id=stream_id)
        result.result.value = b"\x01"
        self.conn._stream_manager.update([result])

    def test_a_firing_passed_to_a_value_callback_is_replayed(self) -> None:
        event = self.unissued_event(4000000)
        values: list[object] = []
        event.stream.add_callback(values.append)
        self.fire(4000000)
        self.assertEqual([True], values)
        called = []
        event.add_callback(lambda: called.append(True))
        self.assertEqual([True], called)

    def test_a_callback_removed_before_its_replay_takes_no_firing(self) -> None:
        event = self.unissued_event(4000001)
        stream = event.stream._stream
        self.fire(4000001)
        removed: list[bool] = []
        call = stream.call

        def callback() -> None:
            removed.append(True)

        def remove_then_call(*args: object, **kwargs: object) -> None:
            event.remove_callback(callback)
            call(*args, **kwargs)  # type: ignore[arg-type]

        # Removed between the reservation of the firing and the replay
        with mock.patch.object(stream, "call", remove_then_call):
            event.add_callback(callback)
        self.assertEqual([], removed)
        called: list[bool] = []
        event.add_callback(lambda: called.append(True))
        self.assertEqual([True], called)

    def test_two_late_callbacks_do_not_both_replay_a_firing(self) -> None:
        event = self.unissued_event(4000002)
        stream = event.stream._stream
        self.fire(4000002)
        called: list[int] = []
        call = stream.call

        def add_then_call(*args: object, **kwargs: object) -> None:
            if not called:
                called.append(0)
                event.add_callback(lambda: called.append(2))
            call(*args, **kwargs)  # type: ignore[arg-type]

        # The second callback is added between the reservation of the firing for the
        # first callback and its replay
        with mock.patch.object(stream, "call", add_then_call):
            event.add_callback(lambda: called.append(1))
        self.assertEqual([0, 1], called)

    def test_a_removed_callback_does_not_take_a_firing(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.bool_type, 1)
        removed: list[bool] = []
        stream.add_event_callback(lambda: removed.append(True), 0)
        callback = stream.callbacks[0]
        stream.remove_callback(callback.fn)
        result = KRPC.StreamResult(id=1)
        result.result.value = b"\x01"
        manager.update([result])
        # Stands in for an update thread that copied the callback before its removal
        stream.call(callback, 1, True)
        called: list[bool] = []
        stream.add_event_callback(lambda: called.append(True), 0)
        self.assertEqual([], removed)
        self.assertEqual([True], called)

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

    def test_event_that_ends_without_firing(self) -> None:
        event = self.conn.test_service.remove_event_after(200)
        called = []
        ended = []
        done = threading.Event()

        def end_callback(ending: Exception) -> None:
            ended.append(ending)
            done.set()

        event.add_callback(lambda: called.append(True))
        event.add_end_callback(end_callback)
        with event.condition:
            with self.assertRaises(self.conn.krpc.StreamRemovedException):
                event.wait()
        self.assertTrue(done.wait(10))
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], self.conn.krpc.StreamRemovedException)
        self.assertEqual([], called)

    def test_firing_before_wait(self) -> None:
        event = self.conn.test_service.on_timer(100)
        time.sleep(0.5)
        with event.condition:
            start_time = time.time()
            self.assertTrue(event.wait(1))
            self.assertLess(time.time() - start_time, 0.1)

    def test_firings_coalesce(self) -> None:
        event = self.conn.test_service.on_timer(50, repeats=3)
        time.sleep(0.5)
        with event.condition:
            self.assertTrue(event.wait())
            with self.assertRaises(self.conn.krpc.StreamRemovedException):
                event.wait()

    def test_two_threads_wait_on_one_event(self) -> None:
        event = self.conn.test_service.on_timer(1000)
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

    def test_remove_wakes_an_event_wait(self) -> None:
        event = self.conn.test_service.on_timer(10000)
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

    def test_end_callback_on_close(self) -> None:
        conn = self.connect()
        event = conn.test_service.on_timer(10000)
        ended = []
        event.add_end_callback(ended.append)
        conn.close()
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], krpc.error.ConnectionError)

    def test_end_callback_on_connection_failure(self) -> None:
        conn = self.connect()
        try:
            event = conn.test_service.on_timer(10000)
            ended = []
            done = threading.Event()

            def end_callback(ending: Exception) -> None:
                ended.append(ending)
                done.set()

            event.add_end_callback(end_callback)
            # Stands in for the server closing the connection
            conn._stream_connection._socket.shutdown(socket.SHUT_RDWR)
            self.assertTrue(done.wait(10))
            self.assertEqual(1, len(ended))
            self.assertIsInstance(ended[0], krpc.error.ConnectionError)
        finally:
            conn.close()

    def test_connection_failure_wakes_an_event_wait(self) -> None:
        conn = self.connect()
        try:
            event = conn.test_service.on_timer(10000)
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
            # Stands in for the server closing the connection
            conn._stream_connection._socket.shutdown(socket.SHUT_RDWR)
            self.assertTrue(raised.wait(10))
        finally:
            conn.close()

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

        event = self.conn.add_event(expr)
        with event.condition:
            event.wait()
            # The counter advances on each evaluation and on this read, and the
            # server can evaluate the function again before the read
            self.assertGreaterEqual(
                self.conn.test_service.counter("TestEvent.test_custom_event"), 21
            )


if __name__ == "__main__":
    unittest.main()
