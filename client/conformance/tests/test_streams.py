"""Stream requirements.

Most tests stream ConformanceService.Value, which changes only when the control connection sets
it, so each test chooses its updates. A stream sends an update only when its value changes.
"""

import math
import time
from typing import NamedTuple

import pytest
import testee_pb2 as pb
from driver import profile, section, should, timing, values
from driver.errors import (
    assert_error_kind,
    assert_native_error,
    error_types,
    is_removed_error,
)
from driver.framing import encode_sint32
from driver.helpers import (
    counter,
    create,
    ended,
    name,
    raised,
    round_trip,
    single_ending,
    watched,
)
from driver.scripted import ScriptedServer, definitions
from driver.testee import (
    AT_ONCE,
    REPLY_TIMEOUT,
    SETTLE,
    SHORT_WAIT,
    TesteeError,
    update_barrier,
    wait_for_control,
    wait_until,
)

import krpc.schema.KRPC_pb2 as KRPC


def member_stream(connected, member):
    """A stream of a member of the given kind, and the value it holds."""
    if member == "procedure":
        stream = connected.add_stream("TestService", "Int32ToString", values.sint32(42))
        return stream, "42"
    if member == "service-property":
        connected.invoke("TestService", "set_StringProperty", values.string("foo"))
        return connected.add_stream("TestService", "get_StringProperty"), "foo"
    if member == "static-method":
        stream = connected.add_stream(
            "TestService",
            "TestClass_static_StaticMethod",
            values.string("a"),
            values.string("b"),
        )
        return stream, "staticab"
    obj = values.obj(create(connected))
    if member == "method":
        stream = connected.add_stream("TestService", "TestClass_GetValue", obj)
        return stream, "value=alpha"
    if member == "class-property":
        connected.invoke(
            "TestService", "TestClass_set_IntProperty", obj, values.sint32(42)
        )
        return connected.add_stream("TestService", "TestClass_get_IntProperty", obj), 42
    stream = connected.add_stream(
        "TestServiceExtensions", "TestClass_get_ExtensionProperty", obj
    )
    return stream, "value=alpha"


def stream_values(connected, kind):
    """A stream of a value of the given kind, and the value it holds."""
    if kind == "null-object":
        stream = connected.add_stream("TestService", "EchoTestObject", values.null())
        return stream, None
    if kind == "null-int":
        return (
            connected.add_stream("TestService", "EchoNullableInt", values.null()),
            None,
        )
    if kind == "null-string":
        stream = connected.add_stream(
            "TestService", "EchoNullableString", values.null()
        )
        return stream, None
    if kind == "nullable-int":
        stream = connected.add_stream(
            "TestService", "EchoNullableInt", values.sint32(42)
        )
        return stream, 42
    if kind == "enum":
        enum = values.enum("TestService", "TestEnum", 2)
        return connected.add_stream("TestService", "EnumEcho", enum), values.Enum(2)
    if kind == "struct":
        struct = values.struct(
            "TestService",
            "TestStruct",
            values.sint32(1),
            values.string("alpha"),
            values.enum("TestService", "TestEnum", 2),
            values.list_of([values.sint32(1)]),
        )
        stream = connected.add_stream("TestService", "StructEcho", struct)
        return stream, values.Struct((1, "alpha", values.Enum(2), [1]))
    if kind == "list-of-nullable-ints":
        argument = values.list_of([values.sint32(1), values.null(), values.sint32(3)])
        stream = connected.add_stream("TestService", "EchoListOfNullableInts", argument)
        return stream, [1, None, 3]
    obj = values.obj(create(connected))
    argument = values.list_of([obj, values.null()])
    stream = connected.add_stream("TestService", "EchoListOfNullableObjects", argument)
    return stream, [obj.object.id, None]


class Thrown(NamedTuple):
    """The procedures that throw an exception: for the first value, for a later value, and
    the procedure that resets the later one. Then the exception's description."""

    first: str
    later: str
    reset: str
    message: str


EXCEPTIONS = {
    "InvalidOperationException": Thrown(
        "ThrowInvalidOperationException",
        "ThrowInvalidOperationExceptionLater",
        "ResetInvalidOperationExceptionLater",
        "Invalid operation",
    ),
    "CustomException": Thrown(
        "ThrowCustomException",
        "ThrowCustomExceptionLater",
        "ResetCustomExceptionLater",
        "A custom kRPC exception",
    ),
}


def assert_exception(connected, control, error, exception):
    """The error is the one a call raises for the server's exception: the native type for a
    mapped exception, the client's type for a custom one, and a message that starts with
    the description."""
    message = EXCEPTIONS[exception].message
    if exception == "CustomException":
        assert error.kind == pb.Error.RPC
        if error_types(connected):
            assert (error.service, error.name) == ("TestService", "CustomException")
    else:
        assert_native_error(connected, control, error, exception)
    assert message in error.message
    if error_types(connected):
        assert error.message.startswith(message), error.message


@pytest.fixture
def scripted(client, server, scripted_server):
    """The client connected to a scripted server that answers its calls. The server's
    definitions of KRPC and TestService answer GetServices, and stream ids count from 1.
    """

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    yield scripted
    if client.testee.running:
        client.close()


def scripted_counter(client):
    return counter(client, "scripted")


