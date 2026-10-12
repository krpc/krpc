from __future__ import annotations
import threading
import time
from typing import Any, Callable, Optional, TYPE_CHECKING
from krpc.types import TypeBase
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client
    from krpc.streammanager import StreamImpl


class Stream:
    """A streamed remote procedure call. When called, returns the
    most recently received result of the call."""

    def __init__(self, stream: StreamImpl) -> None:
        self._stream = stream

    @classmethod
    def from_stream_id(
        cls, client: Client, stream_id: int, return_type: TypeBase
    ) -> Stream:
        """Create a stream from an existing stream id on the server"""
        stream = client._stream_manager.get_stream(return_type, stream_id)
        return cls(stream)

    @classmethod
    def from_call(
        cls, client: Client, return_type: TypeBase, call: KRPC.ProcedureCall
    ) -> Stream:
        """Create a stream from a remote procedure call"""
        stream = client._stream_manager.add_stream(return_type, call)
        return cls(stream)

    def start(self, wait: bool = True) -> None:
        """Start the stream. If wait is true,
        blocks until the stream has received its first update.
        Raises the error that ended the stream, if it ends before or during the wait."""
        stream = self._stream
        stream.raise_if_ended()
        if stream.started:
            return
        if not wait:
            stream.start()
        else:
            with stream.condition:
                stream.start()
                while not stream.updated and not stream.ended:
                    stream.condition.wait()
                stream.raise_if_ended()

    @property
    def rate(self) -> float:
        """The update rate for the stream in Hertz.
        Zero if the rate is unlimited."""
        return self._stream.rate

    @rate.setter
    def rate(self, value: float) -> None:
        """The update rate for the stream in Hertz.
        Zero if the rate is unlimited."""
        self._stream.rate = value

    def __call__(self):  # type: ignore[no-untyped-def]
        """Get the most recent value for this stream. Raises the error that ended the
        stream, if it has ended."""
        self._stream.raise_if_ended()
        if not self._stream.started:
            self.start()
        return self._stream.value

    @property
    def condition(self) -> threading.Condition:
        """Condition variable that is notified when the stream updates."""
        return self._stream.condition

    def wait(self, timeout: Optional[float] = None) -> bool:
        """Wait until the next stream update or a timeout occurs.
        The condition variable must be locked before calling this method.

        When timeout is not None, it should be a floating point number
        specifying the timeout in seconds for the operation. Returns true if the
        stream updated, and false on a timeout. Raises the error that ended the
        stream, if it ends before or during the wait."""
        stream = self._stream
        stream.raise_if_ended()
        if not stream.started:
            stream.start()
        version = stream.version
        deadline = None if timeout is None else time.monotonic() + timeout
        while stream.version == version:
            stream.raise_if_ended()
            if deadline is None:
                stream.condition.wait()
            else:
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    return False
                stream.condition.wait(remaining)
        return True

    def add_callback(self, callback: Callable[[Any], None]) -> None:
        """Add a callback that is invoked whenever the stream is updated. An
        error from the stream does not invoke the callback."""
        self._stream.add_callback(callback)

    def remove_callback(self, callback: Callable[[Any], None]) -> None:
        """Remove a callback."""
        self._stream.remove_callback(callback)

    def add_end_callback(self, callback: Callable[[Exception], None]) -> None:
        """Add a callback that is invoked once when the stream ends, with the error
        that ended it. It is invoked straight away if the stream has already ended."""
        self._stream.add_end_callback(callback)

    def remove_end_callback(self, callback: Callable[[Exception], None]) -> None:
        """Remove an end callback."""
        self._stream.remove_end_callback(callback)

    def remove(self) -> None:
        """Remove the stream"""
        self._stream.remove()
