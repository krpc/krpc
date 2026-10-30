"""Event requirements.

Most tests use an event from ConformanceService.CreateEvent, which fires once per
TriggerEvent and ends on RemoveEvent, so each test chooses its firings.
"""

import math
import time

import pytest
import testee_pb2 as pb
from driver import profile, section, should, values
from driver.errors import is_removed_error
from driver.helpers import ended, raised, round_trip, single_ending
from driver.testee import (
    AT_ONCE,
    REPLY_TIMEOUT,
    SETTLE,
    SHORT_WAIT,
    update_barrier,
    wait_for_control,
    wait_until,
)


class Fixture:
    """An event the control connection fires, and a stream on the control connection of
    the number of times it has fired."""

    def __init__(self, client, control, name):
        self.client = client
        self.control = control
        self.service = control.conformance_service
        self.name = name
        self.event = client.event(
            "ConformanceService", "CreateEvent", values.string(name)
        )
        self.firings = control.add_stream(self.service.event_firings, name)
        self.triggered = 0

    def trigger(self, count=1):
        """Fire the event, and wait until the client has received the firings."""
        for _ in range(count):
            self.service.trigger_event(self.name)
        self.triggered += count
        # The server writes a firing in the pass that counts it, so a response to a later
        # call follows the firing
        wait_for_control(self.firings, lambda firings: firings >= self.triggered)
        round_trip(self.client)

    def end(self):
        self.service.remove_event(self.name)


@pytest.fixture
def fixture(connected, control, request):
    fixture = Fixture(connected, control, request.node.nodeid)
    yield fixture
    fixture.firings.remove()


section("Getting Events")


@profile("events")
def test_event(fixture):
    """A procedure that returns an event gives the program an event. Waiting on it
    returns when it fires."""
    fixture.service.trigger_event(fixture.name)
    assert fixture.event.wait()


@profile("events")
def test_event_stream(fixture):
    """The stream of an event is a stream of bool. Its value is false until the event
    fires, and true after a firing."""
    stream = fixture.event.stream
    assert stream.get() is False
    fixture.trigger()
    assert stream.get() is True


section("Handles")


@profile("events")
def test_equality(fixture):
    """Copies of an event handle are equal. Events from two calls of one procedure are
    not."""
    other = fixture.client.event(
        "ConformanceService", "CreateEvent", values.string(fixture.name + "-other")
    )
    assert fixture.client.compare_handles(fixture.event, fixture.event.copy())
    assert not fixture.client.compare_handles(fixture.event, other)


@profile("events")
def test_hash(fixture):
    """Copies of an event handle hash equal, in a language that hashes handles."""
    copy = fixture.event.copy()
    assert fixture.client.hash_handle(fixture.event) == fixture.client.hash_handle(copy)


section("Callbacks")


@profile("events", "callbacks")
def test_callback(fixture):
    """A callback is called once for each firing after it is added."""
    callback = fixture.event.add_callback()
    for count in (1, 2, 3):
        fixture.trigger()
        assert len(callback.wait_for_calls(count)) == count
    round_trip(fixture.client)
    assert len(callback.calls) == 3


@profile("events", "callbacks")
def test_remove_callback(fixture):
    """A removed callback is not called again."""
    removed = fixture.event.add_callback()
    kept = fixture.event.add_callback()
    fixture.trigger()
    assert len(removed.wait_for_calls(1)) == 1
    removed.remove()
    fixture.trigger()
    assert len(kept.wait_for_calls(2)) == 2
    assert len(removed.calls) == 1


section(
    "Unseen Firings",
    "A firing is unseen until a wait on the event returns for it or one of its callbacks "
    "is called for it. A callback added after an unseen firing is called for it, which "
    "replays the firing. A remove or a close discards an unseen firing, and an end by "
    "the server keeps it.",
)


