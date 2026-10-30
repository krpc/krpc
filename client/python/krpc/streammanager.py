from __future__ import annotations

import threading
import weakref
from collections.abc import Callable, Iterable, Mapping
from typing import TYPE_CHECKING

import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder
from krpc.error import ConnectionError, StreamError
from krpc.session import _report, on_dispatcher_thread
from krpc.types import TypeBase

if TYPE_CHECKING:
    from krpc.client import Client


def _invoke_callback(fn: Callable[..., None], *args: object) -> None:
    """Run a callback, and report an exception it raises through the thread excepthook.
    On the dispatcher thread every exception is reported, as one that escapes ends the
    thread and stops every stream. On another thread an exception that is not an
    Exception, such as KeyboardInterrupt, propagates."""
    try:
        fn(*args)
    except Exception:
        _report()
    except BaseException:
        if not on_dispatcher_thread():
            raise
        _report()


class _WakingCondition(threading.Condition):
    """A condition that can be told to wake its waiters when the thread holding it lets
    go, for a closing thread that cannot take it."""

    def __init__(self) -> None:
        super().__init__()
        self._wake_on_release = False
        # The condition binds release to its lock, so it is replaced here
        self._release_lock = self.release
        self.release = self._release

    def _release(self) -> None:
        if self._wake_on_release:
            self.notify_all()
        self._release_lock()

    def __exit__(self, *args: object) -> None:
        self._release()

    def wake(self) -> None:
        """Wake the waiters now if the condition is free. Otherwise wake them once the
        thread holding it lets go, or once a thread of its own can take it, as a holder
        that waits lets go without the release that wakes them."""
        self._wake_on_release = True
        if self.acquire(blocking=False):
            self._release()
            return

        def retry() -> None:
            while not self.acquire(timeout=0.01):
                pass
            self._release()

        threading.Thread(target=retry, name="krpc-waker", daemon=True).start()


class _Callback:
    """A value callback, or an event callback that takes no arguments. The version is
    that of the last value passed to a value callback, or the firing count last passed
    to an event callback. The lock is held while the callback runs, so it runs on one
    thread at a time. While the adding thread makes the first call, the dispatcher thread
    queues its values in missed for the adding thread to pass on, and leaves a server
    ending in ending for the adding thread to call the end callbacks with after them."""

    def __init__(self, fn: Callable[..., None], event: bool = False) -> None:
        self.fn = fn
        self.event = event
        self.version = 0
        self.lock = threading.RLock()
        self.adding = False
        # Whether the adding thread is passing on the values queued during the first call
        self.draining = False
        self.missed: list[tuple[int, object]] = []
        self.ending: _Ending | None = None