section("Adding Streams")


@profile("streams")
@pytest.mark.parametrize(
    "member",
    [
        "procedure",
        "service-property",
        "method",
        "static-method",
        "class-property",
        "extension-member",
    ],
)
def test_members(connected, member):
    """The client streams each kind of member that returns a value: a procedure, a
    service property, a method, a static method, a class property, and a member another
    service adds to a class."""
    stream, expected = member_stream(connected, member)
    assert stream.get() == expected


@profile("streams", "dynamic")
def test_procedure_without_a_value(connected):
    """Streaming a procedure that returns no value, such as a property setter, raises the
    stream error."""
    calls = [
        ("TestService", "set_StringProperty", values.string("foo")),
        (
            "TestService",
            "TestClass_set_IntProperty",
            values.obj(create(connected)),
            values.sint32(42),
        ),
        ("TestService", "ResetCustomExceptionLater"),
    ]
    for call in calls:
        error = raised(lambda call=call: connected.add_stream(*call))
        assert_error_kind(connected, error, pb.Error.STREAM)


@profile("streams")
def test_procedure_returning_an_event(connected):
    """Streaming a procedure that returns an event raises the stream error."""
    error = raised(
        lambda: connected.add_stream(
            "TestService", "OnTimer", values.uint32(1000), values.uint32(1)
        )
    )
    assert_error_kind(connected, error, pb.Error.STREAM)


@profile("streams")
@pytest.mark.parametrize("case", ["together", "after-remove"])
def test_add_twice(connected, control, request, case):
    """Each add makes a new stream with its own id and lifetime, including an add of a
    call already streamed or removed."""
    first = watched(connected, name(request))
    assert first.get() == 0
    if case == "after-remove":
        first.remove()
    second = watched(connected, name(request))
    assert second.get() == 0
    first.remove()
    control.conformance_service.set_value(name(request), 5)
    assert wait_until(lambda: second.get() == 5)
    assert is_removed_error(connected, raised(first.get))


section("Handles")


@profile("streams")
def test_equality(connected, request):
    """Copies of a stream handle are equal. Handles from two adds of one call are not."""
    first = watched(connected, name(request))
    second = watched(connected, name(request))
    assert connected.compare_handles(first, first.copy())
    assert not connected.compare_handles(first, second)


@profile("streams")
def test_hash(connected, request):
    """Copies of a stream handle hash equal, in a language that hashes handles."""
    stream = watched(connected, name(request))
    assert connected.hash_handle(stream) == connected.hash_handle(stream.copy())


section("Rate")


@profile("streams")
def test_rate(connected, request):
    """The rate of a stream is 0, for no limit, until the program sets it. The program sets
    the rate, and reads it back."""
    stream = watched(connected, name(request))
    assert stream.rate == 0
    stream.rate = 5
    assert stream.rate == 5
    stream.rate = 0
    assert stream.rate == 0


@timing
@profile("streams")
def test_rate_takes_effect(connected):
    """A stream with a rate updates at most that many times a second."""
    stream = counter(connected, "rate")
    stream.rate = 5
    # An update already on its way when the rate is set
    stream.wait()
    first = stream.get()
    time.sleep(1)
    # The test server updates its streams 60 times a second
    assert 1 <= stream.get() - first <= 8


section("Values")


@profile("streams")
@pytest.mark.parametrize("case", ["immediate", "delayed"])
def test_first_value(connected, control, request, case):
    """A stream starts when it is added. Reading its value blocks until the first value
    arrives."""
    if case == "immediate":
        control.conformance_service.set_value(name(request), 3)
        assert watched(connected, name(request)).get() == 3
    else:
        # The procedure returns after ten of the server's updates
        stream = connected.add_stream(
            "TestService", "BlockingProcedure", values.sint32(10), values.sint32(0)
        )
        assert stream.get() == 55


@profile("streams")
@pytest.mark.parametrize(
    "kind",
    [
        "null-object",
        "null-int",
        "null-string",
        "nullable-int",
        "enum",
        "struct",
        "list-of-nullable-ints",
        "list-of-nullable-objects",
    ],
)
def test_value_types(connected, kind):
    """A stream delivers a value of any type, such as a null, an enumeration value, a
    structure and a collection holding nulls."""
    stream, expected = stream_values(connected, kind)
    result = stream.get()
    if kind == "list-of-nullable-objects":
        result = [None if x is None else x.id for x in result]
    assert result == expected


VALUE_CHANGES = {
    "null": [1.0, None, 1.0],
    "negative-zero": [0.0, -0.0, 0.0],
    "nan": [math.nan, 1.0],
    "infinity": [math.inf, -math.inf],
}


def same_double(a, b):
    """Whether two doubles, or nulls, are the same value, with NaN the same as NaN and 0.0
    different from -0.0."""
    if a is None or b is None:
        return a is b
    if math.isnan(a) or math.isnan(b):
        return math.isnan(a) and math.isnan(b)
    return a == b and math.copysign(1, a) == math.copysign(1, b)


