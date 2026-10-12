from __future__ import annotations
from typing import cast, Callable, Iterable, List, Optional, TYPE_CHECKING
import sys
import threading
import weakref
from krpc.stream import Stream
from krpc.types import TypeBase
from krpc.decoder import Decoder
from krpc.error import ConnectionError, StreamError  # pylint: disable=redefined-builtin
from krpc.connection import Connection
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


# Marks the update thread
_thread_state = threading.local()


def _invoke_callback(fn: Callable[..., None], *args: object) -> None:
    """Run a callback, and report an exception it raises through the thread excepthook.
    On the update thread every exception is reported, as one that escapes ends the
    thread and stops every stream. On another thread an exception that is not an
    Exception, such as KeyboardInterrupt, propagates."""
    try:
        fn(*args)
    except Exception:  # pylint: disable=broad-except
        _report()
    except BaseException:  # pylint: disable=broad-except
        if not getattr(_thread_state, "update_thread", False):
            raise
        _report()


def _report() -> None:
    threading.excepthook(
        threading.ExceptHookArgs(sys.exc_info() + (threading.current_thread(),))
    )


class _Callback:  # pylint: disable=too-few-public-methods
    """A value callback, or an event callback that takes no arguments. The version is
    that of the last value passed to a value callback, or the firing count last passed
    to an event callback. The lock is held while the callback runs, so it runs on one
    thread at a time. While the adding thread makes the first call, the update thread
    queues its values in missed for the adding thread to pass on."""

    def __init__(self, fn: Callable[..., None], event: bool = False) -> None:
        self.fn = fn
        self.event = event
        self.version = 0
        self.lock = threading.RLock()
        self.adding = False
        self.missed: List[tuple[int, object]] = []


