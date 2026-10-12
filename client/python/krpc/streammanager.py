from __future__ import annotations
from typing import cast, Callable, Iterable, List, Optional, TYPE_CHECKING
import sys
import threading
from krpc.stream import Stream
from krpc.types import TypeBase
from krpc.decoder import Decoder
from krpc.error import ConnectionError, StreamError  # pylint: disable=redefined-builtin
from krpc.connection import Connection
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


def _invoke_callback(fn: Callable[..., None], *args: object) -> None:
    """Run a stream callback, reporting anything it raises rather than letting it
    propagate. It runs on the update thread, which has no caller to propagate to and
    would end if it escaped, stopping every stream on the connection from updating
    again. Report it through the thread excepthook, so it is visible by default and an
    application can route it elsewhere."""
    try:
        fn(*args)
    except Exception:  # pylint: disable=broad-except
        threading.excepthook(
            threading.ExceptHookArgs(sys.exc_info() + (threading.current_thread(),))
        )


class StreamImpl:  # pylint: disable=too-many-instance-attributes
    """The state shared by every handle of one stream. It is changed under the stream's
    condition, and then the update lock."""

    def __init__(
        self,
        client: Client,
        stream_id: int,
        return_type: TypeBase,
        update_lock: threading.RLock,
    ):
        self._client = client
        self._stream_id = stream_id
        self._return_type = return_type
        self._update_lock = update_lock
        self._started = False
        self._updated = False
        self._value: Optional[object] = None
        self._version = 0
        self._fired = 0
        self._ending: Optional[Exception] = None
        self._condition = threading.Condition()
        self._callbacks: List[Callable[[object], None]] = []
        self._end_callbacks: List[Callable[[Exception], None]] = []
        self._rate = 0.0

    @property
    def stream_id(self) -> int:
        return self._stream_id

    @property
    def return_type(self) -> TypeBase:
        return self._return_type

    def start(self) -> None:
        if not self._started:
            self._client.krpc.start_stream(self._stream_id)
            self._started = True

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
    def started(self) -> bool:
        return self._started

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
            # Set the value before the flag that says there is one. A reader checks the
            # flag first and takes no lock, so the other order lets it see the flag set
            # and read the value that has not been stored yet.
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
    def callbacks(self) -> List[Callable[[object], None]]:
        with self._update_lock:
            return self._callbacks

    def add_callback(
        self, callback: Callable[[object], None]
    ) -> List[Callable[[object], None]]:
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_callback(
        self, callback: Callable[[object], None]
    ) -> List[Callable[[object], None]]:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

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
        self._client._stream_manager.remove_stream(self)


def _end_streams(streams: Iterable[StreamImpl], ending: Exception) -> None:
    for stream in streams:
        for fn in stream.end(ending):
            _invoke_callback(fn, ending)


class StreamManager:
    def __init__(self, client: Client) -> None:
        self._client = client
        self._update_lock = threading.RLock()
        self._condition = threading.Condition()
        self._streams: dict[int, StreamImpl] = {}
        self._callbacks: list[Callable[[], None]] = []
        self._closed = False

    def add_stream(self, return_type: TypeBase, call: KRPC.ProcedureCall) -> StreamImpl:
        stream_id = self._client.krpc.add_stream(call, False).id
        return self.get_stream(return_type, stream_id)

    def get_stream(self, return_type: TypeBase, stream_id: int) -> StreamImpl:
        with self._update_lock:
            if stream_id not in self._streams:
                stream = StreamImpl(
                    self._client, stream_id, return_type, self._update_lock
                )
                if self._closed:
                    stream.end(_closed_error())
                    return stream
                self._streams[stream_id] = stream
            return self._streams[stream_id]

    def remove_stream(self, stream: StreamImpl) -> None:
        with self._update_lock:
            stream_id = stream.stream_id
            if self._streams.get(stream_id) is not stream:
                return
            del self._streams[stream_id]
        # Ended first, so a value that arrives during the call is dropped, and a call that
        # fails leaves the stream ended
        _end_streams([stream], StreamError("The stream has been removed"))
        self._client.krpc.remove_stream(stream_id)

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

    def notify_closed(self) -> None:
        """End every stream, after the connection has closed and no further update can
        arrive."""
        with self._update_lock:
            self._closed = True
            streams = list(self._streams.values())
            self._streams.clear()
        _end_streams(streams, _closed_error())
        with self._condition:
            self._condition.notify_all()

    def update(self, results: Iterable[KRPC.StreamResult]) -> None:
        # The update lock is held only to find the streams and decode their new values, and
        # is released before any stream condition is taken. A thread waiting for an update
        # holds a condition and then needs the update lock. Taking the two in the opposite
        # order here deadlocks. The callbacks are read under the lock for the same reason:
        # they run below without it held.
        decoded = []
        with self._update_lock:
            for result in results:
                stream = self._streams.get(result.id)
                if stream is None:
                    continue
                value: object
                ended = True
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
                # A final result ends the stream, and the server has already removed it
                if ended:
                    del self._streams[result.id]
                decoded.append((stream, value, ended, stream.callbacks))
            update_callbacks = self._callbacks

        for stream, value, ended, callbacks in decoded:
            if ended:
                assert isinstance(value, Exception)
                _end_streams([stream], value)
                continue
            with stream.condition:
                with self._update_lock:
                    # The stream can be removed while its new value is being decoded. The
                    # ending it stored is kept.
                    if stream.ended:
                        continue
                    stream.set_value(value)
                stream.condition.notify_all()
            for fn in callbacks:
                _invoke_callback(fn, value)

        with self._condition:
            self._condition.notify_all()
        for fn in update_callbacks:
            _invoke_callback(fn)


def _closed_error() -> Exception:
    return ConnectionError("Connection closed")


def update_thread(
    manager: StreamManager, connection: Connection, stop: threading.Event
) -> None:
    try:
        _read_updates(manager, connection, stop)
    finally:
        # The connection is closed or failed, so no further update arrives
        manager.notify_closed()


def _read_updates(
    manager: StreamManager, connection: Connection, stop: threading.Event
) -> None:
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
            except:  # noqa pylint: disable=bare-except
                # The socket closing as the client shuts down, or failing
                return
            if stop.is_set():
                connection.close()
                return

        # Read and decode the update message
        try:
            data = connection.receive(size)
            update = cast(
                KRPC.StreamUpdate, Decoder.decode_message(data, KRPC.StreamUpdate)
            )
        except:  # noqa pylint: disable=bare-except
            # A message that cannot be read leaves the connection unusable
            connection.close()
            return

        # Add the data to the cache
        manager.update(update.results)