@profile("streams")
@pytest.mark.parametrize("case", list(VALUE_CHANGES))
def test_value_changes(connected, control, request, case):
    """A stream delivers each change of its value, such as a change to or from a null,
    ``NaN`` or an infinity, and between 0.0 and -0.0. A ``NaN`` once read is not new to a
    wait, as for any other value."""
    stream = connected.add_stream(
        "ConformanceService", "Double", values.string(name(request))
    )
    assert stream.get() is None
    for value in VALUE_CHANGES[case]:
        control.conformance_service.set_double(name(request), value)
        assert wait_until(lambda v=value: same_double(stream.get(), v))
        assert stream.wait(0) is False


@profile("streams")
def test_independent_streams(connected, control, request):
    """Several streams update independently."""
    a = watched(connected, name(request) + "a")
    b = watched(connected, name(request) + "b")
    assert a.get() == b.get() == 0
    control.conformance_service.set_value(name(request) + "a", 1)
    assert wait_until(lambda: a.get() == 1)
    assert b.get() == 0
    control.conformance_service.set_value(name(request) + "b", 2)
    assert wait_until(lambda: b.get() == 2)
    assert a.get() == 1


@profile("streams")
def test_current_after_call(connected, control, request):
    """Once a call returns, each stream value is at least as new as the last update the
    server wrote before the response to the call."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    service = control.conformance_service
    # The server writes the control connection's update in the same pass as the testee's, so
    # once control sees a value, a response to a later call follows the testee's update
    seen = control.add_stream(service.value, name(request))
    try:
        for value in range(1, 6):
            service.set_value(name(request), value)
            wait_for_control(seen, lambda seen_value, value=value: seen_value == value)
            connected.invoke("TestService", "Int32ToString", values.sint32(value))
            assert stream.get() == value
    finally:
        seen.remove()


@profile("streams")
def test_update_with_a_response(client, scripted):
    """The client delivers a stream update that arrives in the same read as the response
    to a call."""
    stream = scripted_counter(client)
    scripted.send_update([(1, encode_sint32(1))])
    assert wait_until(lambda: stream.get() == 1)
    update = KRPC.StreamUpdate()
    update.results.add(id=1, result=KRPC.ProcedureResult(value=encode_sint32(2)))
    scripted.after_response["Int32ToString"] = lambda call: [
        KRPC.Message(stream_update=update)
    ]
    round_trip(client)
    assert wait_until(lambda: stream.get() == 2)


@profile("streams")
@pytest.mark.parametrize("case", ["value", "two-values", "ending", "event"])
def test_update_with_add_response(client, scripted, case):
    """The client delivers a stream update that arrives in the same read as the response
    to the add, before the stream's handle exists. This holds for a value, for the latest
    of two values, for an error that ends the stream, and for the firing of an event a
    call returns."""
    results = {
        "value": [KRPC.ProcedureResult(value=encode_sint32(5))],
        "two-values": [
            KRPC.ProcedureResult(value=encode_sint32(4)),
            KRPC.ProcedureResult(value=encode_sint32(5)),
        ],
        "ending": [
            KRPC.ProcedureResult(error=KRPC.Error(description="Ended by the test"))
        ],
        "event": [KRPC.ProcedureResult(value=b"\x01")],
    }[case]
    updates = []
    for result in results:
        update = KRPC.StreamUpdate()
        update.results.add(id=1, result=result)
        updates.append(KRPC.Message(stream_update=update))
    procedure = "OnTimer" if case == "event" else "AddStream"
    scripted.after_response[procedure] = lambda call: updates
    if case == "event":
        event = client.event(
            "TestService", "OnTimer", values.uint32(1000), values.uint32(1)
        )
        assert event.wait(REPLY_TIMEOUT)
        return
    stream = scripted_counter(client)
    if case == "ending":
        with pytest.raises(TesteeError) as info:
            stream.get()
        assert "Ended by the test" in info.value.error.message
    else:
        assert stream.get() == 5


@profile("streams")
@pytest.mark.parametrize("case", ["before-add", "never-issued"])
def test_update_for_an_unknown_id(client, scripted, case):
    """The client drops an update for a stream id it holds no stream for, such as an
    update that arrives before the response to the add."""
    if case == "before-add":
        scripted.before_response["AddStream"] = lambda call: scripted.send_update(
            [(1, encode_sint32(3))]
        )
        stream = scripted_counter(client)
        # The call returns once the client has handled the update sent before the response
        round_trip(client)
        getting = client.testee.send(get=pb.Get(stream=stream.handle))
        scripted.send_update([(1, encode_sint32(4))])
        assert values.native(client.testee.reply(getting).value) == 4
    else:
        stream = scripted_counter(client)
        scripted.send_update([(99, encode_sint32(5))])
        scripted.send_update([(1, encode_sint32(7)), (98, encode_sint32(8))])
        assert stream.get() == 7
        round_trip(client)
        assert stream.get() == 7


section("Errors")


@profile("streams")
def test_error_ends_the_stream(connected, control, request):
    """A result with an error ends the stream. Later reads and waits raise the error a
    call raises for it, after a remove too."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    control.conformance_service.fail_value(name(request), "Failed by the test")
    assert wait_until(lambda: ended(stream))
    for read in (stream.get, stream.wait, lambda: stream.wait(1)):
        error = raised(read)
        assert_native_error(connected, control, error, "InvalidOperationException")
        assert "Failed by the test" in error.message
    # The stream stays ended once the value can be read again
    control.conformance_service.set_value(name(request), 1)
    assert "Failed by the test" in raised(stream.get).message
    stream.remove()
    assert "Failed by the test" in raised(stream.get).message