class StreamImpl:  # pylint: disable=too-many-instance-attributes
    """The state shared by every handle of one stream. It is changed under the stream's
    condition, and then the update lock."""

    def __init__(
        self, manager: StreamManager, stream_id: int, return_type: TypeBase
    ) -> None:
        self._manager = manager
        self._client = manager.client
        self._stream_id = stream_id
        self._return_type = return_type
        self._update_lock = manager.update_lock
        self._updated = False
        self._value: Optional[object] = None
        self._version = 0
        self._fired = 0
        # The highest firing count passed to an event callback
        self._callbacks_fired = 0
        # The firing count a replay to a late event callback is about to pass, or zero
        self._replaying = 0
        self._ending: Optional[Exception] = None
        self._condition = threading.Condition()
        self._callbacks: List[_Callback] = []
        self._end_callbacks: List[Callable[[Exception], None]] = []
        self._rate = 0.0

    @property
    def stream_id(self) -> int:
        return self._stream_id

    @property
    def return_type(self) -> TypeBase:
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
        """Raises the error that ended the stream, if it has ended. The traceback is
        reset, so raising it again does not grow it."""
        ending = self._ending
        if ending is not None:
            raise ending.with_traceback(None)

    @property
    def value(self) -> object:
        self.raise_if_ended()
        if not self._updated:
            raise StreamError("Stream has no value")
        return self._value

    @property
    def updated(self) -> bool:
        return self._updated

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

    def end(self, ending: Exception) -> List[Callable[[Exception], None]]:
        """Ends the stream and wakes its waiters. Returns the end callbacks to call, with
        no lock held. Returns none if the stream had already ended."""
        with self._condition:
            with self._update_lock:
                if self._ending is not None:
                    return []
                self._ending = ending
                callbacks = self._end_callbacks
                self._end_callbacks = []
            self._condition.notify_all()
        return callbacks

    @property
    def condition(self) -> threading.Condition:
        return self._condition

    @property
    def callbacks(self) -> List[_Callback]:
        with self._update_lock:
            return self._callbacks

    def call(
        self, callback: _Callback, version: int, value: object, added: bool = False
    ) -> None:
        """Call a callback with a value, unless it has been passed a newer one, has been
        removed, or the stream has ended. The version is the firing count for an event
        callback, which is passed only firings. Called with no lock held.

        The first call, from the adding thread, skips the value if the update thread is
        calling the callback, as that thread has a newer value. The update thread
        queues its values for the adding thread while the first call runs."""
        if callback.event and value is not True:
            return
        if added:
            if not callback.lock.acquire(blocking=False):
                return
        elif not self._enter_on_update_thread(callback, version, value):
            return
        try:
            with self._update_lock:
                if self._ending is not None or not self._note_passed(callback, version):
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
                    self._callbacks = [x for x in self._callbacks if x is not callback]
                raise
        finally:
            callback.lock.release()

    def _enter_on_update_thread(
        self, callback: _Callback, version: int, value: object
    ) -> bool:
        """Take a callback's lock on the update thread. Returns false, without the lock,
        once the value is queued for the adding thread or the client is closing."""
        while not callback.lock.acquire(timeout=0.01):
            if self._manager.close_requested:
                return False
            with self._update_lock:
                if callback.adding:
                    callback.missed.append((version, value))
                    return False
        return True

    def _call_missed(self, callback: _Callback) -> None:
        """Call a callback, on the adding thread, with the values the update thread
        queued during its first call. A value queued before the stream ended is still
        passed on. Clears adding in the same hold of the update lock that finds the queue
        empty, so the update thread queues no value after the last check. Called with the
        callback's lock held."""
        while True:
            with self._update_lock:
                if not callback.missed:
                    callback.adding = False
                    return
                version, value = callback.missed.pop(0)
                if not self._note_passed(callback, version):
                    continue
            self._invoke(callback, value)

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
            replay = self._ending is None and fired > max(
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

    def remove_callback(self, fn: Callable[[object], None]) -> None:
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
) -> Optional[BaseException]:
    """Call end callbacks. Returns the first exception that propagates from one, for the
    caller to raise once it has finished ending or removing streams."""
    deferred: Optional[BaseException] = None
    for fn in callbacks:
        try:
            _invoke_callback(fn, ending)
        except BaseException as exn:  # pylint: disable=broad-except
            if deferred is None:
                deferred = exn
    return deferred


