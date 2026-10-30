"""Threading requirements.

Each command runs on a named thread of the testee, and Client.start sends one without waiting
for its reply, so a test can have one thread wait or call while another removes or closes.
"""

import threading
import time

import pytest
import testee_pb2 as pb
from driver import profile, section, should, timing, values
from driver.errors import is_removed_error
from driver.framing import encode_sint32
from driver.helpers import counter, name, raised, round_trip, watched
from driver.scripted import ScriptedServer, definitions, is_call
from driver.testee import (
    REPLY_TIMEOUT,
    SETTLE,
    SHORT_WAIT,
    Callback,
    Stream,
    TesteeError,
    invocation,
    update_barrier,
    wait_until,
)

import krpc.schema.KRPC_pb2 as KRPC


def to_string(value):
    return invocation("TestService", "Int32ToString", values.sint32(value))


def block(milliseconds):
    return invocation("ConformanceService", "Block", values.sint32(milliseconds))


def single_result(callback):
    """The reply to the one command a callback ran."""
    (notification,) = [
        x
        for x in callback.wait_for(lambda xs: any(x.HasField("result") for x in xs))
        if x.HasField("result")
    ]
    return notification.result


def assert_closed(client):
    """Check that a call fails, as the connection is closed. The error a call after close
    raises is checked by test_connection.py::test_calls_after_end."""
    client.start("", invoke=to_string(1)).error()


section("Calls")


@profile("threads")
def test_calls_on_several_threads(connected):
    """Calls made from several threads on one connection each return their own
    result."""
    calls = [
        (i, connected.start("caller%d" % (i % 4), invoke=to_string(i)))
        for i in range(40)
    ]
    for i, call in calls:
        assert call.result() == str(i)


@profile("threads")
def test_interleaved_calls(client, server, scripted_server):
    """A call on one thread writes its request while a call on another thread waits for
    its response. Each call returns its own result, in whichever order the responses
    arrive."""
    # The server answers a client's requests in order, so a scripted server holds both
    # requests, then answers the second call before the first
    requests = []

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        while len(requests) < 2:
            message = scripted.receive()
            assert message is not None
            if message.WhichOneof("content") != "request":
                continue
            call = message.request.calls[0]
            if is_call(call, "Int32ToString"):
                requests.append(message.request)
            else:
                scripted.answer(message.request)
        by_argument = {x.calls[0].arguments[0].value: x for x in requests}
        for argument, text in ((encode_sint32(2), "fast"), (encode_sint32(1), "slow")):
            response = KRPC.Response(id=by_argument[argument].id)
            response.results.add(value=bytes([len(text)]) + text.encode())
            scripted.send(KRPC.Message(response=response))
        scripted.serve()

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    try:
        slow = client.start("slow", invoke=to_string(1))
        fast = client.start("fast", invoke=to_string(2))
        assert fast.result() == "fast"
        assert slow.result() == "slow"
    finally:
        client.close()


@profile("streams", "threads")
def test_streams_added_on_several_threads(connected, control, request):
    """Streams added from several threads at once are each registered."""
    names = ["%s%d" % (name(request), i) for i in range(4)]
    adds = [
        connected.start(
            "adder%d" % i,
            add_stream=pb.AddStream(
                invoke=invocation("ConformanceService", "Value", values.string(x))
            ),
        )
        for i, x in enumerate(names)
    ]
    streams = [Stream(connected, add.reply().stream) for add in adds]
    assert len({x.handle for x in streams}) == len(streams)
    for i, x in enumerate(names):
        control.conformance_service.set_value(x, i + 1)
    for i, stream in enumerate(streams):
        assert wait_until(lambda stream=stream, i=i: stream.get() == i + 1)


@profile("threads")
def test_close_with_a_call_in_flight(connected):
    """A thread closes the connection while another thread waits for a response. The
    waiting call raises the connection error."""
    slow = connected.start("slow", invoke=block(5000))
    time.sleep(SETTLE)
    start = time.perf_counter()
    connected.start("closer", close=pb.Close()).reply()
    assert slow.error().kind == pb.Error.CONNECTION
    # Well short of the 5 s the blocked call takes
    assert time.perf_counter() - start < 4