@profile("events", "callbacks")
@pytest.mark.parametrize("taker", ["wait", "callback"])
def test_callback_after_seen_firing(fixture, taker):
    """A callback added once a firing is seen, by a wait or by a callback, is not called
    for that firing."""
    if taker == "wait":
        fixture.trigger()
        assert fixture.event.wait()
    else:
        # A callback takes the firing as a wait does
        first = fixture.event.add_callback()
        fixture.trigger()
        assert len(first.wait_for_calls(1)) == 1
    callback = fixture.event.add_callback()
    round_trip(fixture.client)
    assert callback.calls == []
    fixture.trigger()
    assert len(callback.wait_for_calls(1)) == 1


@profile("events", "callbacks")
@pytest.mark.parametrize("state", ["live", "ended"])
def test_callback_after_unseen_firing(fixture, state):
    """A callback added after an unseen firing is called once for it. This holds after the
    server has ended the event too."""
    fixture.trigger()
    if state == "ended":
        fixture.end()
        stream = fixture.event.stream
        assert wait_until(lambda: ended(stream))
    callback = fixture.event.add_callback()
    assert len(callback.wait_for_calls(1)) == 1
    round_trip(fixture.client)
    assert len(callback.calls) == 1


@should
@profile("events", "callbacks")
def test_first_call_on_add(fixture):
    """Adding a callback after an unseen firing calls the callback before the add
    returns."""
    fixture.trigger()
    assert len(fixture.event.add_callback().calls) == 1


@profile("events", "callbacks")
def test_one_callback_per_unseen_firing(fixture):
    """Of two callbacks added after an unseen firing, only one is called for it."""
    fixture.trigger()
    first = fixture.event.add_callback()
    second = fixture.event.add_callback()
    assert len(first.wait_for_calls(1)) == 1
    round_trip(fixture.client)
    assert len(first.calls) + len(second.calls) == 1


@profile("events", "callbacks")
def test_stream_callback_leaves_firing(fixture):
    """A callback on the event's stream leaves the firing unseen, so a callback added to the
    event later is called for it."""
    value_callback = fixture.event.stream.add_callback()
    fixture.trigger()
    assert wait_until(lambda: value_callback.calls[-1:] == [True])
    callback = fixture.event.add_callback()
    assert len(callback.wait_for_calls(1)) == 1


@profile("events", "callbacks")
def test_removed_callback_leaves_firing(fixture):
    """A callback removed before a firing leaves the firing unseen, so a callback added
    later is called for it."""
    removed = fixture.event.add_callback()
    removed.remove()
    fixture.trigger()
    callback = fixture.event.add_callback()
    assert len(callback.wait_for_calls(1)) == 1
    assert removed.calls == []


@profile("events", "callbacks", "threads", "races")
def test_callback_added_during_replay(fixture):
    """Of two callbacks added after an unseen firing, only one is called for it, though the
    second is added while the firing is replayed to the first."""
    fixture.trigger()
    with fixture.client.paused(pb.REPLAY) as pause:
        first, adding = fixture.event.start_callback("adder")
        pause.wait()
        second = fixture.event.add_callback()
    adding.reply()
    round_trip(fixture.client)
    assert len(first.calls) + len(second.calls) == 1


@profile("events", "callbacks", "threads", "races")
def test_callback_removed_during_add(fixture):
    """A callback added after an unseen firing, and removed before the replay calls it,
    leaves the firing unseen, so a callback added later is called for it."""
    fixture.trigger()
    with fixture.client.paused(pb.REPLAY) as pause:
        removed, adding = fixture.event.start_callback("adder")
        pause.wait()
        removed.remove()
    adding.reply()
    callback = fixture.event.add_callback()
    assert len(callback.wait_for_calls(1)) == 1
    assert removed.calls == []


@profile("events", "callbacks", "races")
def test_callback_removed_during_update(fixture):
    """A callback removed while an update passes a firing to the event's callbacks leaves
    the firing unseen, so a callback added later is called for it."""
    removed = fixture.event.add_callback()
    assert fixture.event.stream.get() is False
    # The update thread finishes with the first value, so the pause is for the firing
    update_barrier(fixture.client, fixture.control, fixture.name + "barrier")
    with fixture.client.paused(pb.STREAM_CALLBACKS) as pause:
        fixture.service.trigger_event(fixture.name)
        pause.wait()
        removed.remove()
    update_barrier(fixture.client, fixture.control, fixture.name + "barrier")
    callback = fixture.event.add_callback()
    assert len(callback.wait_for_calls(1)) == 1
    assert removed.calls == []