@profile("streams")
@pytest.mark.parametrize("exception", list(EXCEPTIONS))
@pytest.mark.parametrize("when", ["first", "later"])
def test_procedure_exception(connected, control, when, exception):
    """A procedure that throws an exception ends the stream with the error a call raises
    for the exception, of the same type and with the same message, whether it throws for
    the first value or a later one."""
    thrown = EXCEPTIONS[exception]
    if when == "later":
        connected.invoke("TestService", thrown.reset)
        stream = connected.add_stream("TestService", thrown.later)
        assert stream.get() == 0
        assert wait_until(lambda: ended(stream))
    else:
        stream = connected.add_stream("TestService", thrown.first)
    assert_exception(connected, control, raised(stream.get), exception)


@profile("streams")
def test_undecodable_value(client, scripted):
    """A value that fails to decode is dropped. Its stream keeps its value, takes the
    next update, and does not end."""
    stream = scripted_counter(client)
    other = scripted_counter(client)
    scripted.send_update([(1, encode_sint32(1)), (2, encode_sint32(1))])
    assert stream.get() == 1
    ending = stream.add_end_callback()
    # A varint cut short
    scripted.send_update([(1, b"\xff"), (2, encode_sint32(2))])
    assert wait_until(lambda: other.get() == 2)
    assert stream.get() == 1
    scripted.send_update([(1, encode_sint32(3))])
    assert wait_until(lambda: stream.get() == 3)
    round_trip(client)
    assert ending.calls == []


section("Removing Streams")


@profile("streams")
def test_remove(connected, request):
    """Removing a stream through its handle ends it at once, for every copy of the
    handle. Reading it raises the error for a removed stream."""
    stream = watched(connected, name(request))
    copy = stream.copy()
    assert stream.get() == 0
    stream.remove()
    assert is_removed_error(connected, raised(stream.get))
    assert is_removed_error(connected, raised(copy.get))
    assert is_removed_error(connected, raised(copy.wait))


@profile("streams")
def test_remove_twice(connected, control, request):
    """Removing a stream again, through any copy of the handle, does nothing. A stream
    added since keeps running, though the server can give it the removed stream's id."""
    stream = watched(connected, name(request))
    copy = stream.copy()
    stream.get()
    stream.remove()
    later = watched(connected, name(request))
    stream.remove()
    copy.remove()
    round_trip(connected)
    control.conformance_service.set_value(name(request), 1)
    assert wait_until(lambda: later.get() == 1)


@should
@profile("streams")
def test_scoped_removal(connected, request):
    """A stream can be scoped to a block of code, using the scoping construct of the
    language. Leaving the block removes the stream."""
    stream = connected.add_stream(
        "ConformanceService", "Value", values.string(name(request)), scoped=True
    )
    assert is_removed_error(connected, raised(stream.get))


@profile("streams", "callbacks")
def test_drop_last_handle(connected, control, request):
    """A stream runs while the program holds a handle to it. Callbacks added through a copy
    of the handle keep running after the copy is dropped. Dropping the last handle removes
    the stream: the server stops evaluating it, and its callbacks are not called again.
    Dropping a handle means letting go of it, such as by a destructor or a garbage
    collection."""
    stream = counter(connected, name(request))
    copy = stream.copy()
    callback = copy.add_callback()
    copy.drop()
    count = len(callback.wait_for_calls(1))
    assert len(callback.wait_for_calls(count + 1)) > count
    stream.drop()

    def evaluated():
        """Whether the server evaluates the stream between two calls of the counter."""
        first = connected.invoke(
            "TestService", "Counter", values.string(name(request)), values.sint32(1)
        )
        time.sleep(SETTLE)
        second = connected.invoke(
            "TestService", "Counter", values.string(name(request)), values.sint32(1)
        )
        return second != first + 1

    # A client that drops a handle by a garbage collection removes the stream later
    assert wait_until(lambda: not evaluated())
    update_barrier(connected, control, name(request) + "barrier")
    count = len(callback.calls)
    update_barrier(connected, control, name(request) + "barrier")
    assert len(callback.calls) == count


@profile("streams")
@pytest.mark.parametrize("case", ["in-flight", "after-response"])
def test_update_after_remove(client, scripted, case):
    """The client drops an update for a removed stream, whether it arrives before or
    after the response to the remove."""
    stream = scripted_counter(client)
    scripted.send_update([(1, encode_sint32(1))])
    assert stream.get() == 1
    if case == "in-flight":
        scripted.before_response["RemoveStream"] = lambda call: scripted.send_update(
            [(1, encode_sint32(2))]
        )
        stream.remove()
    else:
        stream.remove()
        scripted.wait_for_call("RemoveStream")
        scripted.send_update([(1, encode_sint32(2))])
    round_trip(client)
    assert is_removed_error(client, raised(stream.get))


section("Connection Ends")