@profile("threads")
def test_response_after_close(client, server, scripted_server):
    """A response that arrives after the client sends its ``disconnect`` does not reach
    the call waiting for it. The call raises the connection error."""
    holding = threading.Event()

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        held = None
        while True:
            message = scripted.receive()
            assert message is not None
            case = message.WhichOneof("content")
            if case == "disconnect":
                break
            if case != "request":
                continue
            if is_call(message.request.calls[0], "Int32ToString"):
                held = message.request
                holding.set()
            else:
                scripted.answer(message.request)
        assert held is not None
        response = KRPC.Response(id=held.id)
        response.results.add(value=b"\x01x")
        scripted.send(KRPC.Message(response=response))
        scripted.close()

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    slow = client.start("slow", invoke=to_string(1))
    assert holding.wait(REPLY_TIMEOUT)
    client.start("closer", close=pb.Close()).reply()
    assert slow.error().kind == pb.Error.CONNECTION


section(
    "Conditions",
    "Each stream has a condition: a lock that a thread holds while it reads the stream's "
    "value and waits for the next one. While a thread holds it, the value the thread reads "
    "changes only when it waits, so the thread misses no update. The connection has a "
    "condition for its updates too.",
)


def held_elsewhere(connected, handle, stream):
    """A read of a stream on another thread, which takes the condition of the handle
    first."""
    return connected.start("other", condition=handle, get=pb.Get(stream=stream.handle))


def blocked(pending):
    """Whether a command started on another thread still waits, once it has had time to
    finish."""
    time.sleep(SETTLE)
    return not pending.done()


@profile("streams", "conditions")
def test_wait_holding(connected, request):
    """A thread holding a stream's condition sees one update per wait, and the value
    after the wait is the next value. While it holds the condition, a zero timeout
    returns false."""
    stream = counter(connected, name(request))
    with connected.holding(stream.handle):
        count = stream.get()
        # Each wait sees one update, as the next waits for the condition
        for _ in range(3):
            assert stream.wait()
            count += 1
            assert stream.get() == count
        assert stream.wait(0) is False
        assert stream.get() == count


@should
@profile("streams", "conditions")
def test_wait_for_update(connected, request):
    """The client offers a wait for the next update message of any stream, under a
    condition for the connection's updates. Holding that condition, a thread sees one
    update per wait."""
    stream = counter(connected, name(request))
    with connected.holding():
        connected.wait_for_update()
        count = stream.get()
        for _ in range(3):
            connected.wait_for_update()
            count += 1
            assert stream.get() == count
        connected.wait_for_update(0)
        assert stream.get() == count


