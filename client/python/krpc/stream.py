from __future__ import annotations

import threading
import time
from collections.abc import Callable
from typing import TYPE_CHECKING, Any

import krpc.schema.KRPC_pb2 as KRPC
from krpc.types import TypeBase

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

    def __call__(self) -> Any:
        """Get the most recent value for this stream. Blocks until the first value
        arrives. Raises the error that ended the stream, if it has ended."""
        stream = self._stream
        if not stream.updated:
            with stream.condition:
                while not stream.updated and not stream.ended:
                    stream.condition.wait()
        return stream.value

    @property
    def condition(self) -> threading.Condition:
        """Condition variable that is notified when the stream updates."""
        return self._stream.condition

    def wait(self, timeout: float | None = None) -> bool:
        """Wait until the next stream update or a timeout occurs.
        The condition variable must be locked before calling this method.

        When timeout is not None, it should be a floating point number
        specifying the timeout in seconds for the operation. Returns true if the
        stream updated, and false on a timeout. Raises the error that ended the
        stream, if it ends before or during the wait."""
        stream = self._stream
        stream.raise_if_ended()
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
        """Add a callback that is invoked whenever the stream is updated. It is
        invoked straight away with the value the stream already has, if any. An
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