class StreamImpl:
    """The state shared by every handle of one stream. It is changed under the update
    lock. The reader registers a stream with no return type, and keeps its latest result
    undecoded until the first handle gives the type."""

    def __init__(
        self, manager: StreamManager, stream_id: int, return_type: TypeBase | None
    ) -> None:
        self._manager = manager
        self._client = manager.client
        self._stream_id = stream_id
        self._return_type = return_type
        self._update_lock = manager.update_lock
        self._updated = False
        self._value: object | None = None
        self._version = 0
        # The last value announced to waiters, which wait and a thread holding the
        # condition read. Changed with the condition held, so the holder sees it frozen.
        self._announced = False
        self._announced_value: object | None = None
        self.announced_version = 0
        # The version of the value each thread last read, which a wait on that thread waits
        # past
        self._seen = threading.local()
        self._fired = 0
        # The highest firing count passed to an event callback
        self._callbacks_fired = 0
        # The firing count a replay to a late event callback is about to pass, or zero
        self._replaying = 0
        self._ending: Exception | None = None
        # The ending as a thread holding the condition reads it. An ending from the
        # server reaches the holder when its waiters are woken, as a value does.
        self._announced_ending: Exception | None = None
        # Whether a remove or a close ended the stream, which discards an unseen firing
        self._ended_by_client = False
        self._condition = _WakingCondition()
        self._callbacks: list[_Callback] = []
        self._end_callbacks: list[Callable[[Exception], None]] = []
        self._rate = 0.0
        self._raw: KRPC.StreamResult | None = None

    @property
    def stream_id(self) -> int:
        return self._stream_id

    @property
    def return_type(self) -> TypeBase:
        assert self._return_type is not None
        return self._return_type

    @property
    def rate(self) -> float:
        return self._rate

    @rate.setter
    def rate(self, value: float) -> None:
        self._rate = value
        # The server has no stream for the id once it ends
        if not self.ended:
            self._client.krpc.set_stream_rate(self._stream_id, value)

    @property
    def ended(self) -> bool:
        return self._ending is not None

    def raise_if_ended(self) -> None:
        """Raises the error that ended the stream, if it has ended. A thread holding the
        condition reads the ending once it is announced. The traceback is reset, so
        raising it again does not grow it."""
        ending = self._ending
        if ending is None:
            return
        if (
            ending is not self._announced_ending
            and not self._manager.closed
            and self._condition._is_owned()  # pyright: ignore[reportAttributeAccessIssue]
        ):
            return
        raise ending.with_traceback(None)

    @property
    def ended_by_client(self) -> bool:
        return self._ended_by_client

    @property
    def ending_announced(self) -> bool:
        return self._announced_ending is not None

    def announce_ending(self) -> None:
        """Announce the ending to a thread holding the condition, with it held."""
        self._announced_ending = self._ending

    @property
    def value(self) -> object:
        """The latest value, or the last one announced to waiters for a thread holding the
        condition, which then sees the value change only when its waiters are woken."""
        self.raise_if_ended()
        if self._condition._is_owned():  # pyright: ignore[reportAttributeAccessIssue]
            if not self._announced:
                raise StreamError("Stream has no value")
            self._seen.version = self.announced_version
            return self._announced_value
        with self._update_lock:
            if not self._updated:
                raise StreamError("Stream has no value")
            self._seen.version = self._version
            return self._value

    @property
    def seen_version(self) -> int:
        """The version of the value this thread last read."""
        return getattr(self._seen, "version", 0)

    @property
    def updated(self) -> bool:
        """Whether there is a value, announced to waiters for a thread holding the
        condition."""
        if self._condition._is_owned():  # pyright: ignore[reportAttributeAccessIssue]
            return self._announced
        return self._updated

    def announce(self, value: object, version: int) -> None:
        """Announce a stored value to waiters, with the condition held."""
        # Stored before the flag, as a reader checks the flag with no lock held
        self._announced_value = value
        self.announced_version = version
        self._announced = True

    @property
    def version(self) -> int:
        """The number of values received."""
        return self._version

    @property
    def fired(self) -> int:
        """The number of true values received, which for an event is its firings."""
        return self._fired

    def set_value(self, value: object) -> None:
        with self._update_lock:
            # Stored before the flag, as a reader checks the flag with no lock held
            self._value = value
            self._updated = True
            self._version += 1
            if value is True:
                self._fired += 1

    def set_ending(
        self, ending: Exception, announce: bool = True, by_client: bool = False
    ) -> list[Callable[[Exception], None]]:
        """Ends the stream, without waking its waiters. An ending that is not announced
        reaches a thread holding the condition once announce_ending is called. Returns
        the end callbacks to call, with no lock held. Returns none if the stream had
        already ended."""
        with self._update_lock:
            if self._ending is not None:
                return []
            self._ending = ending
            self._ended_by_client = by_client
            if announce:
                self._announced_ending = ending
            callbacks = self._end_callbacks
            self._end_callbacks = []
            return callbacks

    def end(self, ending: Exception) -> list[Callable[[Exception], None]]:
        """Ends the stream and wakes its waiters. Returns the end callbacks to call, with
        no lock held. Returns none if the stream had already ended."""
        with self._condition:
            callbacks = self.set_ending(ending, by_client=True)
            self._condition.notify_all()
        return callbacks

    @property
    def condition(self) -> _WakingCondition:
        return self._condition

    @property
    def callbacks(self) -> list[_Callback]:
        with self._update_lock:
            return self._callbacks

    def call(
        self, callback: _Callback, version: int, value: object, added: bool = False
    ) -> None:
        """Call a callback with a value, unless it has been passed a newer one, has been
        removed, or the stream has ended. The version is the firing count for an event
        callback, which is passed only firings. Called with no lock held.

        The first call, from the adding thread, skips the value if the dispatcher thread
        is calling the callback, as that thread has a newer value. The dispatcher thread
        queues its values for the adding thread while the first call runs."""
        if callback.event and value is not True:
            return
        if added:
            if not callback.lock.acquire(blocking=False):
                return
        elif not self._enter_on_dispatcher_thread(callback, version, value):
            return
        try:
            with self._update_lock:
                # A late event callback is passed an unseen firing after a server ending
                ended = self._ending is not None and (
                    not added or self._ended_by_client
                )
                if ended or not self._note_passed(callback, version):
                    return
                callback.adding = added
            if not added:
                self._invoke(callback, value)
                return
            try:
                self._invoke(callback, value)
                self._call_missed(callback)
            except BaseException:
                # A failed add adds nothing
                with self._update_lock:
                    callback.adding = False
                    callback.missed.clear()
                    ending = callback.ending
                    callback.ending = None
                    self._callbacks = [x for x in self._callbacks if x is not callback]
                if ending is not None:
                    _call_end_callbacks(*ending)
                raise
        finally:
            callback.lock.release()

    def _enter_on_dispatcher_thread(
        self, callback: _Callback, version: int, value: object
    ) -> bool:
        """Take a callback's lock on the dispatcher thread. Returns false, without the
        lock, once the value is queued for the adding thread or the client is closing."""
        while not callback.lock.acquire(timeout=0.01):
            if self._manager.close_requested:
                return False
            with self._update_lock:
                if callback.adding:
                    callback.missed.append((version, value))
                    return False
        return True

    def _call_missed(self, callback: _Callback) -> None:
        """Call a callback, on the adding thread, with the values the dispatcher thread
        queued during its first call, then the end callbacks of a server ending that
        arrives meanwhile. A value queued before the server ended the stream is still
        passed on, and a remove or a close drops the queue. Clears adding in the hold of
        the update lock that takes the queue, so a value that arrives later waits for the
        callback's lock on the dispatcher thread. Called with the callback's lock held."""
        with self._update_lock:
            missed = callback.missed
            callback.missed = []
            callback.adding = False
            callback.draining = True
        try:
            for version, value in missed:
                with self._update_lock:
                    if self._ended_by_client:
                        break
                    if not self._note_passed(callback, version):
                        continue
                self._invoke(callback, value)
        finally:
            with self._update_lock:
                callback.draining = False
        with self._update_lock:
            ending = callback.ending
            callback.ending = None
        if ending is not None:
            _call_end_callbacks(*ending)

    def defer_ending(self, ending: _Ending) -> bool:
        """Leave a server ending to a callback's adding thread, to call the end callbacks
        once the values queued during the first call are passed. Returns false if no
        first call is running."""
        with self._update_lock:
            for callback in self._callbacks:
                if callback.adding or callback.draining:
                    callback.ending = ending
                    return True
        return False

    def _note_passed(self, callback: _Callback, version: int) -> bool:
        """Note that a callback is passed a version. Returns false, noting nothing, if
        the callback has been removed or passed that version or a newer one. Called
        with the update lock held."""
        if version <= callback.version or callback not in self._callbacks:
            return False
        callback.version = version
        if callback.event:
            self._callbacks_fired = max(self._callbacks_fired, version)
        return True

    @staticmethod
    def _invoke(callback: _Callback, value: object) -> None:
        if callback.event:
            _invoke_callback(callback.fn)
        else:
            _invoke_callback(callback.fn, value)

    def add_callback(self, fn: Callable[[object], None]) -> None:
        callback = _Callback(fn)
        with self._update_lock:
            self._callbacks = self._callbacks + [callback]
            has_value = self._updated and self._ending is None
            value = self._value
            version = self._version
        # Called with the value the stream already has, which no update will pass to it
        if has_value:
            self.call(callback, version, value, added=True)

    def add_event_callback(self, fn: Callable[[], None], seen: int) -> None:
        """Add an event callback. It is called straight away for a firing passed neither
        to a wait that has seen the given number of firings, nor to the event's
        callbacks."""
        callback = _Callback(fn, event=True)
        with self._update_lock:
            self._callbacks = self._callbacks + [callback]
            fired = self._fired
            replay = not self._ended_by_client and fired > max(
                seen, self._callbacks_fired, self._replaying
            )
            if replay:
                # Reserved, so a concurrent late callback does not replay it too
                self._replaying = fired
        if not replay:
            return
        try:
            self.call(callback, fired, True, added=True)
        finally:
            # A replay that passes the firing has raised callbacks_fired. One that does
            # not, as the callback was removed or the stream ended, leaves the firing.
            with self._update_lock:
                if self._replaying == fired:
                    self._replaying = 0

    def remove_callback(self, fn: Callable[..., None]) -> None:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x.fn != fn]

    def add_end_callback(self, callback: Callable[[Exception], None]) -> None:
        with self._update_lock:
            ending = self._ending
            if ending is None:
                self._end_callbacks = self._end_callbacks + [callback]
        # A stream that has already ended calls the callback here, as no ending is to come
        if ending is not None:
            _invoke_callback(callback, ending)

    def remove_end_callback(self, callback: Callable[[Exception], None]) -> None:
        with self._update_lock:
            self._end_callbacks = [x for x in self._end_callbacks if x != callback]

    def remove(self) -> None:
        self._manager.remove_stream(self)


