from __future__ import annotations

import queue
import sys
import threading
import time
from collections.abc import Callable, Mapping
from typing import TYPE_CHECKING, NoReturn

import krpc.schema.KRPC_pb2 as KRPC
from krpc.connection import Connection, MalformedSize, ReadTimeout
from krpc.decoder import decode_response_message
from krpc.error import ConnectionError

if TYPE_CHECKING:
    from krpc.streammanager import StreamManager

# Seconds a message that has started to arrive may go without a new byte, by default
DEFAULT_STALL_TIMEOUT = 30.0
# Seconds to wait for the server to close its side once ours is closed
CLOSE_TIMEOUT = 1.0

# Marks the dispatcher thread, on which callbacks run
_thread_state = threading.local()


def on_dispatcher_thread() -> bool:
    """Whether the current thread is a connection's dispatcher thread."""
    return getattr(_thread_state, "dispatcher", False)


def _report() -> None:
    threading.excepthook(
        threading.ExceptHookArgs(sys.exc_info() + (threading.current_thread(),))
    )


def disconnect_reason_name(reason: int) -> str:
    """The name of a disconnect reason, or its number if this client does not know it."""
    try:
        return KRPC.Disconnect.Reason.Name(reason)
    except ValueError:
        return str(reason)


class _Slot:
    """A call waiting for its response. stream_type is the message type a call that
    returns a stream or an event returns, KRPC.Stream or KRPC.Event. Changed under the
    session's lock."""

    __slots__ = (
        "abandoned",
        "counted",
        "done",
        "error",
        "reading",
        "response",
        "stream_type",
    )

    def __init__(self, stream_type: type | None, abandoned: bool = False) -> None:
        self.stream_type = stream_type
        self.abandoned = abandoned
        self.done = False
        self.response: KRPC.Response | None = None
        self.error: Exception | None = None
        # Whether the call holds the reader role, and whether it counts as a caller
        self.reading = False
        self.counted = False


class _Ended(Exception):
    """The connection has ended, and the thread reading it stops."""