class StreamManager:  # pylint: disable=too-many-instance-attributes
    def __init__(self, client: Client) -> None:
        self._client = client
        self._update_lock = threading.RLock()
        self._condition = threading.Condition()
        self._streams: dict[int, StreamImpl] = {}
        self._callbacks: list[Callable[[], None]] = []
        self._closed = False
        # The error that closed the connection, if it failed
        self._close_cause: Optional[Exception] = None
        # Whether a thread is closing the connection
        self._close_requested = False
        # Streams out of the registry and not yet ended, with the ending for each. The
        # closing thread ends any whose condition the update thread stops trying to take.
        self._to_end: dict[StreamImpl, Exception] = {}
        # Every registered stream, to find one whose condition the closing thread holds
        self._registered: weakref.WeakSet[StreamImpl] = weakref.WeakSet()
        # The highest stream id the server has given this client, and those of its ids not
        # yet registered. A result for one of these, or for a higher id, can arrive before
        # its stream is registered, and is kept until it is. Only the latest value and an
        # ending are kept, as a stream registered later shows nothing else.
        self._issued = -1
        self._unregistered: set[int] = set()
        self._pending: dict[int, list[KRPC.StreamResult]] = {}

    @property
    def client(self) -> Client:
        return self._client

    @property
    def update_lock(self) -> threading.RLock:
        return self._update_lock

    @property
    def close_requested(self) -> bool:
        return self._close_requested

    def note_issued(self, stream_id: int) -> None:
        """Note a stream id the server has given this client. Called in the order the
        server's replies arrive."""
        with self._update_lock:
            self._issued = max(self._issued, stream_id)
            if stream_id not in self._streams:
                self._unregistered.add(stream_id)

    def note_removed(self, stream_id: int) -> None:
        """Note a stream id this client has asked the server to remove. A stream not yet
        registered for it is never registered, so its kept results are dropped."""
        with self._update_lock:
            self._issued = max(self._issued, stream_id)
            self._unregistered.discard(stream_id)
            self._pending.pop(stream_id, None)

    def add_stream(self, return_type: TypeBase, call: KRPC.ProcedureCall) -> StreamImpl:
        stream_id = self._client.krpc.add_stream(call).id
        return self.get_stream(return_type, stream_id)

    def get_stream(self, return_type: TypeBase, stream_id: int) -> StreamImpl:
        stream = StreamImpl(self, stream_id, return_type)
        undecodable = False
        # The kept results are stored under the new stream's condition and the update
        # lock, so an update for the stream lands after them
        with stream.condition:
            with self._update_lock:
                existing = self._streams.get(stream_id)
                if existing is not None:
                    return existing
                self._unregistered.discard(stream_id)
                pending = self._pending.pop(stream_id, [])
                if self._closed:
                    stream.end(_closed_error(self._close_cause))
                    return stream
                self._streams[stream_id] = stream
                self._registered.add(stream)
                for result in pending:
                    _, value, ended, undecodable = self._decode(stream, result)
                    if ended:
                        # A new stream has no end callbacks to call
                        assert isinstance(value, Exception)
                        stream.end(value)
                        del self._to_end[stream]
                        break
                    stream.set_value(value)
        if undecodable:
            self._remove_on_server(stream_id)
        return stream

    def remove_stream(self, stream: StreamImpl) -> None:
        with self._update_lock:
            stream_id = stream.stream_id
            on_server = self._streams.get(stream_id) is stream
            ending: Optional[Exception]
            if on_server:
                del self._streams[stream_id]
                ending = StreamError("The stream has been removed")
            else:
                # A stream with a decoded final result, or one the client is closing, is
                # ended with that ending. The server has no stream for it to remove.
                ending = self._to_end.get(stream)
        if ending is None:
            return
        # Ended first, so a value that arrives during the call is dropped, and a call that
        # fails leaves the stream ended
        callbacks = stream.end(ending)
        with self._update_lock:
            self._to_end.pop(stream, None)
        deferred = _call_end_callbacks(callbacks, ending)
        try:
            if on_server:
                self._client.krpc.remove_stream(stream_id)
        finally:
            if deferred is not None:
                raise deferred

    @property
    def update_condition(self) -> threading.Condition:
        return self._condition

    def wait_for_update(self, timeout: Optional[float] = None) -> None:
        self._condition.wait(timeout=timeout)

    @property
    def update_callbacks(self) -> List[Callable[[], None]]:
        with self._update_lock:
            return self._callbacks

    def add_update_callback(
        self, callback: Callable[[], None]
    ) -> List[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_update_callback(
        self, callback: Callable[[], None]
    ) -> List[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

    def request_close(self) -> None:
        """Note that the client is closing the connection. The update thread then stops
        trying to take a stream's condition, and the closing thread ends the stream."""
        self._close_requested = True

    def holds_a_condition(self) -> bool:
        """Whether this thread holds the update condition or the condition of a
        stream."""
        if self._condition._is_owned():  # type: ignore[attr-defined] # pylint: disable=protected-access
            return True
        with self._update_lock:
            streams = list(self._registered)
        return any(
            x.condition._is_owned()  # type: ignore[attr-defined] # pylint: disable=protected-access
            for x in streams
        )

    def notify_closed(
        self, on_update_thread: bool = False, cause: Optional[Exception] = None
    ) -> Optional[BaseException]:
        """End every stream, once the connection is closing and no further update is
        applied. Called by the closing thread and by the update thread. Returns the first
        exception that propagates from an end callback, for the caller to raise once it
        has finished closing.

        Each pass ends the streams whose condition it can take, and the passes repeat
        until every stream has ended. A thread can hold one stream's condition while it
        waits on another, and ending the other releases the first. The update thread
        leaves the remaining streams to the closing thread."""
        deferred: Optional[BaseException] = None
        with self._update_lock:
            if not self._closed:
                self._close_cause = cause
            self._closed = True
            ending = _closed_error(self._close_cause)
            for stream in self._streams.values():
                self._to_end[stream] = ending
            self._streams.clear()
            self._pending.clear()
            self._unregistered.clear()
        while True:
            with self._update_lock:
                for stream in [x for x in self._to_end if x.ended]:
                    del self._to_end[stream]
                streams = list(self._to_end.items())
                if not streams:
                    break
                if on_update_thread and self._close_requested:
                    break
            for stream, stream_ending in streams:
                exn = self._try_end(stream, stream_ending)
                if deferred is None:
                    deferred = exn
            # A thread holding a stream condition can be waiting for a stream update
            if self._condition.acquire(blocking=False):
                try:
                    self._condition.notify_all()
                finally:
                    self._condition.release()
        self._notify_update(on_update_thread)
        return deferred

    def _notify_update(self, on_update_thread: bool) -> None:
        """Wake the threads waiting for a stream update."""
        if on_update_thread:
            if not self._lock(self._condition):
                return
        else:
            self._condition.acquire()
        try:
            self._condition.notify_all()
        finally:
            self._condition.release()

    def _try_end(
        self, stream: StreamImpl, ending: Exception
    ) -> Optional[BaseException]:
        if stream.condition.acquire(timeout=0.01):
            return self._end(stream, ending)
        return None

    def _end(self, stream: StreamImpl, ending: Exception) -> Optional[BaseException]:
        """End a stream out of the registry, and release its condition. Returns the first
        exception that propagates from an end callback."""
        try:
            callbacks = stream.end(ending)
            with self._update_lock:
                self._to_end.pop(stream, None)
        finally:
            stream.condition.release()
        return _call_end_callbacks(callbacks, ending)

    def _lock(self, condition: threading.Condition) -> bool:
        """Acquire a stream's condition, or the update condition, on the update thread.
        Returns false, without it, once the client is closing, as the closing thread can
        hold it while it joins the update thread."""
        while not condition.acquire(timeout=0.05):
            if self._close_requested:
                return False
        return True

    def _end_on_update_thread(self, stream: StreamImpl, ending: Exception) -> None:
        """End a stream the server has ended. When the update thread stops trying to take
        its condition, the closing thread ends it."""
        if self._lock(stream.condition):
            self._end(stream, ending)

    def _decode(
        self, stream: StreamImpl, result: KRPC.StreamResult
    ) -> tuple[StreamImpl, object, bool, bool]:
        """Decode a result. A final result moves the stream from the registry to the
        streams to end, as the server has already removed it. Returns the stream, the
        value or ending, whether it ended, and whether it ended on a value that fails to
        decode, which the server still streams. Called with the update lock held."""
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
                ended = False
            except Exception as exn:  # pylint: disable=broad-except
                # A value that fails to decode ends only its stream
                value = exn
                undecodable = True
        if ended:
            del self._streams[result.id]
            assert isinstance(value, Exception)
            self._to_end[stream] = value
        return (stream, value, ended, undecodable)

    def _apply(self, decoded: Iterable[tuple[StreamImpl, object, bool, bool]]) -> None:
        """Store decoded values and endings, wake their waiters and call their
        callbacks. Called with no lock held."""
        for stream, value, ended, undecodable in decoded:
            if ended:
                assert isinstance(value, Exception)
                self._end_on_update_thread(stream, value)
                if undecodable:
                    self._remove_on_server(stream.stream_id)
                continue
            if not self._lock(stream.condition):
                continue
            try:
                with self._update_lock:
                    # The stream can be removed while its new value is being decoded. The
                    # ending it stored is kept.
                    if stream.ended:
                        continue
                    stream.set_value(value)
                    # Read with the value stored, so a callback added after this reads the
                    # value itself
                    callbacks = stream.callbacks
                    version = stream.version
                    fired = stream.fired
                stream.condition.notify_all()
            finally:
                stream.condition.release()
            for callback in callbacks:
                stream.call(callback, fired if callback.event else version, value)

    def _remove_on_server(self, stream_id: int) -> None:
        """Remove a stream the client has ended on a value that fails to decode. The call
        is made on its own thread, so a slow call on another thread does not hold up
        stream updates."""

        def remove() -> None:
            # The connection closing also removes the stream
            if self._closed:
                return
            try:
                self._client.krpc.remove_stream(stream_id)
            except Exception:  # pylint: disable=broad-except
                pass

        threading.Thread(target=remove, daemon=True).start()

    def update(self, results: Iterable[KRPC.StreamResult]) -> None:
        # The update lock is held only to find the streams and decode their new values, and
        # is released before any stream condition is taken. A thread waiting for an update
        # holds a condition and then needs the update lock. Taking the two in the opposite
        # order here deadlocks.
        decoded = []
        with self._update_lock:
            for result in results:
                stream = self._streams.get(result.id)
                if stream is None:
                    # A result for a stream not registered yet is kept. One for an id
                    # below those is for a stream that has ended.
                    if self._closed:
                        continue
                    if result.id > self._issued or result.id in self._unregistered:
                        self._keep(result)
                    continue
                decoded.append(self._decode(stream, result))
            update_callbacks = self._callbacks
        self._apply(decoded)

        self._notify_update(on_update_thread=True)
        for fn in update_callbacks:
            # Update callbacks have no stream ending to stop them once the client closes
            if self._closed:
                break
            _invoke_callback(fn)

    def _keep(self, result: KRPC.StreamResult) -> None:
        """Keep a result for a stream not registered yet. Called with the update lock
        held."""
        kept = self._pending.setdefault(result.id, [])
        # An ending is the last result kept
        if kept and kept[-1].result.HasField("error"):
            return
        if result.result.HasField("error"):
            kept.append(result)
        else:
            kept[:] = [result]


def _closed_error(cause: Optional[Exception] = None) -> Exception:
    """The error that ends a stream once the connection closes. Its cause is the error
    that closed the connection, if it failed."""
    error = ConnectionError("Connection closed")
    error.__cause__ = cause
    return error


def update_thread(
    manager: StreamManager, connection: Connection, stop: threading.Event
) -> None:
    _thread_state.update_thread = True
    cause: Optional[Exception] = None
    try:
        cause = _read_updates(manager, connection, stop)
    finally:
        # The connection is closed or failed, so no further update arrives
        manager.notify_closed(on_update_thread=True, cause=cause)


def _read_updates(
    manager: StreamManager, connection: Connection, stop: threading.Event
) -> Optional[Exception]:
    """Read stream updates until the client stops the thread, or the connection fails.
    Returns the error that ended the connection, if it failed."""
    while True:

        # Read the size and position of the update message
        data = b""
        while True:
            try:
                data += connection.partial_receive(1)
                size = Decoder.decode_message_size(data)
                break
            except IndexError:
                pass
            except Exception as exn:  # pylint: disable=broad-except
                # The socket closing as the client shuts down, or failing
                return exn
            if stop.is_set():
                connection.close()
                return None

        # Read and decode the update message
        try:
            data = connection.receive(size)
            update = cast(
                KRPC.StreamUpdate, Decoder.decode_message(data, KRPC.StreamUpdate)
            )
        except Exception as exn:  # pylint: disable=broad-except
            # A message that cannot be read leaves the connection unusable
            connection.close()
            return exn

        # Add the data to the cache
        manager.update(update.results)