def _call_end_callbacks(
    callbacks: Iterable[Callable[[Exception], None]], ending: Exception
) -> BaseException | None:
    """Call end callbacks. Returns the first exception that propagates from one, for the
    caller to raise once it has finished ending or removing streams."""
    deferred: BaseException | None = None
    for fn in callbacks:
        try:
            _invoke_callback(fn, ending)
        except BaseException as exn:
            if deferred is None:
                deferred = exn
    return deferred


# A stream's notification for the dispatcher: the stream, then for a value the callbacks,
# version, firing count and value to pass, or for an ending the end callbacks and ending
_Value = tuple[list[_Callback], int, int, object]
_Ending = tuple[list[Callable[[Exception], None]], Exception]


def _run_inline(work: Callable[[], object]) -> None:
    work()


class StreamManager:
    """The streams of a connection. The thread reading the socket registers streams and
    stores their values, and the dispatcher thread wakes their waiters and calls their
    callbacks."""

    def __init__(
        self,
        client: Client,
        dispatch: Callable[[Callable[[], object]], None] = _run_inline,
    ) -> None:
        self._client = client
        self._dispatch = dispatch
        self._update_lock = threading.RLock()
        self._condition = _WakingCondition()
        self._streams: dict[int, StreamImpl] = {}
        self._callbacks: list[Callable[[], None]] = []
        self._closed = False
        # The error that closed the connection, if it failed
        self._close_cause: Exception | None = None
        # Whether a thread is closing the connection
        self._close_requested = False
        # Every registered stream, to wake its waiters when the connection closes
        self._registered: weakref.WeakSet[StreamImpl] = weakref.WeakSet()

    @property
    def client(self) -> Client:
        return self._client

    @property
    def update_lock(self) -> threading.RLock:
        return self._update_lock

    @property
    def close_requested(self) -> bool:
        return self._close_requested

    @property
    def streams(self) -> Mapping[int, StreamImpl]:
        """The open streams, by id. Changed under the update lock."""
        return self._streams

    def register(self, stream_id: int) -> None:
        """Register a stream the server has added for this client. Called by the reader,
        before it reads the stream's first update."""
        with self._update_lock:
            if self._closed or stream_id in self._streams:
                return
            stream = StreamImpl(self, stream_id, None)
            self._streams[stream_id] = stream
            self._registered.add(stream)

    def add_stream(self, return_type: TypeBase, call: KRPC.ProcedureCall) -> StreamImpl:
        stream_id = self._client.krpc.add_stream(call).id
        return self.get_stream(return_type, stream_id)

    def get_stream(self, return_type: TypeBase, stream_id: int) -> StreamImpl:
        with self._update_lock:
            stream = self._streams.get(stream_id)
            if stream is not None:
                if stream._return_type is not None:
                    return stream
                stream._return_type = return_type
                raw = stream._raw
                stream._raw = None
                if raw is not None:
                    # No handle has been given out, so there is no waiter to wake or
                    # callback to call
                    _, value, ended, undecodable = self._decode(stream, raw)
                    if undecodable:
                        pass
                    elif ended:
                        assert isinstance(value, Exception)
                        stream.set_ending(value)
                    else:
                        stream.set_value(value)
                        stream.announce(value, stream.version)
            else:
                stream = StreamImpl(self, stream_id, return_type)
                if self._closed:
                    stream.set_ending(_closed_error(self._close_cause))
                    return stream
                self._streams[stream_id] = stream
                self._registered.add(stream)
        return stream

    def remove_stream(self, stream: StreamImpl) -> None:
        with self._update_lock:
            stream_id = stream.stream_id
            if self._streams.get(stream_id) is not stream:
                # A stream the server or the connection has already ended
                return
            del self._streams[stream_id]
        ending = StreamError("The stream has been removed")
        # Ended first, so a value that arrives during the call is dropped, and a call that
        # fails leaves the stream ended
        callbacks = stream.end(ending)
        deferred = _call_end_callbacks(callbacks, ending)
        try:
            self._client.krpc.remove_stream(stream_id)
        finally:
            if deferred is not None:
                raise deferred

    @property
    def update_condition(self) -> threading.Condition:
        return self._condition

    def wait_for_update(self, timeout: float | None = None) -> None:
        # Set before the close wakes the waiters, so a wait that starts after the wake
        # returns
        if self._closed:
            return
        self._condition.wait(timeout=timeout)

    @property
    def update_callbacks(self) -> list[Callable[[], None]]:
        with self._update_lock:
            return self._callbacks

    def add_update_callback(
        self, callback: Callable[[], None]
    ) -> list[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_update_callback(
        self, callback: Callable[[], None]
    ) -> list[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

    @property
    def closed(self) -> bool:
        return self._closed

    def request_close(self) -> None:
        """Note that the client is closing the connection. The dispatcher thread then
        stops trying to take a stream's condition, and the closing thread wakes the
        stream's waiters."""
        self._close_requested = True

    def holds_a_condition(self) -> bool:
        """Whether this thread holds the update condition or the condition of a
        stream."""
        if self._condition._is_owned():  # pyright: ignore[reportAttributeAccessIssue]
            return True
        with self._update_lock:
            streams = list(self._registered)
        return any(x.condition._is_owned() for x in streams)  # pyright: ignore[reportAttributeAccessIssue]

    def notify_closed(
        self, on_dispatcher_thread: bool = False, cause: Exception | None = None
    ) -> BaseException | None:
        """End every stream, once the connection is closing and no further update is
        applied. Called by the closing thread, and by the dispatcher thread when the
        connection fails. Returns the first exception that propagates from an end
        callback, for the caller to raise once it has finished closing.

        The waiters of a stream whose condition another thread holds are woken once that
        thread lets go of it, such as a thread holding one stream's condition while it
        waits on another."""
        with self._update_lock:
            if not self._closed:
                self._close_cause = cause
            self._closed = True
            ending = _closed_error(self._close_cause)
            endings: list[_Ending] = [
                (stream.set_ending(ending, by_client=True), ending)
                for stream in self._streams.values()
            ]
            self._streams.clear()
            to_wake = list(self._registered)
        for stream in to_wake:
            stream.condition.wake()
        # A thread holding a stream condition can be waiting for a stream update
        self._condition.wake()
        deferred: BaseException | None = None
        for callbacks, stream_ending in endings:
            exn = _call_end_callbacks(callbacks, stream_ending)
            if deferred is None:
                deferred = exn
        return deferred

    def _notify_update(self, on_dispatcher_thread: bool) -> None:
        """Wake the threads waiting for a stream update."""
        if on_dispatcher_thread:
            if not self._lock(self._condition):
                return
        else:
            self._condition.acquire()
        try:
            self._condition.notify_all()
        finally:
            self._condition.release()

    def _lock(self, condition: threading.Condition) -> bool:
        """Acquire a stream's condition, or the update condition, on the dispatcher
        thread. Returns false, without it, once the client is closing, as the closing
        thread can hold it while it joins the dispatcher thread."""
        while not condition.acquire(timeout=0.05):
            if self._close_requested:
                return False
        return True

    def _decode(
        self, stream: StreamImpl, result: KRPC.StreamResult
    ) -> tuple[StreamImpl, object, bool, bool]:
        """Decode a result. A final result removes the stream from the registry, as the
        server has already removed it. Returns the stream, the value or ending, whether it
        ended, and whether the value failed to decode, which drops it. Called with the
        update lock held."""
        value: object
        ended = True
        undecodable = False
        if result.result.HasField("error"):
            value = self._client._build_error(result.result.error)
        else:
            try:
                if result.result.is_null:
                    value = None
                else:
                    value = Decoder.decode(
                        self._client, result.result.value, stream.return_type
                    )
            except Exception:
                value = None
                undecodable = True
            ended = False
        if ended and self._streams.get(result.id) is stream:
            del self._streams[result.id]
        return (stream, value, ended, undecodable)

    def _apply(
        self,
        changed: list[tuple[StreamImpl, _Value | None, _Ending | None]],
        update_callbacks: list[Callable[[], None]],
    ) -> None:
        """Wake the waiters of streams with a new value or an ending, and call their
        callbacks. Runs on the dispatcher thread, with no lock held."""
        for stream, value, ending in changed:
            locked = self._lock(stream.condition)
            if locked:
                try:
                    if value is not None:
                        stream.announce(value[3], value[1])
                    else:
                        stream.announce_ending()
                    stream.condition.notify_all()
                finally:
                    stream.condition.release()
            # The ending is already stored, so the end callbacks run even once closing
            if ending is not None:
                if not stream.defer_ending(ending):
                    _call_end_callbacks(*ending)
            elif value is not None and locked:
                callbacks, version, fired, new_value = value
                for callback in callbacks:
                    stream.call(
                        callback, fired if callback.event else version, new_value
                    )
        self._notify_update(on_dispatcher_thread=True)
        for fn in update_callbacks:
            # Update callbacks have no stream ending to stop them once the client closes
            if self._closed:
                break
            _invoke_callback(fn)

    def update(self, results: Iterable[KRPC.StreamResult]) -> None:
        """Store the values and endings of a stream update, and queue the work of waking
        waiters and calling callbacks with the dispatcher. Called by the reader. A result
        for a stream not in the registry is dropped, as the client has removed it."""
        changed: list[tuple[StreamImpl, _Value | None, _Ending | None]] = []
        with self._update_lock:
            for result in results:
                stream = self._streams.get(result.id)
                if stream is None:
                    continue
                if stream._return_type is None:
                    # Kept for the first handle, unless an ending is already kept
                    raw = stream._raw
                    if raw is None or not raw.result.HasField("error"):
                        stream._raw = result
                    continue
                _, value, ended, undecodable = self._decode(stream, result)
                if undecodable:
                    continue
                if ended:
                    assert isinstance(value, Exception)
                    ending = stream.set_ending(value, announce=False)
                    changed.append((stream, None, (ending, value)))
                    continue
                stream.set_value(value)
                # Read with the value stored, so a callback added after this reads the
                # value itself
                changed.append(
                    (
                        stream,
                        (stream.callbacks, stream.version, stream.fired, value),
                        None,
                    )
                )
            update_callbacks = self._callbacks
        self._dispatch(lambda: self._apply(changed, update_callbacks))


def _closed_error(cause: Exception | None = None) -> Exception:
    """The error that ends a stream once the connection closes. Its cause is the error
    that closed the connection, if it failed, and it carries that error's disconnect
    reason."""
    reason = cause.reason if isinstance(cause, ConnectionError) else None
    error = ConnectionError("Connection closed", reason=reason)
    error.__cause__ = cause
    return error
