import threading
from typing import Callable, Any, Optional, TYPE_CHECKING

if TYPE_CHECKING:
    from krpc import Client
    from krpc.types import TypeBase


class Stream:
    """ A streamed remote procedure call. When called, returns the
        most recently received result of the call. """

    def __init__(self, stream):
        self._stream = stream

    @classmethod
    def from_stream_id(cls, conn, stream_id, return_type):
        # type: (Client, int, TypeBase) -> Stream
        """ Create a stream from an existing stream id on the server """
        stream = conn._stream_manager.get_stream(return_type, stream_id)
        return cls(stream)

    @classmethod
    def from_call(cls, conn, return_type, call):
        # type: (Set[float], TypeBase, Any) -> Stream
        """ Create a stream from a remote procedure call """
        stream = conn._stream_manager.add_stream(return_type, call)
        return cls(stream)

    def start(self, wait=True):
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
    def rate(self):
        # type: () -> float
        """ The update rate for the stream in Hertz.
            Zero if the rate is unlimited. """
        return self._stream.rate

    @rate.setter
    def rate(self, value):
        # type: (float) -> None
        """ The update rate for the stream in Hertz.
            Zero if the rate is unlimited. """
        self._stream.rate = value

    def __call__(self):
        """ Get the most recent value for this stream. """
        if not self._stream.started:
            self.start()
        value = self._stream.value
        if isinstance(value, Exception):
            raise value  # pylint: disable=raising-bad-type
        return value

    @property
    def condition(self):
        # type: () -> threading.Condition
        """ Condition variable that is notified when the stream updates. """
        return self._stream.condition

    def wait(self, timeout=None):
        # type: (Optional[float]) -> None
        """ Wait until the next stream update or a timeout occurs.
            The condition variable must be locked before calling this method.

            When timeout is not None, it should be a floating point number
            specifying the timeout in seconds for the operation. """
        if not self._stream.started:
            self._stream.start()
        self._stream.condition.wait(timeout=timeout)

    def add_callback(self, callback):
        # type: (Callable) -> None
        """ Add a callback that is invoked whenever the stream is updated. """
        self._stream.add_callback(callback)

    def remove_callback(self, callback):
        # type: (Callable) -> None
        """ Remove a callback. """
        self._stream.remove_callback(callback)

    def remove(self):
        """ Remove the stream """
        self._stream.remove()
