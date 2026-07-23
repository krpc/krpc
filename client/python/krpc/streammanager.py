from __future__ import annotations
from typing import cast, Callable, Iterable, List, Optional, TYPE_CHECKING
import sys
import threading
from krpc.stream import Stream
from krpc.types import TypeBase
from krpc.decoder import Decoder
from krpc.error import StreamError
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


class StreamImpl:
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
        self._condition = threading.Condition()
        self._callbacks: List[Callable[[object], None]] = []
        self._rate = 0.0

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
        self._client.krpc.set_stream_rate(self._stream_id, value)

    @property
    def started(self) -> bool:
        return self._started

    @property
    def value(self) -> object:
        if not self._updated:
            raise StreamError("Stream has no value")
        return self._value

    @value.setter
    def value(self, value: object) -> None:
        with self._update_lock:
            # Set the value before the flag that says there is one. A reader checks the
            # flag first and takes no lock, so the other order lets it see the flag set
            # and read the value that has not been stored yet.
            self._value = value
            self._updated = True

    @property
    def updated(self) -> bool:
        return self._updated

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

    def remove(self) -> None:
        self._client._stream_manager.remove_stream(self._stream_id)
        with self._condition:
            with self._update_lock:
                self._value = StreamError("Stream does not exist")
            # No further update will ever arrive for this stream, so anything waiting on it
            # has to be woken here or it waits forever. It sees the error above on waking.
            self._condition.notify_all()


class StreamManager:
    def __init__(self, client: Client) -> None:
        self._client = client
        self._update_lock = threading.RLock()
        self._condition = threading.Condition()
        self._streams: dict[int, StreamImpl] = {}
        self._callbacks: list[Callable[[], None]] = []

    def add_stream(self, return_type: TypeBase, call: KRPC.ProcedureCall) -> StreamImpl:
        stream_id = self._client.krpc.add_stream(call, False).id
        with self._update_lock:
            if stream_id not in self._streams:
                self._streams[stream_id] = StreamImpl(
                    self._client, stream_id, return_type, self._update_lock
                )
            return self._streams[stream_id]

    def get_stream(self, return_type: TypeBase, stream_id: int) -> StreamImpl:
        with self._update_lock:
            if stream_id not in self._streams:
                self._streams[stream_id] = StreamImpl(
                    self._client, stream_id, return_type, self._update_lock
                )
            return self._streams[stream_id]

    def remove_stream(self, stream_id: int) -> None:
        with self._update_lock:
            if stream_id in self._streams:
                self._client.krpc.remove_stream(stream_id)
                del self._streams[stream_id]

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
        """Wake everything waiting for a stream update, after the connection has closed and
        no further update can arrive."""
        with self._update_lock:
            streams = list(self._streams.values())
        for stream in streams:
            with stream.condition:
                stream.condition.notify_all()
        with self._condition:
            self._condition.notify_all()

    def update(self, results: Iterable[KRPC.StreamResult]) -> None:
        # The update lock is held only to find the streams and decode their new values, and
        # is released before any stream condition is taken. A thread waiting for an update
        # holds a condition and then needs the update lock - Event.wait resets the stream
        # value while holding it, as its documented use requires - so taking the two in the
        # opposite order here deadlocks. The callbacks are read under the lock for the same
        # reason: they run below without it held.
        decoded = []
        with self._update_lock:
            for result in results:
                if result.id not in self._streams:
                    continue

                # Check for an error response
                stream = self._streams[result.id]
                if result.result.HasField("error"):
                    value = self._client._build_error(result.result.error)
                elif result.result.is_null:
                    value = None
                else:
                    # Decode the return value
                    value = Decoder.decode(
                        self._client, result.result.value, stream.return_type
                    )
                decoded.append((result.id, stream, value, stream.callbacks))
            update_callbacks = self._callbacks

        # Store each value in the cache and notify anything waiting on it
        for stream_id, stream, value, callbacks in decoded:
            with stream.condition:
                with self._update_lock:
                    # The stream can be removed while its new value is being decoded, in
                    # which case remove() has already stored the error saying so and this
                    # value must not overwrite it - the stream is gone from the registry,
                    # so nothing would ever replace it and it would be returned forever.
                    if stream_id not in self._streams:
                        continue
                    stream.value = value
                stream.condition.notify_all()
            for fn in callbacks:
                _invoke_callback(fn, value)

        with self._condition:
            self._condition.notify_all()
        for fn in update_callbacks:
            _invoke_callback(fn)


def update_thread(
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
                # Any failure here (the socket closing as the client shuts down,
                # or the thread being interrupted) should just end the update thread.
                return
            if stop.is_set():
                connection.close()
                return

        # Read and decode the update message
        data = connection.receive(size)
        update = cast(
            KRPC.StreamUpdate, Decoder.decode_message(data, KRPC.StreamUpdate)
        )

        # Add the data to the cache
        manager.update(update.results)