@profile("streams", "conditions", "threads")
def test_held_value(connected, control, request):
    """While a thread holds the condition of a stream, the value it reads changes only
    when it waits. A thread that does not hold the condition reads the latest value. A
    wait returns for a value that arrived while the holder read the old one."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.holding(stream.handle):
        assert stream.get() == 0
        control.conformance_service.set_value(name(request), 5)
        assert wait_until(
            lambda: (
                connected.start("other", get=pb.Get(stream=stream.handle)).result() == 5
            )
        )
        assert stream.get() == 0
        assert stream.wait()
        assert stream.get() == 5


@profile("streams", "conditions", "threads")
def test_held_stream_ending(connected, control, request):
    """While a thread holds the condition of a stream, an error that ends the stream
    reaches it as a value does. The thread reads the old value until it waits. The wait
    returns, and later reads and waits raise the error."""
    stream = watched(connected, name(request))
    assert stream.get() == 0

    def ended_for_others():
        other = connected.start("other", get=pb.Get(stream=stream.handle))
        try:
            other.reply()
        except TesteeError:
            return True
        return False

    with connected.holding(stream.handle):
        control.conformance_service.fail_value(name(request), "Failed by the test")
        assert wait_until(ended_for_others)
        assert stream.get() == 0
        assert stream.wait()
        for read in (stream.get, stream.wait):
            assert "Failed by the test" in raised(read).message


@profile("streams", "conditions", "threads")
def test_held_stream_ending_before_value(client, server, scripted_server):
    """While a thread holds the condition of a stream with no value, an error ends the
    stream. A read on the thread raises the error."""

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    try:
        stream = counter(client, "scripted")
        with client.holding(stream.handle):
            update = KRPC.StreamUpdate()
            update.results.add(
                id=1,
                result=KRPC.ProcedureResult(
                    error=KRPC.Error(description="Failed by the test")
                ),
            )
            scripted.send(KRPC.Message(stream_update=update))
            # The response follows the update, so the client has stored the ending
            round_trip(client)
            assert "Failed by the test" in raised(stream.get).message
    finally:
        client.close()


@profile("streams", "conditions")
def test_wait_for_unread_value(connected, control, request):
    """A wait returns only for a value newer than the last one the thread read,
    including a value it read without holding the condition."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    control.conformance_service.set_value(name(request), 5)
    assert wait_until(lambda: stream.get() == 5)
    with connected.holding(stream.handle):
        assert not stream.wait(SHORT_WAIT)


@profile("streams", "conditions", "races")
def test_value_read_before_wake(connected, control, request):
    """A wait does not return for a value the thread read before the update woke the
    stream's waiters."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.paused(pb.STREAM_VALUE) as pause:
        control.conformance_service.set_value(name(request), 5)
        pause.wait()
        assert stream.get() == 5
        # Held before the update wakes the waiters, so the wait sees the wake
        with connected.holding(stream.handle):
            pause.resume()
            assert not stream.wait(SHORT_WAIT)


@profile("streams", "conditions")
def test_call_while_holding(connected, request):
    """A thread holding the condition of a stream makes a call, which returns."""
    stream = watched(connected, name(request))
    stream.get()
    call = connected.start("", condition=stream.handle, invoke=to_string(1))
    assert call.result() == "1"


@profile("streams", "conditions", "threads")
def test_wait_on_condition_object(connected, control, request):
    """A thread holding a stream's condition waits on the condition object itself, as the
    language waits on a condition variable. Other threads can take the condition while it
    waits. An update wakes the wait, and the thread then holds the condition again."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.holding(stream.handle):
        waiting = connected.start(
            "",
            wait_on_condition=pb.WaitOnCondition(
                stream=stream.handle, timeout=REPLY_TIMEOUT
            ),
        )
        time.sleep(SETTLE)
        assert held_elsewhere(connected, stream.handle, stream).result() == 0
        control.conformance_service.set_value(name(request), 1)
        assert waiting.result() is True
        other = held_elsewhere(connected, stream.handle, stream)
        assert blocked(other)
    assert other.result() == 1


@profile("streams", "events", "conditions", "threads")
@pytest.mark.parametrize("kind", ["stream", "event"])
def test_spurious_wake(connected, request, kind):
    """A wait with a timeout, woken by a notification of the condition with no update,
    waits on until the timeout and returns false."""
    timeout = 5 * SETTLE
    if kind == "stream":
        stream = watched(connected, name(request))
        stream.get()
        handle = stream.handle
        waiting = connected.start(
            "waiter", wait=pb.Wait(stream=handle, timeout=timeout)
        )
    else:
        event = connected.event(
            "ConformanceService", "CreateEvent", values.string(name(request))
        )
        handle = event.handle
        waiting = connected.start(
            "waiter", wait_event=pb.WaitEvent(event=handle, timeout=timeout)
        )
    time.sleep(SETTLE)
    connected.notify_condition(handle)
    assert blocked(waiting)
    assert waiting.result() is False


section("Sharing a Condition")


