from __future__ import annotations
from typing import cast, Callable, Iterable, List, Optional, TYPE_CHECKING
import threading
from krpc.stream import Stream
from krpc.types import TypeBase
from krpc.decoder import Decoder
from krpc.error import StreamError
from krpc.connection import Connection
import krpc.schema.KRPC_pb2 as KRPC
if TYPE_CHECKING:
    from krpc.client import Client


class StreamImpl:
    def __init__(self, client: Client, stream_id: int, return_type: TypeBase,
                 update_lock: threading.RLock):
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
            self._updated = True
            self._value = value

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

    def add_callback(self, callback: Callable[[object], None]) -> List[Callable[[object], None]]:
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_callback(self, callback: Callable[[object], None]) -> List[Callable[[object], None]]:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

    def remove(self) -> None:
        self._client._stream_manager.remove_stream(self._stream_id)
        with self._update_lock:
            self._value = StreamError("Stream does not exist")


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
                    self._client, stream_id, return_type, self._update_lock)
            return self._streams[stream_id]

    def get_stream(self, return_type: TypeBase, stream_id: int) -> StreamImpl:
        with self._update_lock:
            if stream_id not in self._streams:
                self._streams[stream_id] = StreamImpl(
                    self._client, stream_id, return_type, self._update_lock)
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

    def add_update_callback(self, callback: Callable[[], None]) -> List[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = self._callbacks[:]
            self._callbacks.append(callback)
            return self._callbacks

    def remove_update_callback(self, callback: Callable[[], None]) -> List[Callable[[], None]]:
        with self._update_lock:
            self._callbacks = [x for x in self._callbacks if x != callback]
            return self._callbacks

    def update(self, results: Iterable[KRPC.StreamResult]) -> None:
        with self._update_lock:
            for result in results:
                if result.id not in self._streams:
                    continue

                # Check for an error response
                if result.result.HasField('error'):
                    self._update_stream(
                        result.id,
                        self._client._build_error(result.result.error))
                    continue

                # Decode the return value and store it in the cache
                typ = self._streams[result.id].return_type
                value = Decoder.decode(self._client, result.result.value, typ)
                self._update_stream(result.id, value)
            with self._condition:
                self._condition.notify_all()
            for fn in self._callbacks:
                fn()

    def _update_stream(self, stream_id: int, value: object) -> None:
        stream = self._streams[stream_id]
        with stream.condition:
            stream.value = value
            stream.condition.notify_all()
        for fn in stream.callbacks:
            fn(value)


def update_thread(manager: StreamManager, connection: Connection, stop: threading.Event) -> None:
    while True:

        # Read the size and position of the update message
        data = b''
        while True:
            try:
                data += connection.partial_receive(1)
                size = Decoder.decode_message_size(data)
                break
            except IndexError:
                pass
            except:  # noqa pylint: disable=bare-except
                # TODO: is there a better way to catch exceptions when the
                #      thread is forcibly stopped (e.g. by CTRL+c)?
                return
            if stop.is_set():
                connection.close()
                return

        # Read and decode the update message
        data = connection.receive(size)
        update = cast(KRPC.StreamUpdate, Decoder.decode_message(data, KRPC.StreamUpdate))

        # Add the data to the cache
        manager.update(update.results)
