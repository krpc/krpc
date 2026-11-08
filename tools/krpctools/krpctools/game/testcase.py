"""The ``TestCase`` base class, for tests against any game.

A test class names the mods it requires in ``mods``. Before its ``setUpClass`` runs, the
game the run tests against is running with exactly those mods. Tolerance-based assertions
come from :class:`krpctools.game.assertions.AssertionsMixin`.
"""

import time
from typing import TYPE_CHECKING

from . import game
from .assertions import AssertionsMixin

if TYPE_CHECKING:
    import krpc.client


class TestCase(AssertionsMixin):
    # The mods this test class requires, by name, e.g. ["RemoteTech"]
    mods = []

    # Whether the class needs a running game. Tests of the framework itself set this False,
    # to run without one.
    game_required = True

    def __init_subclass__(cls, **kwargs):
        # Ensure the game before a subclass's setUpClass, which most tests override without
        # calling super()
        super().__init_subclass__(**kwargs)
        if not cls.game_required:
            return
        setup = cls.__dict__.get("setUpClass")
        if setup is None or getattr(setup.__func__, "_ensures_game", False):
            return
        raw = setup.__func__

        @classmethod
        def setUpClass(cls, _raw=raw):
            cls.ensure_game()
            _raw(cls)

        setUpClass.__func__._ensures_game = True  # pyright: ignore[reportFunctionMemberAccess]
        cls.setUpClass = setUpClass

    @classmethod
    def game(cls):
        """The game the run tests against."""
        return game.current()

    @classmethod
    def connect(cls, use_cached=True) -> "krpc.client.Client":
        return cls.game().connect(use_cached)

    @classmethod
    def ensure_game(cls):
        """Ensure the game is running with exactly the mods this class requires."""
        cls.game().ensure(cls.mods)

    @classmethod
    def setUpClass(cls):
        if cls.game_required:
            cls.ensure_game()

    @classmethod
    def wait(cls, timeout=0.1):
        time.sleep(timeout)

    @classmethod
    def wait_for_frame(cls):
        """Sleep for long enough that the game draws a frame at 10 fps or more, so its
        rendering code runs on the state set before the call."""
        cls.wait()

    @classmethod
    def wait_until(cls, predicate, timeout=30, message=None, interval=0.1):
        """Repeatedly call wait() until predicate() returns a truthy value, and
        return that value. Fail the test if timeout seconds elapse first.
        interval is the poll period, in seconds, passed to wait().

        Use this instead of an unbounded ``while not predicate(): self.wait()``
        loop so a condition that never holds fails the test with a clear
        message rather than hanging the suite forever."""
        deadline = time.time() + timeout
        while True:
            value = predicate()
            if value:
                return value
            if time.time() > deadline:
                raise cls.failureException(
                    "Timed out after %gs waiting for %s"
                    % (timeout, message or "condition")
                )
            cls.wait(interval)

    @classmethod
    def wait_while(cls, predicate, timeout=30, message=None, interval=0.1):
        """Repeatedly call wait() while predicate() returns a truthy value. Fail
        the test if timeout seconds elapse first. interval is the poll period,
        in seconds, passed to wait(). message should describe the state being
        waited for (i.e. the condition that ends the wait)."""
        return cls.wait_until(
            lambda: not predicate(),
            timeout=timeout,
            message=message,
            interval=interval,
        )