section("Ending")


@profile("events")
def test_server_end(fixture):
    """Waiting on an event the server ends returns for an unseen firing first. Later waits
    raise the error for an event the server ended: ``KRPC.StreamRemovedException``, or the
    stream error."""
    fixture.service.trigger_event(fixture.name)
    fixture.end()
    # The firing is seen before the end
    assert fixture.event.wait()
    for wait in (fixture.event.wait, lambda: fixture.event.wait(0.1)):
        assert is_removed_error(fixture.client, raised(wait))


@profile("events")
def test_ended_without_firing(fixture):
    """Waiting on an event that ends without firing raises the error that ended it."""
    fixture.end()
    assert is_removed_error(fixture.client, raised(fixture.event.wait))


@profile("events")
@pytest.mark.parametrize("through", ["event", "stream"])
@pytest.mark.parametrize("firing", ["none", "unseen"])
def test_remove(fixture, firing, through):
    """Waiting on an event the client removed, through the event or through its stream,
    raises the error for a removed stream, at once. An unseen firing is discarded."""
    if firing == "unseen":
        fixture.trigger()
    if through == "event":
        fixture.event.remove()
    else:
        fixture.event.stream.remove()
    start = time.perf_counter()
    assert is_removed_error(fixture.client, raised(fixture.event.wait))
    assert time.perf_counter() - start < AT_ONCE


@profile("events", "callbacks")
def test_drop_last_handle(fixture):
    """An event runs while the program holds a handle to it. Callbacks added through a copy
    of the handle keep running after the copy is dropped. Dropping the last handle removes
    the event: the server stops sending its firings, and its callbacks are not called again."""
    copy = fixture.event.copy()
    callback = copy.add_callback()
    copy.drop()
    fixture.trigger()
    assert len(callback.wait_for_calls(1)) == 1
    fixture.event.drop()
    round_trip(fixture.client)
    # The server counts a firing only once it sends it
    fired = fixture.firings()
    fixture.service.trigger_event(fixture.name)
    # A later pass of the server, then a round trip that follows its control updates
    update_barrier(fixture.client, fixture.control, fixture.name + "barrier")
    fixture.service.value(fixture.name + "barrier")
    assert fixture.firings() == fired
    assert len(callback.calls) == 1


@profile("events")
def test_wait_after_close(fixture):
    """Waiting on an event after the connection closes raises the connection error."""
    fixture.client.close()
    assert raised(fixture.event.wait).kind == pb.Error.CONNECTION


section("Waiting")


@profile("events")
def test_firing_before_wait(fixture):
    """A firing before the first wait is unseen, so the wait returns at once."""
    fixture.trigger()
    assert fixture.event.wait(0)


@profile("events")
def test_firings_coalesce(fixture):
    """Several firings before a wait make it return once."""
    fixture.trigger(2)
    assert fixture.event.wait(0)
    assert not fixture.event.wait(SHORT_WAIT)


@profile("events")
def test_repeated_waits(fixture):
    """Repeated waits on a repeating event return once for each firing."""
    for _ in range(3):
        fixture.service.trigger_event(fixture.name)
        assert fixture.event.wait()
    assert not fixture.event.wait(SHORT_WAIT)


@profile("events")
@pytest.mark.parametrize("case", ["elapses", "firing"])
def test_wait_timeout(fixture, case):
    """Waiting with a timeout returns false when the timeout elapses before a firing, and
    true on a firing."""
    if case == "elapses":
        start = time.perf_counter()
        assert not fixture.event.wait(SHORT_WAIT)
        assert time.perf_counter() - start >= SHORT_WAIT
        return
    testee = fixture.client.testee
    waiting = testee.send(
        wait_event=pb.WaitEvent(event=fixture.event.handle, timeout=REPLY_TIMEOUT)
    )
    fixture.service.trigger_event(fixture.name)
    reply = testee.reply(waiting, 2 * REPLY_TIMEOUT)
    assert values.native(reply.value) is True


