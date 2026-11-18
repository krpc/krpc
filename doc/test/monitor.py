"""Monitors that record the game while an example runs, and check how it went.

A monitor samples a set of streams on its own thread, from just before the example starts until
it ends. Its check then reads the recording, such as the altitude a flight reached or how
closely the auto-pilot held its target."""

from __future__ import annotations

import math
import threading
from collections.abc import Callable, Iterable
from dataclasses import dataclass
from typing import TYPE_CHECKING, Any

if TYPE_CHECKING:
    from doc.test.harness import ExampleTestCase as TestCase

# Samples per second of wall clock time
RATE = 10


@dataclass(frozen=True)
class Sample:
    values: dict[str, Any]

    def __getattr__(self, name: str) -> Any:
        try:
            return self.values[name]
        except KeyError:
            raise AttributeError(name) from None


class Monitor:
    """Records the streams that streams() returns, as one Sample per read."""

    def __init__(self, test: TestCase):
        self.test = test
        self.samples: list[Sample] = []
        self._streams: dict[str, Any] = {}
        self._stop = threading.Event()
        self._thread: threading.Thread | None = None
        self._error: BaseException | None = None
        # Set when recording fails, which stops the example
        self.failed = threading.Event()

    def streams(self) -> dict[str, Any]:
        """The streams to record, by name."""
        raise NotImplementedError

    def check(self) -> None:
        """Checks the recording, once the example has finished."""
        raise NotImplementedError

    def act(self, sample: Sample) -> None:
        """Acts on the game after each sample, such as to time warp."""

    def settle(self) -> None:
        """Waits, once the example has exited cleanly, for the game to reach the state the
        check reads, still recording."""

    def finish(self) -> None:
        """Undoes what act did, once the example has finished."""

    def summary(self) -> dict[str, Any]:
        """The measurements the check compares, shown when it fails."""
        return {}

    def start(self) -> None:
        self._streams = self.streams()
        self._thread = threading.Thread(target=self._record, daemon=True)
        self._thread.start()

    def stop(self) -> None:
        self._stop.set()
        if self._thread is not None:
            self._thread.join()
        try:
            for stream in self._streams.values():
                stream.remove()
        finally:
            self.finish()
        if self._error is not None:
            raise self._error

    def _record(self) -> None:
        try:
            while not self._stop.is_set():
                sample = Sample({name: s() for name, s in self._streams.items()})
                self.samples.append(sample)
                self.act(sample)
                self._stop.wait(1 / RATE)
        except BaseException as error:  # noqa: BLE001
            self._error = error
            self.failed.set()

    # Helpers for checks

    def describe(self) -> str:
        return "\n".join(
            "  %s = %s" % (name, _format(value))
            for name, value in self.summary().items()
        )

    def require(self, condition: bool, message: str) -> None:
        if not condition:
            self.test.fail(message + "\nFlight:\n" + self.describe())

    def first(self, predicate: Callable[[Sample], bool], start: int = 0) -> int | None:
        """The index of the first sample from start that matches."""
        return next(
            (i for i in range(start, len(self.samples)) if predicate(self.samples[i])),
            None,
        )

    def after(self, index: int, seconds: float) -> int:
        """The index of the first sample at least seconds of game time after index."""
        ut = self.samples[index].ut + seconds
        found = self.first(lambda s: s.ut >= ut, index)
        return len(self.samples) if found is None else found

    def between(self, start: int, end: int) -> list[Sample]:
        return self.samples[start:end]


def _format(value: Any) -> str:
    if isinstance(value, float):
        return "%.2f" % value
    if isinstance(value, tuple):
        return "(" + ", ".join(_format(v) for v in value) + ")"
    return str(value)


def angle_difference(a: float, b: float) -> float:
    """The difference between two angles, in degrees, in [0, 180]."""
    return abs((a - b + 180) % 360 - 180)


def magnitude(vector: Iterable[float]) -> float:
    return math.sqrt(sum(x * x for x in vector))
