from __future__ import annotations
from typing import Callable, Optional, TYPE_CHECKING
import threading
from krpc.stream import Stream
import krpc.schema.KRPC_pb2 as KRPC
if TYPE_CHECKING:
    from krpc.client import Client


class Event:
    """ An event. """
    def __init__(self, client: Client, event: KRPC.Event) -> None:
        self._stream = Stream.from_stream_id(
            client, event.stream.id, client._types.bool_type)
        self._client = client
        self._callback_mapping: \
            dict[Callable[[], None], Callable[[bool], None]] = {}

    def start(self) -> None:
        """ Start the underlying stream for the event """
        self._stream.start(False)

    @property
    def condition(self) -> threading.Condition:
        """ The condition variable for the event """
        return self._stream.condition

    def wait(self, timeout: Optional[float] = None) -> None:
        """ Blocks until the event is triggered or a timeout occurs.
            The condition variable must be locked before calling this method.

            When timeout is not None, it should be a floating point number
            specifying the timeout in seconds for the operation. """
        if not self.stream._stream.started:
            self.start()
        self.stream._stream.value = False
        while not self.stream():
            orig_value = self.stream()
            self.stream.wait(timeout=timeout)
            if timeout is not None and self.stream() == orig_value:
                # Value did not change, must have timed out
                return

    def add_callback(self, callback: Callable[[], None]) -> None:
        def callback_wrapper(x: bool) -> None:
            if x:
                callback()
        self._callback_mapping[callback] = callback_wrapper
        self._stream.add_callback(callback_wrapper)

    def remove_callback(self, callback: Callable[[], None]) -> None:
        if callback in self._callback_mapping:
            callback_wrapper = self._callback_mapping[callback]
            del self._callback_mapping[callback]
            self._stream.remove_callback(callback_wrapper)

    @property
    def stream(self) -> Stream:
        """ The underlying stream for the event """
        return self._stream

    def remove(self) -> None:
        """ Remove the event from the server """
        self._stream.remove()