@profile("streams")
@pytest.mark.parametrize("state", ["live", "ended"])
def test_read_after_close(connected, control, request, state):
    """Reading or waiting on a stream after the connection closes raises the connection
    error, with no cause in a language that chains errors. A stream that had ended with an
    error raises that error instead. Removing the stream does nothing."""
    stream = watched(connected, name(request))
    stream.get()
    if state == "ended":
        control.conformance_service.fail_value(name(request), "Failed by the test")
        assert wait_until(lambda: ended(stream))
    connected.close()

    def check(error):
        if state == "live":
            assert error.kind == pb.Error.CONNECTION
            assert error.cause == ""
        else:
            assert "Failed by the test" in error.message

    for read in (stream.get, stream.wait):
        check(raised(read))
    stream.remove()
    check(raised(stream.get))


@profile("streams")
@pytest.mark.parametrize("failure", ["lost", "malformed-message"])
def test_connection_failure_cause(client, scripted, failure):
    """A stream that a connection failure ends raises the connection error. In a
    language that chains errors, the error carries the error that failed the connection
    as its cause, such as a lost connection or a message that fails to parse."""
    stream = scripted_counter(client)
    scripted.send_update([(1, encode_sint32(1))])
    assert stream.get() == 1
    if failure == "lost":
        scripted.close()
    else:
        # A field tag cut short
        scripted.send_payload(b"\xff\xff")
    assert wait_until(lambda: ended(stream))
    error = raised(stream.get)
    assert error.kind == pb.Error.CONNECTION
    assert error.cause != ""


@profile("streams", "callbacks", "threads")
def test_close_after_failure(connected, control, request):
    """A connection closed after it fails, before its streams have ended, ends them with
    the connection error and the reason for the failure."""
    stream = watched(connected, name(request))
    stream.get()
    other = counter(connected, name(request) + "other")
    # The first call may run on the adding thread, so a later call blocks the callbacks
    callback, adding = other.start_callback("adder", pb.Behavior(block=True, skip=1))
    try:
        callback.wait_for_calls(2)
        control.conformance_service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
        time.sleep(SETTLE)
        # The close waits for the blocked callback, once it has ended the streams
        closing = connected.start("closer", close=pb.Close())
        time.sleep(SETTLE)
        error = raised(stream.get)
    finally:
        for _ in range(10):
            callback.release()
    adding.reply()
    closing.reply()
    assert error.kind == pb.Error.CONNECTION
    assert error.reason == "SERVER_STOPPED"


section("Waiting")


@profile("streams")
def test_wait(connected):
    """Waiting on a stream returns on its next update."""
    stream = counter(connected, "wait")
    value = stream.get()
    assert stream.wait()
    assert stream.get() > value


@profile("streams")
@pytest.mark.parametrize("case", ["elapses", "update"])
def test_wait_timeout(connected, control, request, case):
    """Waiting with a timeout returns false when the timeout elapses, and true on an
    update. The timeout is a deadline for the whole wait."""
    stream = watched(connected, name(request))
    stream.get()
    if case == "elapses":
        start = time.perf_counter()
        assert not stream.wait(SHORT_WAIT)
        assert time.perf_counter() - start >= SHORT_WAIT
        return
    testee = connected.testee
    waiting = testee.send(wait=pb.Wait(stream=stream.handle, timeout=REPLY_TIMEOUT))
    control.conformance_service.set_value(name(request), 1)
    reply = testee.reply(waiting, 2 * REPLY_TIMEOUT)
    assert values.native(reply.value) is True
    assert stream.get() == 1


@profile("streams")
@pytest.mark.parametrize("timeout", ["zero", "infinite", "negative", "nan"])
def test_wait_timeout_values(connected, control, request, timeout):
    """A zero timeout returns false at once, when no update is pending. An infinite
    timeout waits with no deadline, as a wait without a timeout does. A negative or NaN
    timeout raises the argument error."""
    stream = watched(connected, name(request))
    stream.get()
    if timeout == "zero":
        start = time.perf_counter()
        assert stream.wait(0) is False
        assert time.perf_counter() - start < AT_ONCE
        return
    testee = connected.testee
    if timeout in ("negative", "nan"):
        seconds = -1.0 if timeout == "negative" else math.nan
        waiting = testee.send(wait=pb.Wait(stream=stream.handle, timeout=seconds))
        error = raised(lambda: testee.reply(waiting, REPLY_TIMEOUT))
        assert error.kind == pb.Error.ARGUMENT, error
        return
    waiting = testee.send(wait=pb.Wait(stream=stream.handle, timeout=math.inf))
    # A wait that returns at once replies before the update
    time.sleep(SETTLE)
    control.conformance_service.set_value(name(request), 1)
    reply = testee.reply(waiting, REPLY_TIMEOUT)
    assert values.native(reply.value) is True


@profile("streams", "threads")
def test_two_waiters(connected, control, request):
    """Two threads waiting on one stream both return on its next update."""
    stream = watched(connected, name(request))
    stream.get()
    first = connected.start("first", wait=pb.Wait(stream=stream.handle))
    second = connected.start("second", wait=pb.Wait(stream=stream.handle))
    time.sleep(SETTLE)
    control.conformance_service.set_value(name(request), 1)
    assert first.result() is True
    assert second.result() is True


