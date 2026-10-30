"""Error requirements: the errors a client reports for an error returned by the server."""

import pytest
import testee_pb2 as pb
from driver import profile, raw, section, should, values
from driver.errors import (
    assert_native_error,
    assert_server_error_kind,
    error_types,
    native_type,
)
from driver.scripted import ScriptedServer, definitions
from driver.testee import TesteeError

import krpc.schema.KRPC_pb2 as KRPC
from krpc.encoder import Encoder
from krpc.types import Types

# Procedures throwing a KRPC exception with a mapped type, with their arguments
MAPPED_EXCEPTIONS = {
    "InvalidOperationException": ("ThrowInvalidOperationException", []),
    "ArgumentException": ("ThrowArgumentException", []),
    "ArgumentNullException": ("ThrowArgumentNullException", [values.string("foo")]),
    "ArgumentOutOfRangeException": (
        "ThrowArgumentOutOfRangeException",
        [values.sint32(0)],
    ),
}
for _name in (
    "NullReferenceException",
    "KeyNotFoundException",
    "IndexOutOfRangeException",
    "DivideByZeroException",
    "NotSupportedException",
    "TimeoutException",
    "OverflowException",
):
    MAPPED_EXCEPTIONS[_name] = ("ThrowMappedException", [values.string(_name)])

# Procedures throwing each exception, mapped or not
EXCEPTIONS = {**MAPPED_EXCEPTIONS, "CustomException": ("ThrowCustomException", [])}

# Descriptions with corner-case text
DESCRIPTIONS = {
    "empty": "",
    "non-ascii": "Ünïcödé 日本",
    "astral": "\U0001f680",
    "multi-line": "first\nsecond\r\nthird",
    "long": "x" * 5000,
}


def encoded(value):
    """The wire encoding of a string or sint32 value."""
    types = Types()
    if value.WhichOneof("value") == "string":
        return Encoder.encode(value.string, types.string_type)
    return Encoder.encode(value.sint32, types.sint32_type)


def raised(connected, service, procedure, *args):
    """The error a call reports."""
    with pytest.raises(TesteeError) as info:
        connected.invoke(service, procedure, *args)
    return info.value.error


def full_message(connected, server, service, procedure, *args):
    """The whole message of the error a call reports, from the error the server sends for
    it, and the prefix of a client without error types."""
    expected = raw.call(server, service, procedure, *[encoded(x) for x in args]).error
    message = expected.description
    if expected.stack_trace:
        message += "\nServer stack trace:\n" + expected.stack_trace
    prefix = ""
    if not error_types(connected):
        prefix = "%s.%s: " % (expected.service, expected.name)
    return prefix + message, prefix


def assert_cut(message, full, prefix):
    """The message is the whole message, or the start of it holding at least the prefix."""
    assert full.startswith(message), (message, full)
    assert message.startswith(prefix)
    assert message or not full


def assert_raises_type(error, service, name):
    """The error is the client's type for an exception a service lists."""
    assert error.kind == pb.Error.RPC
    assert (error.service, error.name) == (service, name)


def erroring_server(server, scripted_server, response_error=None, result_error=None):
    """A scripted server serving the KRPC service, that answers any other call with the given
    error, for the whole request or for the call."""
    krpc = definitions(server, "KRPC")

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        while True:
            message = scripted.receive()
            if message is None or message.WhichOneof("content") != "request":
                return
            response = KRPC.Response(id=message.request.id)
            if message.request.calls[0].procedure == "GetServices":
                response.results.add(value=krpc)
            elif response_error is not None:
                response.error.CopyFrom(response_error)
            else:
                response.results.add(error=result_error)
            scripted.send(KRPC.Message(response=response))

    return scripted_server(script)


def scripted_error(client, scripted):
    """The error a call to a scripted server reports."""
    client.connect(scripted.endpoint())
    try:
        return raised(client, "KRPC", "GetClientName")
    finally:
        client.close()
        scripted.join()


section("Server Errors")


@pytest.mark.parametrize("name", ["InvalidOperationException", "CustomException"])
def test_rpc_error(connected, name):
    """A call that fails on the server raises the RPC error."""
    procedure, args = EXCEPTIONS[name]
    assert raised(connected, "TestService", procedure, *args).kind == pb.Error.RPC


@should
def test_exception_name(connected):
    """The error exposes the service and name of an exception a service lists."""
    error = raised(connected, "TestService", "ThrowCustomException")
    assert (error.service, error.name) == ("TestService", "CustomException")


def test_setter_error(connected, control):
    """A property setter that fails on the server raises the error for the server's
    exception, as any other call does."""
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))
    connected.invoke("TestService", "set_NullableObject", values.obj(obj))
    # The setter of NullableObject throws for null
    error = raised(connected, "TestService", "set_NullableObject", values.null())
    assert_native_error(connected, control, error, "ArgumentNullException")


