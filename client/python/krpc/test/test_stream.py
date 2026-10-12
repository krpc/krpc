# pylint: disable=too-many-lines
import socket
import sys
import threading
import time
import unittest
from unittest import mock

import krpc.schema.KRPC_pb2 as KRPC
import krpc.error
import krpc.stream
from krpc.client import Client
from krpc.error import StreamError
from krpc import streammanager
from krpc.streammanager import StreamManager
from krpc.test.servertestcase import ServerTestCase


class TestStream(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestStream, cls).setUpClass()

    @staticmethod
    def wait() -> None:
        time.sleep(0.01)

    def test_method(self) -> None:
        with self.conn.stream(self.conn.test_service.float_to_string, 3.14159) as x:
            for _ in range(5):
                self.assertEqual("3.14159", x())
                self.wait()

    def test_property(self) -> None:
        self.conn.test_service.string_property = "foo"
        with self.conn.stream(getattr, self.conn.test_service, "string_property") as x:
            for _ in range(5):
                self.assertEqual("foo", x())
                self.wait()

    def test_class_method(self) -> None:
        obj = self.conn.test_service.create_test_object("bob")
        with self.conn.stream(obj.float_to_string, 3.14159) as x:
            for _ in range(5):
                self.assertEqual("bob3.14159", x())
                self.wait()

    def test_class_static_method(self) -> None:
        with self.conn.stream(
            self.conn.test_service.TestClass.static_method, "foo"
        ) as x:
            for _ in range(5):
                self.assertEqual("jebfoo", x())
                self.wait()

    def test_class_property(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        obj.int_property = 42
        with self.conn.stream(getattr, obj, "int_property") as x:
            for _ in range(5):
                self.assertEqual(42, x())
                self.wait()

    def test_procedure_returning_an_event(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            self.conn.add_stream(self.conn.test_service.on_timer, 1000)
        self.assertIn("returns an event, and cannot be streamed", str(cm.exception))

    def test_null_initial_value(self) -> None:
        """Test that the server sends a first stream update
        even if the value is null. See github issue #515"""
        with self.conn.stream(self.conn.test_service.echo_test_object, None) as x:
            self.assertEqual(None, x())

    def test_property_setters_are_invalid(self) -> None:
        self.assertRaises(
            StreamError,
            self.conn.add_stream,
            setattr,
            self.conn.test_service,
            "string_property",
        )
        obj = self.conn.test_service.create_test_object("bill")
        self.assertRaises(
            StreamError, self.conn.add_stream, setattr, obj.int_property, 42
        )

    def test_counter(self) -> None:
        count = -1
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_counter"
        ) as x:
            for _ in range(5):
                self.assertLess(count, x())
                count = x()
                i = 0
                while count == x():
                    self.wait()
                    if i > 1000:
                        self.fail("Timed out waiting for stream to update")
                    i += 1

    def test_struct_counter(self) -> None:
        service = self.conn.test_service
        with self.conn.stream(
            service.counter_struct, "TestStream.test_struct_counter"
        ) as x:
            count = -1
            for _ in range(5):
                self.assertLess(count, x().int_field)
                count = x().int_field
                i = 0
                while count == x().int_field:
                    self.wait()
                    if i > 1000:
                        self.fail("Timed out waiting for stream to update")
                    i += 1

    def test_unchanging_struct_sends_one_update(self) -> None:
        # The server compares a struct field by field, so a struct whose list field is a
        # different object every frame still counts as unchanged and is sent once
        updates = []
        with self.conn.stream(self.conn.test_service.struct_default) as x:
            x.add_callback(updates.append)
            for _ in range(20):
                self.wait()
        self.assertEqual(1, len(updates))

    def test_nested(self) -> None:
        with self.conn.stream(self.conn.test_service.float_to_string, 0.123) as x0:
            with self.conn.stream(self.conn.test_service.float_to_string, 1.234) as x1:
                for _ in range(5):
                    self.assertEqual("0.123", x0())
                    self.assertEqual("1.234", x1())
                    self.wait()

    def test_interleaved(self) -> None:
        s0 = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual("0", s0())

        self.wait()
        self.assertEqual("0", s0())

        s1 = self.conn.add_stream(self.conn.test_service.int32_to_string, 1)
        self.assertEqual("0", s0())
        self.assertEqual("1", s1())

        self.wait()
        self.assertEqual("0", s0())
        self.assertEqual("1", s1())

        s1.remove()
        self.assertEqual("0", s0())
        self.assertRaises(StreamError, s1)

        self.wait()
        self.assertEqual("0", s0())
        self.assertRaises(StreamError, s1)

        s2 = self.conn.add_stream(self.conn.test_service.int32_to_string, 2)
        self.assertEqual("0", s0())
        self.assertRaises(StreamError, s1)
        self.assertEqual("2", s2())

        self.wait()
        self.assertEqual("0", s0())
        self.assertRaises(StreamError, s1)
        self.assertEqual("2", s2())

        s0.remove()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertEqual("2", s2())

        self.wait()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertEqual("2", s2())

        s2.remove()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertRaises(StreamError, s2)

        self.wait()
        self.assertRaises(StreamError, s0)
        self.assertRaises(StreamError, s1)
        self.assertRaises(StreamError, s2)

    def test_remove_stream_twice(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual("0", stream())

        self.wait()
        self.assertEqual("0", stream())

        stream.remove()
        self.assertRaises(StreamError, stream)
        stream.remove()
        self.assertRaises(StreamError, stream)

    def test_remove_through_a_stale_handle(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 45)
        self.assertEqual("45", stream())
        stream.remove()
        # The server can give a stream added again the id of the removed one
        stream_id = stream._stream.stream_id
        added_again = krpc.stream.Stream.from_stream_id(
            self.conn, stream_id, self.conn._types.string_type
        )
        with mock.patch.object(self.conn.krpc, "remove_stream") as remove:
            stream.remove()
        remove.assert_not_called()
        self.assertFalse(added_again._stream.ended)
        self.assertIs(
            added_again._stream, self.conn._stream_manager._streams[stream_id]
        )
        del self.conn._stream_manager._streams[stream_id]

    def test_add_stream_twice(self) -> None:
        s0 = self.conn.add_stream(self.conn.test_service.int32_to_string, 42)
        s1 = self.conn.add_stream(self.conn.test_service.int32_to_string, 42)
        self.assertNotEqual(s0._stream.stream_id, s1._stream.stream_id)
        self.assertEqual("42", s0())
        self.assertEqual("42", s1())
        s0.remove()
        self.assertRaises(StreamError, s0)
        self.wait()
        self.assertEqual("42", s1())
        s1.remove()

    def test_remove_then_add_stream(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual("0", stream())
        self.wait()
        self.assertEqual("0", stream())
        stream.remove()
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 0)
        self.assertEqual("0", stream())

    def test_read_straight_after_add(self) -> None:
        with self.conn.stream(self.conn.test_service.int32_to_string, 48) as x:
            self.assertEqual("48", x())

    def test_callback_added_after_the_first_value(self) -> None:
        with self.conn.stream(self.conn.test_service.int32_to_string, 49) as x:
            self.assertEqual("49", x())
            values = []
            x.add_callback(values.append)
            self.assertEqual(["49"], values)

    def test_result_before_the_add_reply_is_kept(self) -> None:
        manager = StreamManager(None)
        manager.note_issued(3)
        # One for an id noted but not registered, and one for an id not yet noted
        noted = KRPC.StreamResult(id=3)
        noted.result.value = b"\x2a"
        later = KRPC.StreamResult(id=4)
        later.result.value = b"\x2b"
        manager.update([noted, later])
        self.assertEqual(42, manager.get_stream(self.conn._types.uint32_type, 3).value)
        self.assertEqual(43, manager.get_stream(self.conn._types.uint32_type, 4).value)

    def test_result_for_an_ended_stream_is_dropped(self) -> None:
        manager = StreamManager(None)
        manager.note_issued(5)
        manager.get_stream(self.conn._types.uint32_type, 5)
        result = KRPC.StreamResult(id=2)
        result.result.value = b"\x2a"
        manager.update([result])
        self.assertEqual({}, manager._pending)

    def test_a_removal_drops_the_results_kept_for_an_id(self) -> None:
        conn = self.connect()
        try:
            manager = conn._stream_manager
            call = conn.get_call(conn.test_service.int32_to_string, 59)
            stream_id = conn.krpc.add_stream(call).id
            for _ in range(100):
                if stream_id in manager._pending:
                    break
                time.sleep(0.05)
            self.assertIn(stream_id, manager._pending)
            conn.krpc.remove_stream(stream_id)
            self.assertNotIn(stream_id, manager._unregistered)
            self.assertNotIn(stream_id, manager._pending)
        finally:
            conn.close()

    def test_kept_results_are_bounded(self) -> None:
        manager = StreamManager(None)
        manager.note_issued(3)
        for value in range(1, 100):
            result = KRPC.StreamResult(id=3)
            result.result.value = bytes([value])
            manager.update([result])
        self.assertEqual(1, len(manager._pending[3]))
        self.assertEqual(99, manager.get_stream(self.conn._types.uint32_type, 3).value)

    def test_kept_results_end_at_the_first_ending(self) -> None:
        manager = StreamManager(mock.Mock())
        manager.note_issued(3)
        bad = KRPC.StreamResult(id=3)
        # A varint that never ends
        bad.result.value = b"\xff"
        error = KRPC.StreamResult(id=3)
        error.result.error.description = "it went wrong"
        manager.update([bad, error])
        stream = manager.get_stream(self.conn._types.uint32_type, 3)
        self.assertTrue(stream.ended)
        self.assertNotIn(3, manager._streams)

    def test_kept_results_land_before_a_later_update(self) -> None:
        manager = StreamManager(None)
        manager.note_issued(3)
        first = KRPC.StreamResult(id=3)
        first.result.value = b"\x01"
        manager.update([first])
        second = KRPC.StreamResult(id=3)
        second.result.value = b"\x02"
        updater = threading.Thread(target=manager.update, args=([second],))
        decode = streammanager.Decoder.decode

        # Send the second value while the kept one is being stored
        def decode_then_update(client: object, data: bytes, typ: object) -> object:
            if data == b"\x01" and not updater.is_alive():
                updater.start()
                time.sleep(0.1)
            return decode(client, data, typ)

        with mock.patch.object(streammanager.Decoder, "decode", decode_then_update):
            stream = manager.get_stream(self.conn._types.uint32_type, 3)
            updater.join(10)
        self.assertEqual(2, stream.value)

    def test_a_value_that_fails_to_decode_removes_the_stream(self) -> None:
        client = mock.Mock()
        removed = threading.Event()
        threads = []

        def remove_stream(stream_id: int) -> None:  # pylint: disable=unused-argument
            threads.append(threading.current_thread())
            removed.set()

        client.krpc.remove_stream.side_effect = remove_stream
        manager = StreamManager(client)
        manager.get_stream(self.conn._types.uint32_type, 1)
        bad = KRPC.StreamResult(id=1)
        bad.result.value = b"\xff"
        manager.update([bad])
        self.assertTrue(removed.wait(10))
        client.krpc.remove_stream.assert_called_once_with(1)
        # Sent off the thread that applies updates
        self.assertIsNot(threading.current_thread(), threads[0])

    def test_no_removal_is_sent_once_closed(self) -> None:
        client = mock.Mock()
        manager = StreamManager(client)
        manager.notify_closed()
        manager._remove_on_server(1)
        time.sleep(0.1)
        client.krpc.remove_stream.assert_not_called()

    def test_a_callback_runs_on_one_thread_at_a_time(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        values: list[object] = []
        entered = threading.Event()
        release = threading.Event()

        def callback(value: object) -> None:
            values.append(value)
            entered.set()
            release.wait(10)

        stream.add_callback(callback)
        callback_record = stream.callbacks[0]
        newer = threading.Thread(target=stream.call, args=(callback_record, 2, "newer"))
        newer.start()
        self.assertTrue(entered.wait(10))
        older = threading.Thread(target=stream.call, args=(callback_record, 1, "older"))
        older.start()
        older.join(0.2)
        # The older value waits for the callback, then is dropped
        self.assertTrue(older.is_alive())
        release.set()
        newer.join(10)
        older.join(10)
        self.assertEqual(["newer"], values)

    def test_a_callback_is_not_passed_a_value_once_ended(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        values: list[object] = []
        stream.add_callback(values.append)
        stream.end(StreamError("The stream has been removed"))
        stream.call(stream.callbacks[0], 1, "value")
        self.assertEqual([], values)

    def test_a_server_ending_is_kept_for_the_closing_thread(self) -> None:
        client = mock.Mock()
        error = RuntimeError("ended by the server")
        client._build_error.return_value = error
        manager = StreamManager(client)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        held = threading.Event()
        release = threading.Event()

        def hold() -> None:
            with stream.condition:
                held.set()
                release.wait(10)

        holder = threading.Thread(target=hold, daemon=True)
        holder.start()
        self.assertTrue(held.wait(10))
        # The update thread stops trying to take the condition, as a thread is closing
        # the client
        manager.request_close()
        ending = KRPC.StreamResult(id=1)
        ending.result.error.description = "ended by the server"
        manager.update([ending])
        self.assertFalse(stream.ended)
        release.set()
        manager.notify_closed()
        self.assertTrue(stream.ended)
        self.assertRaises(RuntimeError, lambda: stream.value)

    def test_a_callback_is_not_passed_an_older_value(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        values: list[object] = []
        stream.add_callback(values.append)
        callback = stream.callbacks[0]
        stream.call(callback, 2, "newer")
        stream.call(callback, 1, "older")
        self.assertEqual(["newer"], values)

    @staticmethod
    def value_result(stream_id: int, value: int) -> KRPC.StreamResult:
        result = KRPC.StreamResult(id=stream_id)
        result.result.value = bytes([value])
        return result

    def test_values_during_a_first_call_are_queued(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        manager.update([self.value_result(1, 1)])
        calls: list[tuple[object, threading.Thread]] = []
        entered = threading.Event()
        release = threading.Event()

        def callback(value: object) -> None:
            calls.append((value, threading.current_thread()))
            if value == 1:
                entered.set()
                release.wait(10)

        adder = threading.Thread(target=stream.add_callback, args=(callback,))
        adder.start()
        self.assertTrue(entered.wait(10))
        updater = threading.Thread(
            target=manager.update, args=([self.value_result(1, 2)],)
        )
        updater.start()
        # The update thread queues the value, with no wait for the first call
        updater.join(10)
        self.assertFalse(updater.is_alive())
        release.set()
        adder.join(10)
        self.assertEqual([(1, adder), (2, adder)], calls)

    def test_a_value_queued_as_a_first_call_ends_is_passed(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        manager.update([self.value_result(1, 1)])
        values: list[object] = []
        first_call_returned = threading.Event()
        updater: list[threading.Thread] = []
        update_lock = stream._update_lock

        def callback(value: object) -> None:
            values.append(value)
            if value == 1:
                first_call_returned.set()

        class Lock:
            """The update lock, sending a value just after the adding thread first
            releases it once the first call has returned."""

            def __enter__(self) -> None:
                update_lock.acquire()

            def __exit__(self, *args: object) -> None:
                update_lock.release()
                if first_call_returned.is_set() and not updater:
                    thread = threading.Thread(
                        target=stream.call, args=(stream._callbacks[0], 2, 2)
                    )
                    updater.append(thread)
                    thread.start()
                    # Long enough for the value to be queued, if it is to be
                    thread.join(0.5)

        stream._update_lock = Lock()  # type: ignore[assignment]
        stream.add_callback(callback)
        updater[0].join(10)
        self.assertEqual([1, 2], values)

    def test_the_first_call_of_a_callback_waits_for_updates(self) -> None:
        conn = self.connect()
        try:
            x = conn.add_stream(
                conn.test_service.counter,
                "TestStream.test_the_first_call_of_a_callback_waits_for_updates",
            )
            x()
            waited: list[bool] = []

            def callback(value: int) -> None:  # pylint: disable=unused-argument
                # The values queued during the first call are passed on the same thread
                if threading.current_thread() is thread and not waited:
                    with x.condition:
                        waited.append(x.wait(5))
                        waited.append(x.wait(5))

            thread = threading.Thread(target=x.add_callback, args=(callback,))
            thread.start()
            thread.join(30)
            self.assertEqual([True, True], waited)
        finally:
            conn.close()

    def test_close_while_an_update_waits_for_a_first_call(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        manager.update([self.value_result(1, 1)])
        entered = threading.Event()
        release = threading.Event()
        record: list[object] = []

        def callback(value: object) -> None:
            record.append(value)
            entered.set()
            release.wait(10)

        adder = threading.Thread(target=stream.add_callback, args=(callback,))
        adder.start()
        self.assertTrue(entered.wait(10))
        callback_record = stream.callbacks[0]
        # Stands in for the window before the adding thread marks the first call
        callback_record.adding = False
        updater = threading.Thread(target=stream.call, args=(callback_record, 2, 2))
        updater.start()
        updater.join(0.1)
        self.assertTrue(updater.is_alive())
        manager.request_close()
        updater.join(10)
        self.assertFalse(updater.is_alive())
        release.set()
        adder.join(10)
        self.assertEqual([1], record)

    def test_a_removed_callback_is_not_called(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        values: list[object] = []
        stream.add_callback(values.append)
        callback = stream.callbacks[0]
        stream.remove_callback(values.append)
        stream.call(callback, 1, "value")
        self.assertEqual([], values)

    def test_a_first_call_propagates_keyboard_interrupt(self) -> None:
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        manager.update([self.value_result(1, 1)])

        values: list[object] = []

        def callback(value: object) -> None:
            values.append(value)
            if value == 1:
                raise KeyboardInterrupt()

        self.assertRaises(KeyboardInterrupt, stream.add_callback, callback)
        # The failed add leaves no callback
        self.assertEqual([], stream.callbacks)
        manager.update([self.value_result(1, 2)])
        self.assertEqual([1], values)

    def test_update_callbacks_are_not_called_once_closed(self) -> None:
        manager = StreamManager(None)
        called = []
        manager.add_update_callback(lambda: called.append(True))
        manager.notify_closed()
        manager.update([])
        self.assertEqual([], called)

    def test_a_closer_holding_the_condition_keeps_a_server_ending(self) -> None:
        client = mock.Mock()
        error = RuntimeError("ended by the server")
        client._build_error.return_value = error
        manager = StreamManager(client)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        ending = KRPC.StreamResult(id=1)
        ending.result.error.description = "ended by the server"
        with stream.condition:
            manager.request_close()
            updater = threading.Thread(target=manager.update, args=([ending],))
            updater.start()
            # Closed once the ending is decoded, while the update thread waits for the
            # condition the closing thread holds
            deadline = time.monotonic() + 10
            while 1 in manager._streams:
                if time.monotonic() > deadline:
                    self.fail("Timed out waiting for the ending to be decoded")
                time.sleep(0.001)
            manager.notify_closed()
            self.assertTrue(stream.ended)
            self.assertRaises(RuntimeError, lambda: stream.value)
        updater.join(10)

    def test_remove_keeps_a_server_ending(self) -> None:
        client = mock.Mock()
        error = RuntimeError("ended by the server")
        client._build_error.return_value = error
        manager = StreamManager(client)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        ending = KRPC.StreamResult(id=1)
        ending.result.error.description = "ended by the server"
        # Removed between the update thread decoding the ending and applying it
        with manager.update_lock:
            decoded = manager._decode(stream, ending)
        stream.remove()
        self.assertTrue(stream.ended)
        self.assertRaises(RuntimeError, lambda: stream.value)
        client.krpc.remove_stream.assert_not_called()
        manager._apply([decoded])
        self.assertRaises(RuntimeError, lambda: stream.value)

    def test_close_with_an_end_callback_that_exits(self) -> None:
        conn = self.connect()
        x = conn.add_stream(conn.test_service.int32_to_string, 1)
        y = conn.add_stream(conn.test_service.int32_to_string, 2)
        self.assertEqual("1", x())
        self.assertEqual("2", y())
        ended: list[Exception] = []

        def exit_(_: Exception) -> None:
            sys.exit()

        x.add_end_callback(exit_)
        y.add_end_callback(ended.append)
        self.assertRaises(SystemExit, conn.close)
        # The other stream ended, and the update thread was joined
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], krpc.error.ConnectionError)
        assert conn._stream_thread is not None
        self.assertFalse(conn._stream_thread.is_alive())

    def test_remove_with_an_end_callback_that_exits(self) -> None:
        client = mock.Mock()
        manager = StreamManager(client)
        stream = manager.get_stream(self.conn._types.uint32_type, 1)

        def exit_(_: Exception) -> None:
            sys.exit()

        stream.add_end_callback(exit_)
        self.assertRaises(SystemExit, stream.remove)
        self.assertTrue(stream.ended)
        client.krpc.remove_stream.assert_called_once_with(1)

    def test_invalid_operation_exception_immediately(self) -> None:
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception
        )
        with self.assertRaises(RuntimeError) as cm:
            stream()
        self.assertTrue(str(cm.exception).startswith("Invalid operation"))

    def test_invalid_operation_exception_later(self) -> None:
        self.conn.test_service.reset_invalid_operation_exception_later()
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception_later
        )
        self.assertEqual(0, stream())
        with self.assertRaises(RuntimeError) as cm:
            while True:
                self.wait()
                stream()
        self.assertTrue(str(cm.exception).startswith("Invalid operation"))

    def test_custom_exception_immediately(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.throw_custom_exception)
        with self.assertRaises(RuntimeError) as cm:
            stream()
        self.assertTrue(str(cm.exception).startswith("A custom kRPC exception"))

    def test_custom_exception_later(self) -> None:
        self.conn.test_service.reset_custom_exception_later()
        stream = self.conn.add_stream(
            self.conn.test_service.throw_custom_exception_later
        )
        self.assertEqual(0, stream())
        with self.assertRaises(RuntimeError) as cm:
            while True:
                self.wait()
                stream()
        self.assertTrue(str(cm.exception).startswith("A custom kRPC exception"))

    def test_yield_exception(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.blocking_procedure, 10)
        for _ in range(100):
            self.assertEqual(55, stream())
            self.wait()

    def test_wait(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_wait", 10
        ) as x:
            with x.condition:
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    x.wait()
                    count += 1
                    self.assertEqual(count, x())

    def test_wait_timeout_short(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_wait_timeout_short", 10
        ) as x:
            with x.condition:
                count = x()
                self.assertFalse(x.wait(timeout=0))
                self.assertEqual(count, x())

    def test_wait_timeout_long(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_wait_timeout_long", 10
        ) as x:
            with x.condition:
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    self.assertTrue(x.wait(timeout=10))
                    count += 1
                    self.assertEqual(count, x())

    def test_wait_update(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_wait_update", 10
        ) as x:
            with self.conn.stream_update_condition:
                self.conn.wait_for_stream_update()
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    self.conn.wait_for_stream_update()
                    count += 1
                    self.assertEqual(count, x())

    def test_wait_update_timeout_short(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter,
            "TestStream.test_wait_update_timeout_short",
            10,
        ) as x:
            with self.conn.stream_update_condition:
                self.conn.wait_for_stream_update()
                count = x()
                self.conn.wait_for_stream_update(timeout=0)
                self.assertEqual(count, x())

    def test_wait_update_timeout_long(self) -> None:
        with self.conn.stream(
            self.conn.test_service.counter,
            "TestStream.test_wait_update_timeout_long",
            10,
        ) as x:
            with self.conn.stream_update_condition:
                self.conn.wait_for_stream_update()
                count = x()
                self.assertTrue(count < 10)
                while count < 10:
                    self.conn.wait_for_stream_update(timeout=10)
                    count += 1
                    self.assertEqual(count, x())

    test_callback_value = -1

    def test_callback(self) -> None:
        error = threading.Event()
        stop = threading.Event()

        def callback(x: int) -> None:
            if x > 5:
                stop.set()
            elif self.test_callback_value + 1 != x:
                error.set()
                stop.set()
            else:
                self.test_callback_value += 1

        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_callback", 10
        ) as x:
            x.add_callback(callback)
            stop.wait(3)

        self.assertTrue(stop.is_set())
        self.assertFalse(error.is_set())
        self.assertEqual(self.test_callback_value, 5)

    def test_callback_is_not_passed_an_error(self) -> None:
        self.conn.test_service.reset_invalid_operation_exception_later()
        values = []
        with self.conn.stream(
            self.conn.test_service.throw_invalid_operation_exception_later
        ) as stream:
            stream.add_callback(values.append)
            with self.assertRaises(RuntimeError):
                for _ in range(1000):
                    self.wait()
                    stream()
            # Callbacks run after the value is stored
            time.sleep(0.1)
        self.assertEqual([0], values)

    def test_remove_wakes_a_waiting_thread(self) -> None:
        # Nothing further will update a removed stream, so a thread waiting on it has to be
        # woken by the removal or it waits forever. The value streamed here never changes,
        # so the server sends nothing more after the first update and the removal is the
        # only thing that can wake the waiter.
        conn = self.connect()
        try:
            x = conn.add_stream(conn.test_service.int32_to_string, 42)
            self.assertEqual("42", x())
            woke = threading.Event()

            def wait_for_update() -> None:
                with x.condition:
                    try:
                        x.wait()
                    except StreamError:
                        woke.set()

            thread = threading.Thread(target=wait_for_update, daemon=True)
            thread.start()
            time.sleep(0.2)
            x.remove()
            self.assertTrue(woke.wait(10))
        finally:
            conn.close()

    def test_close_wakes_a_waiting_thread(self) -> None:
        # Same for closing the client, which stops every stream at once
        conn = self.connect()
        x = conn.add_stream(conn.test_service.int32_to_string, 43)
        self.assertEqual("43", x())
        woke = threading.Event()

        def wait_for_update() -> None:
            with x.condition:
                try:
                    x.wait()
                except krpc.error.ConnectionError:
                    woke.set()

        thread = threading.Thread(target=wait_for_update, daemon=True)
        thread.start()
        time.sleep(0.2)
        conn.close()
        self.assertTrue(woke.wait(10))
        self.assertRaises(krpc.error.ConnectionError, x)

    def test_close_from_a_callback(self) -> None:
        # Callbacks run on the update thread, so closing the client from one must not try
        # to join that thread, which would raise and leave the client half closed
        conn = self.connect()
        closed = threading.Event()

        def callback(x: int) -> None:  # pylint: disable=unused-argument
            conn.close()
            closed.set()

        stream = conn.add_stream(
            conn.test_service.counter, "TestStream.test_close_from_a_callback", 1
        )
        stream.add_callback(callback)
        self.assertTrue(closed.wait(10))

    def test_close_while_holding_a_condition(self) -> None:
        conn = self.connect()
        x = conn.add_stream(
            conn.test_service.counter, "TestStream.test_close_while_holding_a_condition"
        )

        def close() -> None:
            with x.condition:
                x.wait()
                time.sleep(0.2)
                conn.close()

        thread = threading.Thread(target=close, daemon=True)
        thread.start()
        thread.join(10)
        self.assertFalse(thread.is_alive())
        self.assertRaises(krpc.error.ConnectionError, x)

    def _close_while_holding_the_update_condition(self, conn: Client) -> None:
        def close() -> None:
            with conn.stream_update_condition:
                conn.close()
                # The update thread stops trying to take the condition this thread holds
                conn._stream_thread.join(10)

        closer = threading.Thread(target=close, daemon=True)
        closer.start()
        closer.join(10)
        self.assertFalse(closer.is_alive())
        self.assertFalse(conn._stream_thread.is_alive())

    def test_close_while_holding_the_update_condition(self) -> None:
        self._close_while_holding_the_update_condition(self.connect())

    def test_close_while_holding_the_update_condition_with_streams(self) -> None:
        conn = self.connect()
        x = conn.add_stream(
            conn.test_service.counter,
            "TestStream.test_close_while_holding_the_update_condition_with_streams",
        )
        with x.condition:
            x.wait()
        self._close_while_holding_the_update_condition(conn)
        self.assertRaises(krpc.error.ConnectionError, x)

    def test_closing_wakes_a_thread_waiting_for_an_update(self) -> None:
        # A thread holding a stream condition waits for a stream update. The closing
        # thread can end the stream only once that thread wakes.
        manager = StreamManager(mock.Mock())
        stream = manager.get_stream(self.conn._types.uint32_type, 1)
        waiting = threading.Event()

        def wait() -> None:
            with stream.condition:
                with manager.update_condition:
                    waiting.set()
                    manager.wait_for_update()

        waiter = threading.Thread(target=wait, daemon=True)
        waiter.start()
        self.assertTrue(waiting.wait(10))
        manager.request_close()
        closer = threading.Thread(target=manager.notify_closed, daemon=True)
        closer.start()
        closer.join(10)
        self.assertFalse(closer.is_alive())
        self.assertTrue(stream.ended)

    def test_close_from_the_first_call_of_a_callback(self) -> None:
        conn = self.connect()
        x = conn.add_stream(
            conn.test_service.counter,
            "TestStream.test_close_from_the_first_call_of_a_callback",
        )
        x()
        closed = threading.Event()

        def callback(value: int) -> None:  # pylint: disable=unused-argument
            conn.close()
            closed.set()

        thread = threading.Thread(target=x.add_callback, args=(callback,), daemon=True)
        thread.start()
        thread.join(10)
        self.assertFalse(thread.is_alive())
        self.assertTrue(closed.is_set())

    def test_update_does_not_resurrect_a_removed_stream(self) -> None:
        # A value decoded before the stream was removed must not overwrite the error that
        # remove() stored. The stream is gone from the registry, so nothing would ever
        # replace that value and it would be returned as though still live.
        manager = StreamManager(None)
        stream = manager.get_stream(self.conn._types.string_type, 1)

        # Remove the stream midway through the update, while its value is being decoded
        class RemovingDecoder:
            # pylint: disable=unused-argument
            @staticmethod
            def decode(client: object, data: bytes, typ: object) -> str:
                del manager._streams[1]
                stream.end(StreamError("The stream has been removed"))
                return "a stale value"

        result = KRPC.StreamResult(id=1)
        result.result.value = b"whatever"
        decoder = streammanager.Decoder
        streammanager.Decoder = RemovingDecoder  # type: ignore[misc]
        try:
            manager.update([result])
        finally:
            streammanager.Decoder = decoder  # type: ignore[misc]

        with self.assertRaises(StreamError):
            stream.value  # pylint: disable=pointless-statement

    def test_a_value_that_fails_to_decode_ends_only_its_stream(self) -> None:
        manager = StreamManager(None)
        failing = manager.get_stream(self.conn._types.uint32_type, 1)
        working = manager.get_stream(self.conn._types.uint32_type, 2)
        ended = []
        failing.add_end_callback(ended.append)
        bad = KRPC.StreamResult(id=1)
        # A varint that never ends
        bad.result.value = b"\xff"
        good = KRPC.StreamResult(id=2)
        good.result.value = b"\x2a"
        manager.update([bad, good])
        self.assertEqual(1, len(ended))
        self.assertRaises(Exception, lambda: failing.value)
        self.assertEqual(42, working.value)
        self.assertNotIn(1, manager._streams)

    def test_error_ends_the_stream(self) -> None:
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception
        )
        ended = []
        stream.add_end_callback(ended.append)
        with stream.condition:
            self.assertRaises(RuntimeError, stream.wait)
            self.assertRaises(RuntimeError, stream.wait, 1)
        self.assertRaises(RuntimeError, stream)
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], RuntimeError)
        # Added after the end, so called straight away
        stream.add_end_callback(ended.append)
        self.assertEqual(2, len(ended))
        # Already removed by the server, so this does nothing
        stream.remove()
        self.assertRaises(RuntimeError, stream)

    def test_raising_the_ending_again_does_not_grow_its_traceback(self) -> None:
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception
        )
        depths = []
        for _ in range(3):
            try:
                stream()
            except RuntimeError as exn:
                depth = 0
                tb = exn.__traceback__
                while tb is not None:
                    depth += 1
                    tb = tb.tb_next
                depths.append(depth)
        self.assertEqual(depths[1], depths[2])

    def test_end_callback_on_remove(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 46)
        self.assertEqual("46", stream())
        ended = []
        stream.add_end_callback(ended.append)
        stream.remove()
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], StreamError)

    def test_end_callback_on_error(self) -> None:
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception
        )
        ended = threading.Event()
        errors: list[Exception] = []

        def end_callback(error: Exception) -> None:
            errors.append(error)
            ended.set()

        stream.add_end_callback(end_callback)
        self.assertTrue(ended.wait(10))
        self.assertTrue(str(errors[0]).startswith("Invalid operation"))

    def test_end_callback_added_after_the_end(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 48)
        stream.remove()
        ended: list[Exception] = []
        stream.add_end_callback(ended.append)
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], StreamError)

    def test_remove_end_callback(self) -> None:
        stream = self.conn.add_stream(self.conn.test_service.int32_to_string, 49)
        ended: list[Exception] = []
        stream.add_end_callback(ended.append)
        stream.remove_end_callback(ended.append)
        stream.remove()
        self.assertEqual([], ended)

    def test_connection_failure_wakes_a_waiting_thread(self) -> None:
        conn = self.connect()
        try:
            x = conn.add_stream(conn.test_service.int32_to_string, 47)
            self.assertEqual("47", x())
            ended = []
            x.add_end_callback(ended.append)
            woke = threading.Event()

            def wait_for_update() -> None:
                with x.condition:
                    try:
                        x.wait()
                    except krpc.error.ConnectionError:
                        woke.set()

            thread = threading.Thread(target=wait_for_update, daemon=True)
            thread.start()
            time.sleep(0.2)
            # Stands in for the server closing the connection
            conn._stream_connection._socket.shutdown(socket.SHUT_RDWR)
            self.assertTrue(woke.wait(10))
            self.assertEqual(1, len(ended))
            self.assertRaises(krpc.error.ConnectionError, x)
        finally:
            conn.close()

    def test_remove_while_holding_condition(self) -> None:
        # The update thread must not hold the update lock while waiting for a stream's
        # condition. A caller holding that condition - which is how waiting for an update
        # is documented to work - otherwise blocks forever on anything needing the update
        # lock, and the update thread blocks on the condition, deadlocking both.
        done = threading.Event()
        # Uses its own connection, so that a regression deadlocks only this test rather
        # than stalling every later test sharing the class connection
        conn = self.connect()

        def run() -> None:
            x = conn.add_stream(
                conn.test_service.counter,
                "TestStream.test_remove_while_holding_condition",
                1,
            )
            with x.condition:
                x.wait()
                # Let the update thread reach the next update and block on this condition
                time.sleep(0.2)
                x.remove()
            done.set()

        thread = threading.Thread(target=run, daemon=True)
        thread.start()
        thread.join(10)
        self.assertTrue(done.is_set())

    def test_callback_that_raises(self) -> None:
        # A callback that raises must not end the update thread, which would stop every
        # stream on the connection from updating again
        called = threading.Event()
        stop = threading.Event()

        def failing_callback(x: int) -> None:  # pylint: disable=unused-argument
            called.set()
            raise RuntimeError("callback failed")

        def callback(x: int) -> None:
            if x > 5:
                stop.set()

        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_callback_that_raises", 10
        ) as x:
            x.add_callback(failing_callback)
            x.add_callback(callback)
            stop.wait(10)

        self.assertTrue(called.is_set())
        # Updates kept arriving, and the callback added after the one that raised ran
        self.assertTrue(stop.is_set())

    def test_callback_that_raises_a_base_exception(self) -> None:
        raised = threading.Event()
        stop = threading.Event()

        test_thread = threading.current_thread()

        def failing_callback(x: int) -> None:  # pylint: disable=unused-argument
            # Raised on the update thread, as a first call on this thread propagates it
            if threading.current_thread() is not test_thread and not raised.is_set():
                raised.set()
                raise SystemExit()

        def callback(x: int) -> None:
            if x > 5:
                stop.set()

        # Uses its own connection, so that a regression stops only this test's updates
        with self.connect() as conn:
            with conn.stream(
                conn.test_service.counter,
                "TestStream.test_callback_that_raises_a_base_exception",
                10,
            ) as x:
                x.add_callback(callback)
                x.add_callback(failing_callback)
                stop.wait(10)

        self.assertTrue(raised.is_set())
        self.assertTrue(stop.is_set())

    def test_end_callback_on_close(self) -> None:
        conn = self.connect()
        x = conn.add_stream(conn.test_service.int32_to_string, 53)
        self.assertEqual("53", x())
        ended = []
        x.add_end_callback(ended.append)
        conn.close()
        self.assertEqual(1, len(ended))
        self.assertIsInstance(ended[0], krpc.error.ConnectionError)

    def test_close_while_holding_two_conditions(self) -> None:
        # Ending the stream waited on releases the other condition, which is then ended
        conn = self.connect()
        first = conn.add_stream(conn.test_service.int32_to_string, 54)
        second = conn.add_stream(conn.test_service.int32_to_string, 55)
        self.assertEqual("54", first())
        self.assertEqual("55", second())
        waiting = threading.Event()
        raised = threading.Event()

        def wait() -> None:
            with first.condition:
                with second.condition:
                    waiting.set()
                    try:
                        second.wait()
                    except krpc.error.ConnectionError:
                        raised.set()

        waiter = threading.Thread(target=wait, daemon=True)
        waiter.start()
        self.assertTrue(waiting.wait(10))
        closer = threading.Thread(target=conn.close, daemon=True)
        closer.start()
        closer.join(10)
        self.assertFalse(closer.is_alive())
        self.assertTrue(raised.is_set())
        self.assertRaises(krpc.error.ConnectionError, first)

    def test_connection_failure_while_holding_two_conditions(self) -> None:
        conn = self.connect()
        try:
            first = conn.add_stream(conn.test_service.int32_to_string, 56)
            second = conn.add_stream(conn.test_service.int32_to_string, 57)
            self.assertEqual("56", first())
            self.assertEqual("57", second())
            waiting = threading.Event()
            raised = threading.Event()

            def wait() -> None:
                with first.condition:
                    with second.condition:
                        waiting.set()
                        try:
                            second.wait()
                        except krpc.error.ConnectionError:
                            raised.set()

            waiter = threading.Thread(target=wait, daemon=True)
            waiter.start()
            self.assertTrue(waiting.wait(10))
            # Stands in for the server closing the connection
            conn._stream_connection._socket.shutdown(socket.SHUT_RDWR)
            self.assertTrue(raised.wait(10))
            conn._stream_thread.join(10)
            self.assertFalse(conn._stream_thread.is_alive())
            self.assertRaises(krpc.error.ConnectionError, first)
        finally:
            conn.close()

    def test_close_holding_a_condition_a_callback_waits_for(self) -> None:
        conn = self.connect()
        y = conn.add_stream(conn.test_service.int32_to_string, 58)
        self.assertEqual("58", y())
        x = conn.add_stream(
            conn.test_service.counter,
            "TestStream.test_close_holding_a_condition_a_callback_waits_for",
        )
        armed = threading.Event()
        entered = threading.Event()
        closed = threading.Event()

        def callback(value: int) -> None:  # pylint: disable=unused-argument
            if armed.is_set():
                entered.set()
                with y.condition:
                    pass

        x.add_callback(callback)

        def close() -> None:
            with y.condition:
                armed.set()
                entered.wait(10)
                conn.close()
                closed.set()

        closer = threading.Thread(target=close, daemon=True)
        closer.start()
        closer.join(10)
        self.assertTrue(entered.is_set())
        self.assertTrue(closed.is_set())
        # The update thread ends once the condition is released
        conn._stream_thread.join(10)
        self.assertFalse(conn._stream_thread.is_alive())

    def test_close_from_an_end_callback(self) -> None:
        conn = self.connect()
        event = conn.test_service.on_timer(200)
        closed = threading.Event()
        threads = []

        def end_callback(ending: Exception) -> None:  # pylint: disable=unused-argument
            threads.append(threading.current_thread())
            conn.close()
            closed.set()

        event.add_end_callback(end_callback)
        self.assertTrue(closed.wait(10))
        self.assertIs(conn._stream_thread, threads[0])

    def test_remove_callback(self) -> None:
        called1 = threading.Event()
        called2 = threading.Event()

        def callback1(x: int) -> None:  # pylint: disable=unused-argument
            called1.set()

        def callback2(x: int) -> None:  # pylint: disable=unused-argument
            called2.set()

        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_remove_callback", 10
        ) as x:
            x.add_callback(callback1)
            x.add_callback(callback2)
            x.remove_callback(callback2)
            called1.wait(3)

        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    def test_update_callback(self) -> None:
        stop = threading.Event()

        def callback() -> None:
            stop.set()

        self.conn.add_stream_update_callback(callback)
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_update_callback", 10
        ):
            stop.wait(3)

        self.assertTrue(stop.is_set())

    def test_remove_update_callback(self) -> None:
        called1 = threading.Event()
        called2 = threading.Event()

        def callback1() -> None:
            called1.set()

        def callback2() -> None:
            called2.set()

        self.conn.add_stream_update_callback(callback1)
        self.conn.add_stream_update_callback(callback2)
        self.conn.remove_stream_update_callback(callback2)
        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_remove_update_callback", 10
        ):
            called1.wait(3)

        self.assertTrue(called1.is_set())
        self.assertFalse(called2.is_set())

    def test_rate(self) -> None:
        error = threading.Event()
        stop = threading.Event()
        values: list[int] = []

        def callback(x: int) -> None:
            # The stream can update before its rate is set, so counting starts from the
            # first value passed
            if values and values[-1] + 1 != x:
                error.set()
                stop.set()
            values.append(x)
            if len(values) > 5:
                stop.set()

        with self.conn.stream(
            self.conn.test_service.counter, "TestStream.test_rate"
        ) as x:
            x.rate = 5
            x.add_callback(callback)
            start = time.time()
            # The wait timeout is a generous backstop that lets the test fail
            # cleanly (rather than hang) if updates stop arriving; it sits well
            # above the elapsed-time upper bound asserted below.
            stop.wait(10)
            elapsed = time.time() - start

        # At 5Hz, five increments take ~1s. The lower bound checks the rate
        # limit was honoured; the upper bound is a generous hang detector, kept
        # loose so the test does not flake under parallel load.
        self.assertGreater(elapsed, 1)
        self.assertLess(elapsed, 3)
        self.assertTrue(stop.is_set())
        self.assertFalse(error.is_set())


if __name__ == "__main__":
    unittest.main()