@profile("streams", "threads")
@pytest.mark.parametrize("end", ["remove", "close", "connection-failure"])
def test_wake_waiter(connected, control, request, end):
    """A thread waiting on a stream wakes when the stream is removed, the connection closes
    or the connection fails. The wait raises the error for a removed stream, or the
    connection error, with the reason for a failure."""
    stream = watched(connected, name(request))
    stream.get()
    waiting = connected.start("waiter", wait=pb.Wait(stream=stream.handle))
    time.sleep(SETTLE)
    if end == "remove":
        stream.remove()
        assert is_removed_error(connected, waiting.error())
    elif end == "close":
        connected.close()
        assert waiting.error().kind == pb.Error.CONNECTION
    else:
        control.conformance_service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
        error = waiting.error()
        assert error.kind == pb.Error.CONNECTION
        assert error.reason == "SERVER_STOPPED"


section("Callbacks")


@profile("streams", "callbacks")
def test_callback(connected):
    """A callback is called with each new value, and none is skipped."""
    stream = counter(connected, "callback")
    calls = stream.add_callback().wait_for_calls(3)
    assert len(calls) >= 3
    assert all(b == a + 1 for a, b in zip(calls, calls[1:]))


@profile("streams", "callbacks")
def test_callback_order(connected, control, request):
    """A callback never receives a value older than the last one it received."""
    stream = watched(connected, name(request))
    callback = stream.add_callback()
    for value in range(1, 21):
        control.conformance_service.set_value(name(request), value)
    assert wait_until(lambda: stream.get() == 20)
    assert wait_until(lambda: callback.calls[-1:] == [20])
    calls = callback.calls
    assert calls == sorted(calls)


@profile("streams", "callbacks")
def test_callback_values_only(connected, control, request):
    """A callback receives values only. The error that ends a stream goes to its end
    callbacks."""
    stream = watched(connected, name(request))
    callback = stream.add_callback()
    ending = stream.add_end_callback()
    assert callback.wait_for_calls(1) == [0]
    control.conformance_service.fail_value(name(request), "Failed by the test")
    assert len(ending.wait_for_calls(1)) == 1
    assert callback.calls == [0]


@profile("streams", "callbacks")
def test_remove_callback(connected, control, request):
    """A removed callback is not called again."""
    stream = watched(connected, name(request))
    callback = stream.add_callback()
    assert callback.wait_for_calls(1) == [0]
    callback.remove()
    control.conformance_service.set_value(name(request), 1)
    assert wait_until(lambda: stream.get() == 1)
    # A second callback sees the update, after which the removed one would have too
    assert stream.add_callback().wait_for_calls(1) == [1]
    assert callback.calls == [0]


@profile("streams", "callbacks", "races")
def test_callback_after_remove(connected, control, request):
    """A callback is not called with a value once the program removes its stream, though
    the value arrived before the remove."""
    stream = watched(connected, name(request))
    callback = stream.add_callback()
    assert callback.wait_for_calls(1) == [0]
    # The update thread finishes with the first value, so the pause is for the next one
    update_barrier(connected, control, name(request) + "barrier")
    with connected.paused(pb.STREAM_CALLBACKS) as pause:
        control.conformance_service.set_value(name(request), 1)
        pause.wait()
        stream.remove()
    update_barrier(connected, control, name(request) + "barrier")
    assert callback.calls == [0]


@profile("streams", "callbacks", "races")
def test_callback_removed_during_update(connected, control, request):
    """A callback removed while an update passes a value to the stream's callbacks is not
    called with the value."""
    stream = watched(connected, name(request))
    callback = stream.add_callback()
    assert callback.wait_for_calls(1) == [0]
    # The update thread finishes with the first value, so the pause is for the next one
    update_barrier(connected, control, name(request) + "barrier")
    with connected.paused(pb.STREAM_CALLBACKS) as pause:
        control.conformance_service.set_value(name(request), 1)
        pause.wait()
        callback.remove()
    update_barrier(connected, control, name(request) + "barrier")
    assert callback.calls == [0]


@should
@profile("streams", "callbacks")
def test_update_callback(connected, control, request):
    """A callback for the whole connection is called once per stream update. A removed
    callback is not called again."""
    callback = connected.add_update_callback()
    stream = watched(connected, name(request))
    stream.get()
    count = len(callback.wait_for_calls(1))
    control.conformance_service.set_value(name(request), 1)
    assert wait_until(lambda: stream.get() == 1)
    assert len(callback.wait_for_calls(count + 1)) > count
    callback.remove()
    count = len(callback.calls)
    control.conformance_service.set_value(name(request), 2)
    assert wait_until(lambda: stream.get() == 2)
    round_trip(connected)
    assert len(callback.calls) == count


section("First Calls")


@profile("streams", "callbacks")
def test_first_call(connected, control, request):
    """A callback added to a stream that has a value is called once with that value. This is
    the callback's first call. Later values follow it."""
    control.conformance_service.set_value(name(request), 4)
    stream = watched(connected, name(request))
    assert stream.get() == 4
    callback = stream.add_callback()
    assert callback.wait_for_calls(1) == [4]
    control.conformance_service.set_value(name(request), 5)
    assert callback.wait_for_calls(2) == [4, 5]


@should
@profile("streams", "callbacks")
def test_first_call_on_add(connected, control, request):
    """The first call of a callback is made before the add returns."""
    control.conformance_service.set_value(name(request), 4)
    stream = watched(connected, name(request))
    assert stream.get() == 4
    # The notification is written before the reply to the add
    assert stream.add_callback().calls == [4]


