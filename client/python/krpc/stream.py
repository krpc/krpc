from __future__ import annotations
import threading
from typing import Any, Callable, Optional, TYPE_CHECKING
from krpc.types import TypeBase
import krpc.schema.KRPC_pb2 as KRPC
if TYPE_CHECKING:
    from krpc.client import Client
    from krpc.streammanager import StreamImpl


class Stream:
    """ A streamed remote procedure call. When called, returns the
        most recently received result of the call. """

    def __init__(self, stream: StreamImpl) -> None:
        self._stream = stream

    @classmethod
    def from_stream_id(cls, client: Client, stream_id: int,
                       return_type: TypeBase) -> Stream:
        """ Create a stream from an existing stream id on the server """
        stream = client._stream_manager.get_stream(return_type, stream_id)
        return cls(stream)

    @classmethod
    def from_call(cls, client: Client, return_type: TypeBase, call: KRPC.ProcedureCall) -> Stream:
        """ Create a stream from a remote procedure call """
        stream = client._stream_manager.add_stream(return_type, call)
        return cls(stream)

    def start(self, wait: bool = True) -> None:
        """ Start the stream. If wait is true,
            blocks until the stream has received its first update. """
        if self._stream.started:
            return
        if not wait:
            self._stream.start()
        else:
            with self._stream.condition:
                self._stream.start()
                self._stream.condition.wait()

    @property
    def rate(self) -> float:
        """ The update rate for the stream in Hertz.
            Zero if the rate is unlimited. """
        return self._stream.rate

    @rate.setter
    def rate(self, value: float) -> None:
        """ The update rate for the stream in Hertz.
            Zero if the rate is unlimited. """
        self._stream.rate = value

    def __call__(self):  # type: ignore[no-untyped-def]
        """ Get the most recent value for this stream. """
        if not self._stream.started:
            self.start()
        value = self._stream.value
        if isinstance(value, Exception):
            raise value
        return value

    @property
    def condition(self) -> threading.Condition:
        """ Condition variable that is notified when the stream updates. """
        return self._stream.condition

    def wait(self, timeout: Optional[float] = None) -> None:
        """ Wait until the next stream update or a timeout occurs.
            The condition variable must be locked before calling this method.

            When timeout is not None, it should be a floating point number
            specifying the timeout in seconds for the operation. """
        if not self._stream.started:
            self._stream.start()
        self._stream.condition.wait(timeout=timeout)

    def add_callback(self, callback: Callable[[Any], None]) -> None:
        """ Add a callback that is invoked whenever the stream is updated. """
        self._stream.add_callback(callback)

    def remove_callback(self, callback: Callable[[Any], None]) -> None:
        """ Remove a callback. """
        self._stream.remove_callback(callback)

    def remove(self) -> None:
        """ Remove the stream """
        self._stream.remove()