@pytest.mark.parametrize("service", ["Unknown", "KRPC"])
def test_unknown_exception(client, server, scripted_server, service):
    """A server error naming a service or exception absent from the client's definitions
    raises the RPC error. The message carries the description and the ``Service.Name`` of
    the error. This includes an absent exception of a service the client has."""
    sent = KRPC.Error(service=service, name="Thing", description="Oops")
    error = scripted_error(
        client, erroring_server(server, scripted_server, result_error=sent)
    )
    assert error.kind == pb.Error.RPC
    assert "%s.Thing" % service in error.message
    assert "Oops" in error.message
    assert error.name in ("", "Thing")


def test_request_error(client, server, scripted_server):
    """An error for the whole request raises the RPC error. The message is the
    description."""
    sent = KRPC.Error(description="Oops")
    error = scripted_error(
        client, erroring_server(server, scripted_server, response_error=sent)
    )
    assert error.kind == pb.Error.RPC
    assert error.message == "Oops"
    assert error.name == ""


def test_call_after_error(connected):
    """An error leaves the connection open, and the next call succeeds."""
    raised(connected, "TestService", "ThrowCustomException")
    assert connected.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


section(
    "Error Types",
    "A client with error types reports each exception a service lists with a type of its "
    "own, such as an exception class. A mapped exception is one whose definition, in "
    "``KRPC.GetServices``, names a ``mapped_type``: the .NET exception it stands for.",
)


@profile("error-types")
@pytest.mark.parametrize("name", sorted(MAPPED_EXCEPTIONS))
def test_mapped_exceptions(connected, control, name):
    """A mapped exception raises the language's native error type for it. The client may
    raise a type of its own for the exception instead, derived from the native type."""
    procedure, args = MAPPED_EXCEPTIONS[name]
    error = raised(connected, "TestService", procedure, *args)
    assert_server_error_kind(error, name)
    native = native_type(connected, control, name)
    if error.name:
        # A client that gives its own type for the exception names it
        assert (error.service, error.name) == ("KRPC", name)
        if native is not None:
            assert native in error.native_types
    else:
        # A client that gives the native type itself gives exactly that type
        assert error.native_types[0] == native


@profile("error-types")
@pytest.mark.parametrize("service", ["custom", "krpc"])
def test_service_exceptions(connected, service):
    """An exception a service lists, with no mapped type, raises the type the stubs expose
    for it. This holds for the ``KRPC`` exceptions, such as
    ``ObjectDestroyedException``."""
    if service == "custom":
        error = raised(connected, "TestService", "ThrowCustomException")
        assert_raises_type(error, "TestService", "CustomException")
    else:
        obj = connected.invoke(
            "TestService", "CreateTestObject", values.string("alpha")
        )
        connected.invoke("TestService", "RemoveTestObject", values.obj(obj))
        error = raised(connected, "TestService", "TestClass_GetValue", values.obj(obj))
        assert_raises_type(error, "KRPC", "ObjectDestroyedException")


section("Messages")


@profile("!error-types")
@pytest.mark.parametrize("name", sorted(EXCEPTIONS))
def test_message_prefix(connected, name):
    """A client without error types starts the message with ``Service.Name:``, the
    service and name the error carries."""
    procedure, args = EXCEPTIONS[name]
    service = "TestService" if name == "CustomException" else "KRPC"
    error = raised(connected, "TestService", procedure, *args)
    assert error.message.startswith("%s.%s: " % (service, name))


@pytest.mark.parametrize("name", sorted(EXCEPTIONS))
def test_message(connected, server, name):
    """The message is the description, followed by ``Server stack trace:`` and the stack
    trace when the server sends one. A client may cut the end off the message, keeping at
    least the prefix of :ref:`errors-message-prefix <spec-errors-message-prefix>`."""
    procedure, args = EXCEPTIONS[name]
    full, prefix = full_message(connected, server, "TestService", procedure, *args)
    message = raised(connected, "TestService", procedure, *args).message
    assert_cut(message, full, prefix)


@should
@pytest.mark.parametrize("name", sorted(EXCEPTIONS))
def test_whole_message(connected, server, name):
    """The message holds the whole description and stack trace."""
    procedure, args = EXCEPTIONS[name]
    full, _ = full_message(connected, server, "TestService", procedure, *args)
    assert raised(connected, "TestService", procedure, *args).message == full


@pytest.mark.parametrize("case", list(DESCRIPTIONS))
def test_description(connected, control, server, request, case):
    """The message carries the description unchanged, apart from any cut allowed by
    :ref:`errors-message <spec-errors-message>`."""
    name = values.string(request.node.nodeid)
    control.conformance_service.fail_value(request.node.nodeid, DESCRIPTIONS[case])
    full, prefix = full_message(connected, server, "ConformanceService", "Value", name)
    assert prefix + DESCRIPTIONS[case] in full
    message = raised(connected, "ConformanceService", "Value", name).message
    assert_cut(message, full, prefix)