@profile("streams", "callbacks", "threads", "races")
def test_first_call_superseded(connected, control, request):
    """A first call is skipped if an update passes the callback a newer value first."""
    control.conformance_service.set_value(name(request), 1)
    stream = watched(connected, name(request))
    assert wait_until(lambda: stream.get() == 1)
    with connected.paused(pb.FIRST_CALL) as pause:
        callback, adding = stream.start_callback("adder")
        pause.wait()
        control.conformance_service.set_value(name(request), 2)
        assert callback.wait_for_calls(1) == [2]
    adding.reply()
    assert callback.calls == [2]


@profile("streams", "callbacks", "threads")
def test_queued_value_before_ending(connected, control, request):
    """A value that arrives while a first call runs is passed to the callback once that call
    returns. This holds when the server ends the stream meanwhile. The end callbacks are
    then called after the value."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    ending = stream.add_end_callback()
    callback, adding = stream.start_callback("adder", pb.Behavior(block=True, only=1))
    try:
        assert callback.wait_for_calls(1) == [0]
        control.conformance_service.set_value(name(request), 1)
        assert wait_until(lambda: stream.get() == 1)
        control.conformance_service.fail_value(name(request), "Failed by the test")
        assert wait_until(lambda: ended(stream))
    finally:
        callback.release()
    adding.reply()
    assert len(ending.wait_for_calls(1)) == 1
    assert callback.wait_for_calls(2) == [0, 1]
    assert connected.testee.call_order(callback.id, ending.id) == [
        callback.id,
        callback.id,
        ending.id,
    ]


@profile("streams", "callbacks", "threads")
def test_queued_value_dropped_by_remove(connected, control, request):
    """A value that arrives while a first call runs is not passed to the callback once the
    program removes the stream."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    callback, adding = stream.start_callback("adder", pb.Behavior(block=True, only=1))
    try:
        assert callback.wait_for_calls(1) == [0]
        control.conformance_service.set_value(name(request), 1)
        assert wait_until(lambda: stream.get() == 1)
        # The update thread queues the value for the adding thread
        time.sleep(SETTLE)
        # The remove can wait for the first call to return
        removing = connected.start("remover", remove=pb.Remove(stream=stream.handle))
        time.sleep(SETTLE)
    finally:
        callback.release()
    removing.reply()
    # The adding thread writes the notification of a queued call before its reply
    adding.reply()
    assert callback.calls == [0]