class Session:
    """The messages of one connection.

    One thread at a time reads the socket, the reader. A caller that has written its
    request becomes the reader until its response arrives, so a call costs no hand-off
    between threads. While streams are open, the reader thread reads when no call is
    waiting for a response, so their updates arrive between calls. It waits for data
    without being the reader, so a caller can take over at once. The reader never runs
    user code, and takes no lock user code can hold.

    The dispatcher thread runs callbacks, wakes stream waiters, and makes the writes the
    reader needs, in the order the reader queues them."""

    def __init__(
        self,
        connection: Connection,
        remove_stream_request: Callable[[int], KRPC.Request],
        stall_timeout: float | None = DEFAULT_STALL_TIMEOUT,
    ) -> None:
        self._connection = connection
        self._remove_stream_request = remove_stream_request
        # None waits forever
        self._stall_timeout = stall_timeout
        self._manager: StreamManager | None = None
        self._streams: Mapping[int, object] = {}
        # Held for the whole of a write
        self._write_lock = threading.Lock()
        # Guards the ids, the pending calls, the error, and which thread is reading
        self._lock = threading.Lock()
        # Notified when a call completes or the socket is free to read, for a caller
        # waiting for one of those
        self._changed = threading.Condition(self._lock)
        # Notified when the last waiting call returns while streams are open, for the
        # reader thread
        self._idle = threading.Condition(self._lock)
        self._next_id = 1
        self._pending: dict[int, _Slot] = {}
        self._error: Exception | None = None
        # The error that failed the connection before it closed
        self._failure_before_close: Exception | None = None
        self._closing = False
        self._ended = False
        # Whether a thread is reading the socket, the number of calls waiting for their
        # response, and the number of those waiting for another thread to read it
        self._reading = False
        self._callers = 0
        self._waiting = 0
        self._half_closed = threading.Event()
        self._queue: queue.SimpleQueue[Callable[[], object] | None] = (
            queue.SimpleQueue()
        )
        self._reader = threading.Thread(
            target=self._read, name="krpc-reader", daemon=True
        )
        self._dispatcher = threading.Thread(
            target=self._dispatch, name="krpc-dispatcher", daemon=True
        )

    def start(self, manager: StreamManager) -> None:
        self._manager = manager
        self._streams = manager.streams
        self._reader.start()
        self._dispatcher.start()

    @property
    def failure_cause(self) -> Exception | None:
        """The cause of the error that failed the connection before it closed, or None
        if it had not failed. The streams end with this as their ending's cause."""
        with self._lock:
            failure = self._failure_before_close
            return _ending_cause(failure) if failure is not None else None

    @property
    def dispatcher(self) -> threading.Thread:
        return self._dispatcher

    @property
    def reader(self) -> threading.Thread:
        return self._reader

    def dispatch(self, work: Callable[[], object]) -> None:
        """Queue work for the dispatcher thread."""
        self._queue.put(work)

    def call(self, request: KRPC.Request, stream_type: type | None) -> KRPC.Response:
        """Send a request and wait for its response. Raises the connection error if the
        connection has failed or fails before the response arrives."""
        slot = _Slot(stream_type)
        # The slot records the role and count it takes, for the release in finally
        try:
            # The slot is added before the request is written, so a fast response finds
            # it. A caller that finds no thread reading becomes the reader in the same
            # hold of the lock.
            with self._lock:
                if self._error is not None:
                    raise self._error.with_traceback(None)
                request_id = self._next_id
                self._next_id += 1
                request.id = request_id
                self._pending[request_id] = slot
                self._callers += 1
                slot.counted = True
                if not self._reading:
                    self._reading = True
                    slot.reading = True
            self._write(request)
            if slot.reading:
                self._read_until(slot, returning=True)
            else:
                self._await(slot)
        except BaseException:
            # A reader discards the response when it arrives
            with self._lock:
                slot.abandoned = True
            raise
        finally:
            with self._lock:
                self._returned(slot)
        if slot.error is not None:
            raise slot.error.with_traceback(None)
        assert slot.response is not None
        return slot.response

    def _returned(self, slot: _Slot) -> None:
        """Release the reader role and the caller count a call holds, and wake the thread
        to read next. Does nothing for a call that holds neither. Called with the lock
        held."""
        if slot.reading:
            slot.reading = False
            self._reading = False
        if not slot.counted:
            return
        slot.counted = False
        self._callers -= 1
        if self._waiting > 0:
            self._changed.notify_all()
        elif self._callers == 0 and self._streams:
            self._idle.notify()

    def _read_until(self, slot: _Slot, returning: bool = False) -> None:
        """Read messages until a call's response arrives, as the reader. The call's own
        response is handled here, which is the common case. If returning and no whole
        message is buffered, the call stops reading and stops waiting in the same hold
        of the lock."""
        connection = self._connection
        pending = self._pending
        try:
            while not slot.done:
                try:
                    data = connection.receive_message_data(None, self._stall_timeout)
                except (OSError, MalformedSize) as exn:
                    self._receive_failed(exn)
                try:
                    response = decode_response_message(data)
                except Exception as exn:
                    self._end(self._malformed(exn))
                    raise _Ended from exn
                if response is None:
                    self._handle(self._parse(data))
                    continue
                with self._lock:
                    if pending.get(response.id) is slot and slot.stream_type is None:
                        del pending[response.id]
                        slot.response = response
                        slot.done = True
                        # A buffered message is handled before the role is released
                        if returning and not connection.has_message():
                            self._returned(slot)
                            return
                        continue
                error = self._handle_response(response)
                if error is not None:
                    self._end(error)
                    raise _Ended
            self._handle_buffered()
        except _Ended:
            pass

    def _handle_buffered(self) -> None:
        """Handle every whole message in the connection's buffer, before releasing the
        reader role. The reader thread then waits only on the socket, which holds the
        rest of any partial message. Raises _Ended once the connection has ended."""
        connection = self._connection
        while self._error is None and connection.has_message():
            self._handle(self._parse(connection.receive_message_data()))

    def _await(self, slot: _Slot) -> None:
        """Wait for a call's response, reading the socket once no other thread is. The
        call's release gives up the reader role."""
        with self._lock:
            while not slot.done and self._reading:
                self._waiting += 1
                try:
                    self._changed.wait()
                finally:
                    self._waiting -= 1
            if slot.done:
                return
            self._reading = True
            slot.reading = True
        self._read_until(slot)

    def _stop_reading(self) -> None:
        with self._lock:
            self._reading = False
            if self._waiting > 0:
                self._changed.notify_all()
            elif self._callers == 0 and self._streams:
                self._idle.notify()

    def remove_stream(self, stream_id: int) -> None:
        """Queue a request to remove a stream on the server, whose response is
        discarded."""

        def remove() -> None:
            try:
                self._send(
                    self._remove_stream_request(stream_id), _Slot(None, abandoned=True)
                )
            except ConnectionError:
                pass

        self.dispatch(remove)

    def _send(self, request: KRPC.Request, slot: _Slot) -> None:
        # The slot is added before the request is written, so a fast response finds it
        with self._lock:
            if self._error is not None:
                raise self._error.with_traceback(None)
            request.id = self._next_id
            self._next_id += 1
            self._pending[request.id] = slot
        self._write(request)

    def _write(self, request: KRPC.Request) -> None:
        """Write a request. A failed write ends the connection, as a failed read does."""
        try:
            with self._write_lock:
                self._connection.send_request(request)
        except OSError as exn:
            self._end(self._failure(exn))

    def close(self) -> None:
        """Tell the server the connection is closing, then close it. Waits for the server
        to close its side, for up to a second, discarding what arrives meanwhile. Skips
        telling the server if the write lock is held for longer than that, such as by a
        caller blocked on a full send buffer."""
        with self._lock:
            self._closing = True
            failed = self._error is not None
            self._failure_before_close = self._error
            # A response that arrives after the disconnect does not reach its call
            self._fail_locked(ConnectionError("Connection closed"))
        if not failed and self._write_lock.acquire(timeout=CLOSE_TIMEOUT):
            try:
                self._connection.send_message(
                    KRPC.Message(
                        disconnect=KRPC.Disconnect(reason=KRPC.Disconnect.CLOSED)
                    )
                )
                self._connection.half_close()
            except OSError:
                pass
            finally:
                self._write_lock.release()
        deadline = time.perf_counter() + CLOSE_TIMEOUT
        # A thread already reading finds the server's close itself
        with self._lock:
            while self._reading and not self._ended:
                remaining = deadline - time.perf_counter()
                if remaining <= 0:
                    break
                # Counted as waiting, so a reader releasing the role wakes this thread
                self._waiting += 1
                try:
                    self._changed.wait(remaining)
                finally:
                    self._waiting -= 1
            drain = not self._reading and not self._ended
            if drain:
                self._reading = True
        if drain:
            self._drain(deadline)
        self._connection.close()
        self._fail(ConnectionError("Connection closed"))
        with self._lock:
            if drain:
                self._reading = False
            self._ended = True
            self._idle.notify_all()
            self._changed.notify_all()

    def _drain(self, deadline: float) -> None:
        """Discard what arrives until the server closes the connection, or until the
        deadline."""
        try:
            while True:
                remaining = deadline - time.perf_counter()
                if remaining <= 0:
                    return
                self._connection.discard(remaining)
        except (OSError, TimeoutError):
            pass

    def join(self, join_dispatcher: bool) -> None:
        """Stop the threads, and wait for them except the one this runs on."""
        self._queue.put(None)
        current = threading.current_thread()
        if current is not self._reader:
            self._reader.join()
        if join_dispatcher and current is not self._dispatcher:
            self._dispatcher.join()

    def _fail(self, error: Exception) -> None:
        """Fail the connection, and every call waiting for a response."""
        with self._lock:
            self._fail_locked(error)

    def _fail_locked(self, error: Exception) -> None:
        if self._error is None:
            self._error = error
        for slot in self._pending.values():
            slot.error = self._error
            slot.done = True
        self._pending.clear()
        self._changed.notify_all()

    def _end(self, cause: Exception | None) -> None:
        """End the connection, once a thread reading it finds it has ended."""
        if cause is None:
            cause = ConnectionError("Connection closed")
        # Failed in the same hold, so a thread that finds the connection ended finds its
        # call failed
        with self._lock:
            if self._ended:
                return
            self._ended = True
            closing = self._closing
            self._fail_locked(cause)
            # The first failure, which the calls raise
            failure = self._error
            assert failure is not None
        # The closing thread ends the streams itself. Otherwise their ending's cause is
        # what ended the connection.
        manager = self._manager
        if not closing and manager is not None:
            ending_cause = _ending_cause(failure)
            self.dispatch(
                lambda: manager.notify_closed(
                    on_dispatcher_thread=True, cause=ending_cause
                )
            )
            self._queue.put(None)
        self._connection.close()
        with self._lock:
            self._idle.notify_all()

    def _read(self) -> None:
        """Read messages while streams are open and no call is waiting for a response."""
        while True:
            with self._lock:
                while (
                    self._callers > 0
                    or self._reading
                    or self._closing
                    or not self._streams
                ) and not self._ended:
                    self._idle.wait()
                if self._ended:
                    return
            # Wait for data without being the reader, so a call can take over. The wait is
            # bounded by the stall clock, so a partial message left in the buffer is timed.
            connection = self._connection
            timeout = self._stall_timeout
            if timeout is not None and connection.has_buffered():
                timeout = connection.stall_remaining(timeout)
            self._wait_readable(timeout)
            with self._lock:
                if self._ended:
                    return
                if self._callers > 0 or self._reading or self._closing:
                    continue
                self._reading = True
            try:
                # A call can read the data between the wait and taking the role
                if connection.has_buffered() or self._wait_readable(0):
                    self._read_one()
            except _Ended:
                return
            finally:
                self._stop_reading()

    def _wait_readable(self, timeout: float | None) -> bool:
        try:
            return self._connection.wait_readable(timeout)
        except OSError:
            # The read reports the failure
            return True

    def _read_one(self) -> None:
        """Read and handle one message, then any whole messages buffered with it.
        Raises _Ended once the connection has ended."""
        self._handle(self._receive())
        self._handle_buffered()

    def _receive(self) -> KRPC.Message:
        """Read one message. Raises _Ended once the connection has ended."""
        return self._parse(self._receive_data())

    def _receive_data(self) -> bytes:
        """Read the bytes of one message. Raises _Ended once the connection has
        ended."""
        try:
            return self._connection.receive_message_data(None, self._stall_timeout)
        except (OSError, MalformedSize) as exn:
            self._receive_failed(exn)

    def _receive_failed(self, exn: OSError | MalformedSize) -> NoReturn:
        """End the connection after a read failed, and raise _Ended."""
        cause: Exception | None
        if isinstance(exn, MalformedSize):
            cause = self._malformed(exn)
        elif isinstance(exn, ReadTimeout):
            cause = self._protocol_error(
                KRPC.Disconnect.TIMEOUT,
                "A message from the server stalled for more than %g seconds"
                % self._stall_timeout,
            )
        else:
            cause = self._failure(exn)
        self._end(cause)
        raise _Ended

    def _failure(self, exn: OSError) -> Exception | None:
        """The cause of the end of a connection whose socket failed, or None if the
        connection is closing."""
        with self._lock:
            if self._closing:
                return None
        cause = ConnectionError("Connection failed: %s" % exn)
        cause.__cause__ = exn
        return cause

    def _parse(self, data: bytes) -> KRPC.Message:
        """Parse a message. Raises _Ended if it is malformed, which ends the
        connection."""
        try:
            return KRPC.Message.FromString(data)
        except Exception as exn:
            cause = self._malformed(exn)
        self._end(cause)
        raise _Ended

    def _malformed(self, exn: Exception) -> Exception:
        """Fail the connection over a message that failed to parse, and return the
        error."""
        cause = self._protocol_error(
            KRPC.Disconnect.MALFORMED_MESSAGE,
            "Failed to parse a message from the server: %s" % exn,
        )
        cause.__cause__ = exn
        return cause

    def _handle(self, message: KRPC.Message) -> None:
        """Handle a message. Raises _Ended once the connection has ended."""
        try:
            cause = self._handle_message(message)
        except Exception as exn:
            cause = self._malformed(exn)
        if cause is not False:
            self._end(cause)  # pyright: ignore[reportArgumentType]
            raise _Ended

    def _handle_message(self, message: KRPC.Message) -> Exception | None | bool:
        """Handle a message. Returns False while the connection carries on, or the error
        that ended it."""
        case = message.WhichOneof("content")
        if case == "response":
            error = self._handle_response(message.response)
            if error is not None:
                return error
        elif case == "stream_update":
            assert self._manager is not None
            self._manager.update(message.stream_update.results)
        elif case == "disconnect":
            return self._received_disconnect(message.disconnect)
        elif case is not None:
            return self._protocol_error(
                KRPC.Disconnect.PROTOCOL_ERROR,
                "Unexpected %s message from the server" % case,
            )
        return False

    def _handle_response(self, response: KRPC.Response) -> Exception | None:
        with self._lock:
            slot = self._pending.pop(response.id, None)
            abandoned = slot is not None and slot.abandoned
            closing = self._closing
        if slot is None:
            # The closing thread has failed the calls this answers
            if closing:
                return None
            return self._protocol_error(
                KRPC.Disconnect.PROTOCOL_ERROR,
                "Response with an unknown id %d from the server" % response.id,
            )
        try:
            stream_id = self._stream_id(slot, response)
        except Exception as exn:
            # Put back, so failing the connection completes it
            with self._lock:
                self._pending[response.id] = slot
            return self._malformed(exn)
        if stream_id is not None:
            assert self._manager is not None
            if abandoned:
                # Removed on the server, as no handle owns it
                self.remove_stream(stream_id)
            else:
                # Registered before the next message is read, which the stream's first
                # update follows
                self._manager.register(stream_id)
        if not abandoned:
            with self._lock:
                slot.response = response
                slot.done = True
                if self._waiting > 0:
                    self._changed.notify_all()
        return None

    @staticmethod
    def _stream_id(slot: _Slot, response: KRPC.Response) -> int | None:
        """The id of the stream a call that returns a stream or an event added, or None."""
        if slot.stream_type is None:
            return None
        if response.HasField("error") or not response.results:
            return None
        result = response.results[0]
        if result.HasField("error") or result.is_null:
            return None
        message = slot.stream_type.FromString(result.value)
        return message.id if isinstance(message, KRPC.Stream) else message.stream.id

    def _received_disconnect(self, disconnect: KRPC.Disconnect) -> Exception:
        reason = disconnect_reason_name(disconnect.reason)
        text = "The server ended the connection (%s)" % reason
        if disconnect.message:
            text += ": " + disconnect.message
        return ConnectionError(text, reason=reason)

    def _protocol_error(self, reason: int, text: str) -> Exception:
        """Fail the connection, send the server a disconnect giving the reason from the
        dispatcher thread, and drain the socket until the server closes it, for up to a
        second after the half-close."""
        error = ConnectionError(text)
        self._fail(error)

        def disconnect() -> None:
            try:
                with self._write_lock:
                    self._connection.send_message(
                        KRPC.Message(
                            disconnect=KRPC.Disconnect(reason=reason, message=text)  # pyright: ignore[reportArgumentType]
                        )
                    )
                    self._connection.half_close()
            except OSError:
                pass
            finally:
                self._half_closed.set()

        # A callback reading the socket runs on the dispatcher thread, so makes the write
        # itself
        if on_dispatcher_thread():
            disconnect()
        else:
            self.dispatch(disconnect)
            self._half_closed.wait(CLOSE_TIMEOUT)
        self._drain(time.perf_counter() + CLOSE_TIMEOUT)
        return error

    def _dispatch(self) -> None:
        _thread_state.dispatcher = True
        while True:
            work = self._queue.get()
            if work is None:
                return
            try:
                work()
            except BaseException:
                _report()


def _ending_cause(error: Exception) -> Exception:
    """The cause of a stream ending, for the error that failed the connection."""
    cause = error.__cause__ or error
    assert isinstance(cause, Exception)
    return cause