@profile("events")
@pytest.mark.parametrize("timeout", ["zero", "infinite", "negative", "nan"])
def test_wait_timeout_values(fixture, timeout):
    """A zero timeout returns at once, with true when a firing is pending. An infinite
    timeout waits with no deadline, as a wait without a timeout does. A negative or NaN
    timeout raises the argument error."""
    if timeout == "zero":
        start = time.perf_counter()
        assert fixture.event.wait(0) is False
        assert time.perf_counter() - start < AT_ONCE
        fixture.trigger()
        assert fixture.event.wait(0) is True
        return
    testee = fixture.client.testee
    if timeout in ("negative", "nan"):
        seconds = -1.0 if timeout == "negative" else math.nan
        waiting = testee.send(
            wait_event=pb.WaitEvent(event=fixture.event.handle, timeout=seconds)
        )
        error = raised(lambda: testee.reply(waiting, REPLY_TIMEOUT))
        assert error.kind == pb.Error.ARGUMENT, error
        return
    waiting = testee.send(
        wait_event=pb.WaitEvent(event=fixture.event.handle, timeout=math.inf)
    )
    # A wait that returns at once replies before the firing
    time.sleep(SETTLE)
    fixture.service.trigger_event(fixture.name)
    reply = testee.reply(waiting, REPLY_TIMEOUT)
    assert values.native(reply.value) is True


def start_wait(fixture, thread):
    return fixture.client.start(
        thread, wait_event=pb.WaitEvent(event=fixture.event.handle)
    )


@profile("events", "threads")
def test_two_waiters(fixture):
    """Two threads waiting on one event both return when it fires."""
    first = start_wait(fixture, "first")
    second = start_wait(fixture, "second")
    time.sleep(SETTLE)
    fixture.service.trigger_event(fixture.name)
    assert first.result() is True
    assert second.result() is True


@profile("events", "threads")
@pytest.mark.parametrize("end", ["remove", "close", "connection-failure"])
def test_wake_waiter(fixture, request, end):
    """A thread waiting on an event wakes when the event is removed, the connection closes
    or the connection fails. The wait raises the error for a removed stream, or the
    connection error, with the reason for a failure."""
    waiting = start_wait(fixture, "waiter")
    time.sleep(SETTLE)
    if end == "remove":
        fixture.event.remove()
        assert is_removed_error(fixture.client, waiting.error())
    elif end == "close":
        fixture.client.close()
        assert waiting.error().kind == pb.Error.CONNECTION
    else:
        fixture.service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
        error = waiting.error()
        assert error.kind == pb.Error.CONNECTION
        assert error.reason == "SERVER_STOPPED"


section("End Callbacks")


@profile("events", "callbacks")
@pytest.mark.parametrize("end", ["server", "remove", "close", "connection-failure"])
def test_end_callback(fixture, request, end):
    """An end callback is called once when the event ends: by the server, by a remove,
    or by a close or failure of the connection. The callback receives the error a wait on
    the event then raises: the error for an event the server ended, the error for a
    removed stream, or the connection error. An event that ends without firing calls
    none of its callbacks."""
    ended = fixture.event.add_end_callback()
    fired = fixture.event.add_callback()
    if end == "server":
        fixture.end()
    elif end == "remove":
        fixture.event.remove()
    elif end == "close":
        fixture.client.close()
    else:
        fixture.service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
    ending = single_ending(ended)
    assert ending.error is not None
    if end == "server":
        assert is_removed_error(fixture.client, ending.error)
    elif end == "remove":
        assert is_removed_error(fixture.client, ending.error)
    else:
        assert ending.error.kind == pb.Error.CONNECTION
    if end in ("server", "remove"):
        round_trip(fixture.client)
        assert len(ended.calls) == 1
        assert fired.calls == []
