from __future__ import annotations
from typing import Callable, Optional, TYPE_CHECKING
import threading
import time
from krpc.stream import Stream
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


class Event:
    """An event."""

    def __init__(self, client: Client, event: KRPC.Event) -> None:
        self._stream = Stream.from_stream_id(
            client, event.stream.id, client._types.bool_type
        )
        self._client = client
        self._seen = 0

    @property
    def condition(self) -> threading.Condition:
        """The condition variable for the event"""
        return self._stream.condition

    def wait(self, timeout: Optional[float] = None) -> bool:
        """Blocks until the event fires or a timeout occurs. Returns straight away if
        the event has fired since the last wait on this event object.
        The condition variable must be locked before calling this method.

        When timeout is not None, it should be a floating point number
        specifying the timeout in seconds for the operation. Returns true if the
        event fired, and false on a timeout. Raises the error that ended the event,
        if it ends before or during the wait."""
        stream = self._stream._stream
        seen = self._seen
        deadline = None if timeout is None else time.monotonic() + timeout
        while stream.fired <= seen:
            stream.raise_if_ended()
            if deadline is None:
                stream.condition.wait()
            else:
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    return False
                stream.condition.wait(remaining)
        self._seen = stream.fired
        return True

    def add_callback(self, callback: Callable[[], None]) -> None:
        """Add a callback that is invoked whenever the event fires. It is invoked
        straight away if the event has fired since the last wait on this event object,
        and the event's callbacks have not been invoked for that firing. A callback
        added after the event has ended is not invoked for an earlier firing. An error
        from the event does not invoke the callback."""
        with self._stream.condition:
            seen = self._seen
        self._stream._stream.add_event_callback(callback, seen)

    def remove_callback(self, callback: Callable[[], None]) -> None:
        """Remove a callback."""
        self._stream._stream.remove_callback(callback)

    def add_end_callback(self, callback: Callable[[Exception], None]) -> None:
        """Add a callback that is invoked once when the event ends, with the error that
        ended it. It is invoked straight away if the event has already ended."""
        self._stream.add_end_callback(callback)

    def remove_end_callback(self, callback: Callable[[Exception], None]) -> None:
        """Remove an end callback."""
        self._stream.remove_end_callback(callback)

    @property
    def stream(self) -> Stream:
        """The underlying stream for the event"""
        return self._stream

    def remove(self) -> None:
        """Remove the event from the server"""
        self._stream.remove()