@profile("streams", "conditions", "threads")
def test_hold_twice(connected, request):
    """A thread holding the condition of a stream takes it again. Other threads wait for
    the condition until the thread releases every hold."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.holding(stream.handle):
        with connected.holding(stream.handle):
            other = held_elsewhere(connected, stream.handle, stream)
            assert blocked(other)
        assert blocked(other)
    assert other.result() == 0


@profile("streams", "conditions", "threads")
def test_wait_holding_twice(connected, control, request):
    """A thread that holds the condition of a stream twice waits on it. The wait returns
    for an update, or times out, and the thread then holds the condition twice again."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.holding(stream.handle):
        with connected.holding(stream.handle):
            control.conformance_service.set_value(name(request), 5)
            assert stream.wait()
            assert stream.get() == 5
            assert not stream.wait(SHORT_WAIT)
            other = held_elsewhere(connected, stream.handle, stream)
            assert blocked(other)
        assert blocked(other)
    assert other.result() == 5


@profile("streams", "conditions", "threads")
def test_copies_share_condition(connected, request):
    """Copies of a stream handle share one condition. A thread holds it through each copy
    at once, and other threads wait for it until the thread releases both holds."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    copy = stream.copy()
    with connected.holding(stream.handle):
        other = held_elsewhere(connected, copy.handle, stream)
        assert blocked(other)
        with connected.holding(copy.handle):
            assert copy.get() == 0
        assert blocked(other)
    assert other.result() == 0


@profile("events", "conditions", "threads")
def test_event_shares_condition(connected, request):
    """An event and its stream share one condition. A thread holding it through the event
    keeps other threads from taking it through the stream."""
    event = connected.event(
        "ConformanceService", "CreateEvent", values.string(name(request))
    )
    stream = event.stream
    with connected.holding(event.handle):
        other = held_elsewhere(connected, stream.handle, stream)
        assert blocked(other)
    assert other.result() is False
    event.remove()


@profile("events", "conditions", "threads")
@pytest.mark.parametrize("case", ["event-then-stream", "stream-then-event", "copy"])
def test_release_through_another_handle(connected, request, case):
    """A thread lets go of a condition through a handle other than the one it took the
    condition through, which shares the condition: the event's stream for an event, the
    event for its stream, or a copy of a stream handle. Other threads can then take the
    condition."""
    event = connected.event(
        "ConformanceService", "CreateEvent", values.string(name(request))
    )
    stream = event.stream
    taken, released = {
        "event-then-stream": (event.handle, stream.handle),
        "stream-then-event": (stream.handle, event.handle),
        "copy": (stream.handle, stream.copy().handle),
    }[case]
    with connected.holding(taken, release=released):
        other = held_elsewhere(connected, stream.handle, stream)
        assert blocked(other)
    assert other.result() is False
    event.remove()


section("Removing and Closing")


@profile("streams", "conditions")
def test_remove_while_holding(connected, request):
    """A thread holding the condition of a stream removes the stream, while updates for
    the stream arrive. A wait by that thread then raises the error for a removed
    stream."""
    stream = counter(connected, name(request))
    with connected.holding(stream.handle):
        assert stream.wait()
        # The next update arrives while the thread holds the condition
        time.sleep(SETTLE)
        stream.remove()
        assert is_removed_error(connected, raised(stream.wait))
    assert is_removed_error(connected, raised(stream.get))


@profile("events", "conditions")
def test_remove_event_while_holding(connected):
    """A thread holding the condition of an event removes the event, while the event
    fires. A wait by that thread then raises the error for a removed stream, at once."""
    event = connected.event(
        "TestService", "OnTimer", values.uint32(10), values.uint32(1000)
    )
    with connected.holding(event.handle):
        assert event.wait()
        # The next firing arrives while the thread holds the condition
        time.sleep(SETTLE)
        event.remove()
        assert is_removed_error(connected, raised(event.wait))


@profile("streams", "conditions")
@pytest.mark.parametrize("condition", ["stream", "updates"])
def test_close_while_holding(connected, request, condition):
    """A thread holding the condition of a stream, or the condition for the connection's
    updates, closes the connection while updates for the stream arrive. A wait on the
    stream by that thread then raises the connection error, and the stream's end
    callbacks are called once."""
    stream = counter(connected, name(request))
    ending = stream.add_end_callback()
    with connected.holding(stream.handle if condition == "stream" else 0):
        if condition == "stream":
            assert stream.wait()
        else:
            connected.wait_for_update()
        # The next update arrives while the thread holds the condition
        time.sleep(SETTLE)
        connected.close()
        assert raised(stream.wait).kind == pb.Error.CONNECTION
    assert_closed(connected)
    assert len(ending.wait_for_calls(1)) == 1


@profile("streams", "conditions", "threads")
def test_wait_holding_another(connected, control, request):
    """A thread holding the condition of one stream waits on a second stream. The wait
    returns on the second stream's next update, when an update of the first stream
    arrives before it."""
    held = watched(connected, name(request) + "held")
    waited = watched(connected, name(request) + "waited")
    held.get()
    waited.get()
    connected.start(
        "holder", hold_condition=pb.HoldCondition(stream=held.handle)
    ).reply()
    try:
        waiting = connected.start("holder", wait=pb.Wait(stream=waited.handle))
        time.sleep(SETTLE)
        control.conformance_service.set_value(name(request) + "held", 1)
        time.sleep(SETTLE)
        control.conformance_service.set_value(name(request) + "waited", 1)
        assert waiting.result() is True
    finally:
        connected.start("holder", release_condition=pb.ReleaseCondition()).reply()


@profile("streams", "conditions", "threads")
@pytest.mark.parametrize("end", ["close", "connection-failure"])
def test_wake_waiter_holding_another(connected, control, request, end):
    """A close or a failure of the connection wakes two threads: one waiting on a stream
    while it holds the condition of a second stream, and one waiting on that second
    stream. Both waits raise the connection error. The wait on the second stream raises
    once the first thread lets go of its condition."""
    held = watched(connected, name(request) + "held")
    waited = watched(connected, name(request) + "waited")
    held.get()
    waited.get()
    held_waiting = connected.start("waiter", wait=pb.Wait(stream=held.handle))
    time.sleep(SETTLE)
    # The waiter lets go of the condition while it waits, so the holder can take it
    connected.start(
        "holder", hold_condition=pb.HoldCondition(stream=held.handle)
    ).reply()
    waiting = connected.start("holder", wait=pb.Wait(stream=waited.handle))
    time.sleep(SETTLE)
    if end == "close":
        connected.close()
    else:
        control.conformance_service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
    try:
        assert waiting.error().kind == pb.Error.CONNECTION
    finally:
        # Released however the wait ends, so a failure leaves no thread holding it
        connected.start("holder", release_condition=pb.ReleaseCondition()).reply()
    assert held_waiting.error().kind == pb.Error.CONNECTION


@profile("streams", "conditions", "threads")
@pytest.mark.parametrize("end", ["close", "connection-failure"])
def test_wake_update_waiter(connected, control, request, end):
    """A close or a failure of the connection wakes a thread waiting for an update while
    it holds the condition of a stream. The wait returns or raises the connection error,
    and a read of the stream then raises the connection error."""
    stream = watched(connected, name(request))
    stream.get()
    waiting = connected.start(
        "waiter", stream.handle, wait_for_update=pb.WaitForUpdate()
    )
    time.sleep(SETTLE)
    if end == "close":
        connected.close()
    else:
        control.conformance_service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
    # The wait returns, or raises the connection error
    reply = waiting.reply()
    assert not reply.HasField("error") or reply.error.kind == pb.Error.CONNECTION
    assert raised(stream.get).kind == pb.Error.CONNECTION


def wait_on_stream(connected, control, request):
    """A wait on a stream on the waiter thread, and a check that the stream runs on."""
    stream = watched(connected, name(request))
    stream.get()
    waiting = connected.start("waiter", wait=pb.Wait(stream=stream.handle))

    def runs_on():
        control.conformance_service.set_value(name(request), 1)
        return wait_until(lambda: stream.get() == 1)

    return waiting, runs_on


def wait_on_event(connected, control, request):
    """A wait on an event on the waiter thread, and a check that the event runs on."""
    event = connected.event(
        "ConformanceService", "CreateEvent", values.string(name(request))
    )
    waiting = connected.start("waiter", wait_event=pb.WaitEvent(event=event.handle))

    def runs_on():
        control.conformance_service.trigger_event(name(request))
        return event.wait()

    return waiting, runs_on


@profile("streams", "events", "threads")
@pytest.mark.parametrize(
    "wait_on", [wait_on_stream, wait_on_event], ids=["stream", "event"]
)
def test_interrupted_wait(connected, control, request, wait_on):
    """Interrupting a thread that waits on a stream or an event, such as with
    ``Thread.interrupt`` in Java, ends the wait with an error. The stream or event runs
    on."""
    waiting, runs_on = wait_on(connected, control, request)
    time.sleep(SETTLE)
    connected.interrupt("waiter")
    waiting.error()
    assert runs_on()


section("Callbacks and Conditions")


@profile("streams", "callbacks", "conditions")
def test_remove_callback_while_holding(connected, request):
    """A thread holding the condition of a stream removes a callback of the stream,
    while updates for the stream arrive. The remove returns, and a later wait returns for
    the next update."""
    stream = counter(connected, name(request))
    callback = stream.add_callback()
    with connected.holding(stream.handle):
        assert stream.wait()
        # The next update arrives while the thread holds the condition
        time.sleep(SETTLE)
        callback.remove()
    assert stream.wait()


@profile("streams", "callbacks", "conditions")
@pytest.mark.parametrize("held_stream", ["live", "removed"])
def test_close_while_callback_waits(connected, request, held_stream):
    """A thread holding the condition of a stream closes the connection, while a
    callback of another stream waits to take that condition. The stream held can be one
    the client removed. The close returns, and the connection is closed."""
    held = watched(connected, name(request) + "held")
    held.get()
    if held_stream == "removed":
        held.remove()
    stream = counter(connected, name(request))
    wait = pb.Command(wait=pb.Wait(stream=held.handle, timeout=0.01))
    callback = stream.add_callback(pb.Behavior(run=wait))
    callback.wait_for_calls(2)
    with connected.holding(held.handle):
        # The callback's next call waits for the condition
        time.sleep(SETTLE)
        connected.close()
    assert_closed(connected)


@profile("streams", "callbacks", "conditions")
def test_add_callback_while_callback_waits(connected, request):
    """A thread holding the condition of a stream adds a callback to another stream,
    while a callback of that stream waits to take the condition. The add returns."""
    held = watched(connected, name(request) + "held")
    held.get()
    stream = counter(connected, name(request))
    wait = pb.Command(wait=pb.Wait(stream=held.handle, timeout=0.01))
    # The first call can run on the adding thread, so only later calls wait
    waiting = stream.add_callback(pb.Behavior(run=wait, skip=1))
    with connected.holding(held.handle):
        # A call that starts once the condition is held waits for it
        count = len(waiting.calls)
        waiting.wait_for_calls(count + 1)
        added = stream.add_callback()
    added.remove()
    waiting.remove()


section("Callback Threads")


@profile("streams", "callbacks", "threads")
def test_callbacks_run_off_the_caller(connected, control, request):
    """Callbacks for stream updates run on a thread of the client, and never on a thread
    that makes calls. The first call of a callback, with the value the stream already
    has, may run on the thread that adds it."""
    stream = watched(connected, name(request))
    # The first call, with the value the stream already has, may run on the adding thread
    stream.get()
    callback = stream.add_callback()
    callback.wait_for_calls(1)
    callers = {
        connected.start(thread, invoke=to_string(1)).reply().thread
        for thread in ("", "a", "b")
    }
    # Each update is waited for, as a callback is passed only the latest value
    for value in (1, 2):
        control.conformance_service.set_value(name(request), value)
        callback.wait_for_calls(value + 1)
    later = [x for x in callback.notifications if not x.returned][1:]
    assert not {x.thread for x in later} & callers


@profile("streams", "callbacks")
def test_first_call_waits(connected, request):
    """The first call of a callback can wait for a later update of its stream. The wait
    returns, whether the call runs on the thread that adds the callback or on a thread of
    the client."""
    stream = counter(connected, name(request))
    stream.get()
    wait = pb.Command(wait=pb.Wait(stream=stream.handle, timeout=5))
    callback = stream.add_callback(pb.Behavior(run=wait, only=1))
    assert values.native(single_result(callback).value) is True


@profile("streams", "callbacks", "threads")
def test_blocked_callback(connected, control, request):
    """Calls return, and reads of a stream return its latest value, while a callback
    blocks. Waits and other callbacks can stall. The callback is passed the latest value
    once it returns."""
    # A wait can stall while a callback blocks, so the other stream has its first value
    # before the callback blocks
    other = watched(connected, name(request) + "other")
    assert other.get() == 0
    blocked = watched(connected, name(request) + "blocked")
    # The first call may run on the adding thread, so a thread of its own adds the callback
    callback, adding = blocked.start_callback("adder", pb.Behavior(block=True))
    try:
        callback.wait_for_calls(1)
        control.conformance_service.set_value(name(request) + "other", 5)
        control.conformance_service.set_value(name(request) + "blocked", 7)
        assert wait_until(lambda: other.get() == 5)
        assert wait_until(lambda: blocked.get() == 7)
        assert connected.invoke("TestService", "Int32ToString", values.sint32(1)) == "1"
    finally:
        # A release for each call that can have blocked, with some to spare
        for _ in range(10):
            callback.release()
    adding.reply()
    assert wait_until(lambda: callback.calls[-1:] == [7])


@profile("streams", "callbacks")
@timing
def test_callback_one_thread_at_a_time(connected, request):
    """A callback never runs on two threads at once."""
    stream = counter(connected, name(request))
    callback = stream.add_callback(pb.Behavior(sleep=0.05, report_return=True))
    notifications = callback.wait_for(lambda xs: len(xs) >= 10)
    # Each call returns before the next starts
    returns = [x.returned for x in notifications]
    pairs = len(returns) // 2
    assert returns[: 2 * pairs] == [False, True] * pairs


@profile("streams", "callbacks", "threads", "races")
def test_close_during_first_call(connected, control, request):
    """Closing the connection returns while an update of a stream waits for the first
    call of a callback to finish."""
    stream = watched(connected, name(request))
    stream.get()
    with connected.paused(pb.FIRST_CALL_STARTED) as pause:
        _, adding = stream.start_callback("adder")
        pause.wait()
        control.conformance_service.set_value(name(request), 1)
        # The update waits for the first call
        time.sleep(SETTLE)
        connected.close()
    adding.reply()


@profile("streams", "callbacks", "threads", "races")
def test_update_callback_after_close(connected, control, request):
    """An update callback is not called once the connection is closed, though the update
    arrived before the close."""
    callback = connected.add_update_callback()
    stream = watched(connected, name(request))
    stream.get()
    callback.wait_for_calls(1)
    # The update thread finishes with the first value, so the pause is for the next one
    time.sleep(SETTLE)
    count = len(callback.calls)
    with connected.paused(pb.UPDATE_CALLBACKS) as pause:
        control.conformance_service.set_value(name(request), 1)
        pause.wait()
        closing = connected.start("closer", close=pb.Close())
        # The close ends the streams, and waits for the update thread
        time.sleep(SETTLE)
    closing.reply()
    assert len(callback.calls) == count


section("Calls From Callbacks")


@profile("streams", "callbacks")
def test_call_from_a_callback(connected, request):
    """A callback makes a call, which returns its result."""
    stream = watched(connected, name(request))
    callback = stream.add_callback(pb.Behavior(run=pb.Command(invoke=to_string(7))))
    result = single_result(callback)
    assert result.WhichOneof("result") == "value"
    assert values.native(result.value) == "7"


@profile("streams", "callbacks")
def test_remove_stream_from_callback(connected):
    """A callback removes the stream it is called for. The remove returns, and a read of
    the stream then raises the error for a removed stream."""
    # The first value arrives after ten of the server's updates, so the first call of the
    # callback runs on the thread of the client that runs callbacks
    stream = connected.add_stream(
        "TestService", "BlockingProcedure", values.sint32(10), values.sint32(0)
    )
    remove = pb.Command(remove=pb.Remove(stream=stream.handle))
    callback = stream.add_callback(pb.Behavior(run=remove))
    assert single_result(callback).WhichOneof("result") == "value"
    assert is_removed_error(connected, raised(stream.get))


@profile("streams", "callbacks")
def test_remove_callback_from_itself(connected, control, request):
    """A callback removes itself, and is not called again."""
    stream = watched(connected, name(request))
    stream.get()
    callback = Callback(connected.testee, connected.new_callback(), "remove_callback")
    remove = pb.Command(remove_callback=pb.RemoveCallback(callback=callback.id))
    # The first call can run before the add returns, when the callback is not yet known
    # to remove, so the callback removes itself on its second call
    connected.testee.run(
        add_callback=pb.AddCallback(
            stream=stream.handle,
            callback=callback.id,
            behavior=pb.Behavior(skip=1, run=remove),
        )
    )
    callback.wait_for_calls(1)
    control.conformance_service.set_value(name(request), 1)
    assert single_result(callback).WhichOneof("result") == "value"
    # A second callback shows when the next update has arrived
    later = stream.add_callback()
    control.conformance_service.set_value(name(request), 2)
    assert wait_until(lambda: 2 in later.calls)
    update_barrier(connected, control, name(request) + "barrier")
    assert callback.calls == [0, 1]


@profile("streams", "callbacks")
@pytest.mark.parametrize("call", ["first", "later"])
def test_close_from_a_callback(connected, control, request, call):
    """A callback closes the connection, from its first call or a later one. The end
    callbacks of the stream are called."""
    stream = watched(connected, name(request))
    stream.get()
    ending = stream.add_end_callback()
    callback = stream.add_callback(
        pb.Behavior(
            run=pb.Command(close=pb.Close()),
            report_return=True,
            skip=0 if call == "first" else 1,
        )
    )
    if call == "later":
        control.conformance_service.set_value(name(request), 1)
    callback.wait_for(lambda xs: any(x.returned for x in xs))
    assert_closed(connected)
    assert len(ending.wait_for_calls(1)) == 1


@profile("streams", "callbacks")
@pytest.mark.parametrize("end", ["remove", "error"])
def test_close_from_an_end_callback(connected, control, request, end):
    """An end callback closes the connection, whether the stream was removed or ended
    with an error. The end callbacks of the other streams are called."""
    stream = watched(connected, name(request))
    stream.get()
    other = watched(connected, name(request) + "other")
    other.get()
    other_ending = other.add_end_callback()
    callback = stream.add_end_callback(
        pb.Behavior(run=pb.Command(close=pb.Close()), report_return=True)
    )
    if end == "error":
        # The server ends the stream, so the callback runs on a thread no command is on
        control.conformance_service.fail_value(name(request), "Failed by the test")
    else:
        # The removal can race the close for the connection, and lose with the error for a
        # call after close
        try:
            stream.remove()
        except TesteeError as exn:
            assert exn.error.kind == pb.Error.CONNECTION, exn.error
    callback.wait_for(lambda xs: any(x.returned for x in xs))
    assert_closed(connected)
    assert len(other_ending.wait_for_calls(1)) == 1