@profile("streams", "callbacks", "threads", "races")
def test_value_as_first_call_ends(connected, control, request):
    """A value that arrives once a first call and the values queued during it have been
    passed, and before the add returns, is passed to the callback."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    with connected.paused(pb.FIRST_CALL_ENDED) as pause:
        callback, adding = stream.start_callback("adder")
        pause.wait()
        control.conformance_service.set_value(name(request), 1)
        # The update thread waits for the callback
        time.sleep(SETTLE)
    adding.reply()
    assert callback.wait_for_calls(2) == [0, 1]


section("Callback Errors")


@profile("streams", "callbacks", "threads")
def test_callback_error(connected, control, request):
    """An error raised by a callback leaves the other callbacks and streams running."""
    raising = watched(connected, name(request) + "raising")
    raising.add_callback(pb.Behavior(fail="Failed by the test"))
    beside = raising.add_callback()
    other = watched(connected, name(request) + "other")
    callback = other.add_callback()
    for value in (1, 2, 3):
        control.conformance_service.set_value(name(request) + "raising", value)
        control.conformance_service.set_value(name(request) + "other", value)
        assert wait_until(lambda value=value: callback.calls[-1:] == [value])
    assert wait_until(lambda: raising.get() == 3)
    # A second callback on the stream is called as well
    assert wait_until(lambda: beside.calls[-1:] == [3])


@profile("streams", "callbacks", "threads")
def test_callback_fatal_error(connected, control, request):
    """An error of the kind the language keeps apart from its ordinary exceptions, such as
    an ``Error`` in Java, raised by a callback running on a thread of the client, leaves the
    other callbacks and streams running."""
    raising = watched(connected, name(request) + "raising")
    # The first call can run on the thread that adds the callback
    raising.add_callback(pb.Behavior(fail_fatally="Failed by the test", skip=1))
    beside = raising.add_callback()
    other = watched(connected, name(request) + "other")
    callback = other.add_callback()
    for value in (1, 2, 3):
        control.conformance_service.set_value(name(request) + "raising", value)
        control.conformance_service.set_value(name(request) + "other", value)
        assert wait_until(lambda value=value: callback.calls[-1:] == [value])
    assert wait_until(lambda: beside.calls[-1:] == [3])


@should
@profile("streams", "callbacks")
def test_first_call_fatal_error(connected, control, request):
    """A fatal error raised by a first call made before the add returns propagates from
    the add. The add leaves no callback, so later updates do not call it."""
    stream = watched(connected, name(request))
    assert stream.get() == 0
    callback, adding = stream.start_callback(
        "", pb.Behavior(fail_fatally="Failed by the test")
    )
    error = adding.error()
    assert error.kind == pb.Error.FATAL
    assert "Failed by the test" in error.message
    control.conformance_service.set_value(name(request), 1)
    assert wait_until(lambda: stream.get() == 1)
    round_trip(connected)
    assert callback.calls == [0]


section("End Callbacks")


@profile("streams", "callbacks")
@pytest.mark.parametrize("end", ["remove", "error", "close", "connection-failure"])
def test_end_callback(connected, control, request, end):
    """An end callback is called once when the stream ends: by a remove, an error, or a
    close or failure of the connection. The callback receives the error a read of the
    stream then raises: the error for a removed stream, the server's error, or the
    connection error."""
    stream = watched(connected, name(request))
    stream.get()
    callback = stream.add_end_callback()
    if end == "remove":
        stream.remove()
    elif end == "error":
        control.conformance_service.fail_value(name(request), "Failed by the test")
    elif end == "close":
        connected.close()
    else:
        control.conformance_service.disconnect_client(
            request.node.name, "ServerStopped", "Stopped by the test"
        )
    ending = single_ending(callback)
    assert ending.error is not None
    if end == "remove":
        assert is_removed_error(connected, ending.error)
    elif end == "error":
        assert_native_error(
            connected, control, ending.error, "InvalidOperationException"
        )
        assert "Failed by the test" in ending.error.message
    else:
        assert ending.error.kind == pb.Error.CONNECTION
        assert raised(stream.get).kind == pb.Error.CONNECTION
    if end in ("remove", "error"):
        # A second remove and a call give a second end call time to arrive
        stream.remove()
        round_trip(connected)
        assert len(callback.calls) == 1


@profile("streams", "callbacks")
@pytest.mark.parametrize("end", ["remove", "error"])
def test_late_end_callback(connected, control, request, end):
    """An end callback added after the stream ends is called at once, with the error
    that ended the stream."""
    stream = watched(connected, name(request))
    stream.get()
    if end == "remove":
        stream.remove()
        ending = single_ending(stream.add_end_callback())
        assert ending.error is not None
        assert is_removed_error(connected, ending.error)
        return
    control.conformance_service.fail_value(name(request), "Failed by the test")
    assert wait_until(lambda: ended(stream))
    ending = single_ending(stream.add_end_callback())
    assert ending.error is not None
    assert_native_error(connected, control, ending.error, "InvalidOperationException")
    assert "Failed by the test" in ending.error.message


@profile("streams", "callbacks")
def test_remove_end_callback(connected, request):
    """A removed end callback is not called."""
    stream = watched(connected, name(request))
    stream.get()
    removed = stream.add_end_callback()
    kept = stream.add_end_callback()
    removed.remove()
    stream.remove()
    single_ending(kept)
    assert removed.calls == []


@profile("streams", "callbacks", "races")
def test_remove_during_server_ending(connected, control, request):
    """A stream the server has ended, removed before the client wakes its waiters and
    calls its end callbacks, keeps the server's error. The remove does nothing, and the end
    callbacks are called once, with the server's error."""
    stream = watched(connected, name(request))
    stream.get()
    callback = stream.add_end_callback()
    with connected.paused(pb.STREAM_ENDING) as pause:
        control.conformance_service.fail_value(name(request), "Failed by the test")
        pause.wait()
        stream.remove()
    ending = single_ending(callback)
    assert ending.error is not None
    assert "Failed by the test" in ending.error.message
    assert "Failed by the test" in raised(stream.get).message
    round_trip(connected)
    assert len(callback.calls) == 1


@profile("streams", "callbacks")
def test_end_callback_fatal_error_on_remove(connected, request):
    """A fatal error raised by an end callback that a remove calls on the program's
    thread propagates from the remove, once the other end callbacks are called and the
    server has removed the stream."""
    stream = counter(connected, name(request))
    stream.get()
    stream.add_end_callback(pb.Behavior(fail_fatally="Failed by the test"))
    other = stream.add_end_callback()
    error = raised(stream.remove)
    assert error.kind == pb.Error.FATAL
    assert len(other.calls) == 1
    # The server no longer evaluates the stream, so the counter goes up by one per call
    calls = connected.invoke(
        "TestService", "Counter", values.string(name(request)), values.sint32(1)
    )
    time.sleep(SETTLE)
    assert (
        connected.invoke(
            "TestService", "Counter", values.string(name(request)), values.sint32(1)
        )
        == calls + 1
    )


@profile("streams", "callbacks")
def test_end_callback_fatal_error_on_close(connected, request):
    """A fatal error raised by an end callback that a close calls on the program's
    thread propagates from the close, once the end callbacks of every stream are
    called."""
    others = []
    for suffix in ("first", "second"):
        stream = watched(connected, name(request) + suffix)
        stream.get()
        stream.add_end_callback(pb.Behavior(fail_fatally="Failed by the test"))
        others.append(stream.add_end_callback())
    error = raised(connected.close)
    assert error.kind == pb.Error.FATAL
    assert [len(x.calls) for x in others] == [1, 1]


@profile("streams", "callbacks")
def test_late_end_callback_fatal_error(connected, request):
    """A fatal error raised by an end callback that its add calls at once, as the stream
    has ended, propagates from the add."""
    stream = watched(connected, name(request))
    stream.get()
    stream.remove()
    error = raised(
        lambda: stream.add_end_callback(pb.Behavior(fail_fatally="Failed by the test"))
    )
    assert error.kind == pb.Error.FATAL
