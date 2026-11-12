"""Call requirements: calling the members of a service."""

import math
import sys
import time

import pytest
import testee_pb2 as pb
from driver import profile, raw, section, should, values
from driver.errors import (
    assert_argument_error,
    assert_error_kind,
    assert_interrupt_error,
)
from driver.framing import encode_sint32, encode_size
from driver.helpers import create
from driver.scripted import ScriptedServer, definitions, is_call, serve_definitions
from driver.testee import TesteeError, invocation, wait_until

import krpc.schema.KRPC_pb2 as KRPC
from krpc.attributes import Attributes


def argument_count_call(connected, kind, count):
    """Call a procedure, or a method, with the given number of arguments past the instance.
    Each takes one argument."""
    if kind == "procedure":
        args = [values.sint32(1)] * count
        return connected.invoke("TestService", "Int32ToString", *args)
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))
    args = [values.float32(1)] * count
    return connected.invoke(
        "TestService", "TestClass_FloatToString", values.obj(obj), *args
    )


# The procedure returning each default, and the value it returns
DEFAULTS = {
    "scalar": ("ScalarDefault", 42),
    "string": ("StringDefault", "foo"),
    "bytes": ("BytesDefault", b"\xca\xfe"),
    "list": ("ListDefault", [1, 2, 3]),
    "set": ("SetDefault", {1, 2, 3}),
    "dictionary": ("DictionaryDefault", {1: False, 2: True}),
    "tuple": ("TupleDefault", (1, False)),
    "enum": ("EnumDefault", values.Enum(2)),
    "flags": ("FlagsDefault", values.Enum(5)),
    "enum-list": ("EnumListDefault", [values.Enum(1), values.Enum(2)]),
    "empty-list": ("EmptyListDefault", []),
    "null": ("NullDefault", "null"),
    "struct": (
        "StructDefault",
        values.Struct((42, "alpha", values.Enum(1), [1, 2, 3])),
    ),
    "double-specials": (
        "DoubleSpecialDefaults",
        [math.nan, math.inf, -math.inf, sys.float_info.max, -sys.float_info.max],
    ),
    "float-specials": (
        "FloatSpecialDefaults",
        [
            math.nan,
            math.inf,
            -math.inf,
            values.FLOAT_MAX,
            -values.FLOAT_MAX,
            values.single(0.1),
        ],
    ),
    "sint32-extremes": ("Int32SpecialDefaults", [2**31 - 1, -(2**31)]),
    "sint64-extremes": ("Int64SpecialDefaults", [2**63 - 1, -(2**63)]),
    "uint32-extremes": ("Uint32SpecialDefaults", [2**32 - 1]),
    "uint64-extremes": ("Uint64SpecialDefaults", [2**64 - 1]),
}


def same(result, expected):
    """Whether a result equals the expected value, with NaN equal to NaN."""
    if isinstance(expected, float) and math.isnan(expected):
        return isinstance(result, float) and math.isnan(result)
    if isinstance(expected, list):
        return (
            isinstance(result, list)
            and len(result) == len(expected)
            and all(same(x, y) for x, y in zip(result, expected))
        )
    if isinstance(expected, bytes):
        return values.as_bytes(result) == expected
    if isinstance(expected, tuple):
        return tuple(result) == expected
    return result == expected


def optional_arguments(connected, kind, *args, named=None):
    """Call OptionalArguments(x, y="foo", z="bar", obj=null) as a procedure, an instance
    method or a static method."""
    if kind == "procedure":
        return connected.invoke("TestService", "OptionalArguments", *args, named=named)
    if kind == "method":
        obj = connected.invoke(
            "TestService", "CreateTestObject", values.string("alpha")
        )
        return connected.invoke(
            "TestService",
            "TestClass_OptionalArguments",
            values.obj(obj),
            *args,
            named=named,
        )
    return connected.invoke(
        "TestService", "TestClass_static_StaticOptionalArguments", *args, named=named
    )


def held_calls(endpoint, count, order, held):
    """A script that answers every call but Int32ToString, holds count calls of that, then
    answers them in the order the order function gives, each with its own argument as text.
    It ends early if the client closes."""
    services = definitions(endpoint, "KRPC", "TestService")

    def script(server: ScriptedServer) -> None:
        server.services = services
        server.accept()
        while len(held) < count:
            message = server.receive()
            if message is None:
                return
            if message.WhichOneof("content") != "request":
                continue
            if is_call(message.request.calls[0], "Int32ToString"):
                held.append(message.request)
            else:
                server.answer(message.request)
        for request in order(held):
            # A small sint32 is one zigzag byte
            text = str(request.calls[0].arguments[0].value[0] >> 1).encode()
            response = KRPC.Response(id=request.id)
            response.results.add(value=bytes([len(text)]) + text)
            server.send(KRPC.Message(response=response))
        server.serve()

    return script


def to_string(value):
    return invocation("TestService", "Int32ToString", values.sint32(value))


# Example names in each case, as the server defines them and as the client exposes them
NAMES = {
    "snake": {
        "Server": "server",
        "MyServer": "my_server",
        "Int32ToString": "int32_to_string",
        "32ToString": "32_to_string",
        "ToInt32": "to_int32",
        "HTTPS": "https",
        "HTTPServer": "http_server",
        "MyHTTPServer": "my_http_server",
        "HTTPServerSSL": "http_server_ssl",
        "HTTP_Server": "http__server",
        "foobar": "foobar",
        "foo_bar": "foo__bar",
    },
}


def member_name(procedure, style):
    """The name of the member a procedure is, in the client's case. A property is named after
    itself, or after each accessor."""
    convert = style.convert
    if Attributes.is_a_class_member(procedure):
        base = Attributes.get_class_member_name(procedure)
        is_property = Attributes.is_a_class_property_accessor(procedure)
        prefix = "set_" if Attributes.is_a_class_property_setter(procedure) else "get_"
    else:
        is_property = Attributes.is_a_property_accessor(procedure)
        base = Attributes.get_property_name(procedure) if is_property else procedure
        prefix = procedure[: len("get_")]
    if is_property and style.accessors:
        return prefix + convert(base)
    return convert(base)


def expected_members(services, service_name, name, style):
    """The names a client that builds its services exposes for a service, a class or an enum of it,
    from the server's definitions. Members are in the client's case, and types keep their
    names."""
    service = next(x for x in services.services if x.name == service_name)
    if name in [x.name for x in service.enumerations]:
        enumeration = next(x for x in service.enumerations if x.name == name)
        return {style.convert(x.name) for x in enumeration.values}
    names = set()
    if name:
        # A class member, which may be added by another service, takes the class as its
        # instance or is a static method of it
        for other in services.services:
            for procedure in other.procedures:
                if not Attributes.is_a_class_member(procedure.name):
                    continue
                if Attributes.get_class_name(procedure.name) != name:
                    continue
                static = Attributes.is_a_class_static_method(procedure.name)
                if static and other.name != service_name:
                    continue
                if not static:
                    instance = procedure.parameters[0].type
                    if (instance.service, instance.name) != (service_name, name):
                        continue
                names.add(member_name(procedure.name, style))
        return names
    for procedure in service.procedures:
        if not Attributes.is_a_class_member(procedure.name) and not procedure.internal:
            names.add(member_name(procedure.name, style))
    types = [service.classes, service.enumerations, service.structs]
    if style.exceptions:
        types.append(service.exceptions)
    for typs in types:
        names.update(x.name for x in typs)
    return names


def exposes_a_service(services, service):
    """Whether a client exposes a service: whether it has a type, or a procedure of its
    own. A member a service adds to another service's class belongs to that class."""
    if service.classes or service.enumerations or service.exceptions or service.structs:
        return True
    return any(
        not Attributes.is_a_class_member(x.name) for x in service.procedures
    ) or any(Attributes.is_a_class_static_method(x.name) for x in service.procedures)


ROSTERS = {
    "services": ("", ""),
    "KRPC": ("KRPC", ""),
    "TestService": ("TestService", ""),
    "TestClass": ("TestService", "TestClass"),
    "TestEnum": ("TestService", "TestEnum"),
    "TestClass2": ("TestService2", "TestClass2"),
}


section("Members")


def test_service_procedure(connected):
    """The client calls a service procedure."""
    assert connected.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


def test_no_return_value(connected):
    """A call to a procedure with no return type returns no value."""
    assert (
        connected.invoke("TestService", "ResetInvalidOperationExceptionLater") is None
    )


def test_service_property(connected):
    """The client gets and sets a service property, including one with only a getter or
    only a setter."""
    connected.invoke("TestService", "set_StringProperty", values.string("foo"))
    assert connected.invoke("TestService", "get_StringProperty") == "foo"
    # A property with only a getter, and one with only a setter
    assert connected.invoke("TestService", "get_StringPropertyPrivateSet") == "foo"
    connected.invoke(
        "TestService", "set_StringPropertyPrivateGet", values.string("bar")
    )


def test_class_method(connected):
    """The client calls a method of an object, including one that takes another
    object."""
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))
    assert connected.invoke("TestService", "TestClass_GetValue", values.obj(obj)) == (
        "value=alpha"
    )
    assert (
        connected.invoke(
            "TestService",
            "TestClass_FloatToString",
            values.obj(obj),
            values.float32(3.14159),
        )
        == "alpha3.14159"
    )
    other = connected.invoke("TestService", "CreateTestObject", values.string("beta"))
    assert (
        connected.invoke(
            "TestService",
            "TestClass_ObjectToString",
            values.obj(obj),
            values.obj(other),
        )
        == "alphabeta"
    )


def test_class_static_method(connected):
    """The client calls a static method of a class."""
    result = connected.invoke(
        "TestService",
        "TestClass_static_StaticMethod",
        values.string("a"),
        values.string("b"),
    )
    assert result == "staticab"


def test_class_property(connected):
    """The client gets and sets a property of an object, including one with only a
    getter or only a setter."""
    obj = values.obj(create(connected))
    for value in (0, 42):
        connected.invoke(
            "TestService", "TestClass_set_IntProperty", obj, values.sint32(value)
        )
        assert (
            connected.invoke("TestService", "TestClass_get_IntProperty", obj) == value
        )
    # A property with only a setter, and one with only a getter, of one value
    connected.invoke(
        "TestService",
        "TestClass_set_StringPropertyPrivateGet",
        obj,
        values.string("beta"),
    )
    result = connected.invoke(
        "TestService", "TestClass_get_StringPropertyPrivateSet", obj
    )
    assert result == "beta"


def test_class_extension_members(connected):
    """The client calls the members another service adds to a class, including one
    returning a class of a third service."""
    obj = values.obj(create(connected))
    service = "TestServiceExtensions"
    assert connected.invoke(
        service, "TestClass_ExtensionMethod", obj, values.sint32(42)
    ) == ("value=alpha42")
    assert (
        connected.invoke(service, "TestClass_get_ExtensionProperty", obj)
        == "value=alpha"
    )
    connected.invoke(
        service, "TestClass_set_ExtensionReadWriteProperty", obj, values.sint32(7)
    )
    assert (
        connected.invoke(service, "TestClass_get_ExtensionReadWriteProperty", obj) == 7
    )
    # The extension property stores its value in the class's own property
    assert connected.invoke("TestService", "TestClass_get_IntProperty", obj) == 7
    other = connected.invoke(
        service, "TestClass_ExtensionMethodReturningClassFromOtherService", obj
    )
    assert connected.invoke(
        "TestService2", "TestClass2_get_Value", values.obj(other)
    ) == ("value=alpha")


section(
    "Deprecated Members",
    "A client signals the use of a deprecated member, so that a program can move to its "
    "replacement. The signal is the language's own: a warning at run time, or a warning from the "
    "compiler. The member still works.",
)


def deprecated_object(connected):
    """A value of the deprecated class, which the testee holds."""
    return values.obj(connected.invoke("TestService", "CreateDeprecatedClass"))


@should
def test_deprecated_procedure(connected):
    """A client signals a call of a deprecated procedure."""
    assert connected.deprecation_signals(
        "TestService", "DeprecatedProcedure", "DeprecatedProcedure", values.float32(1)
    )


@should
@pytest.mark.parametrize(
    ("procedure", "args"),
    [("get_DeprecatedProperty", ()), ("set_DeprecatedProperty", ("value",))],
    ids=["getter", "setter"],
)
def test_deprecated_property(connected, procedure, args):
    """A client signals a read and a write of a deprecated property."""
    args = [values.string(x) for x in args]
    assert connected.deprecation_signals(
        "TestService", "DeprecatedProperty", procedure, *args
    )


@should
def test_deprecated_class(connected):
    """A client signals a use of a deprecated class."""
    assert connected.deprecation_signals("TestService", "DeprecatedClass")


@should
def test_deprecated_method(connected):
    """A client signals a call of a deprecated method."""
    obj = deprecated_object(connected)
    assert connected.deprecation_signals(
        "TestService",
        "DeprecatedClass.DeprecatedMethod",
        "DeprecatedClass_DeprecatedMethod",
        obj,
    )


@should
def test_deprecated_enum(connected):
    """A client signals a use of a deprecated enumeration."""
    assert connected.deprecation_signals("TestService", "DeprecatedEnum")


@should
def test_deprecated_enum_value(connected):
    """A client signals a use of a deprecated value of an enumeration that is not deprecated,
    and no use of its other values."""
    assert connected.deprecation_signals("TestService", "PartlyDeprecatedEnum.ValueB")
    assert not connected.deprecation_signals(
        "TestService", "PartlyDeprecatedEnum.ValueA"
    )


@should
def test_deprecated_struct(connected):
    """A client signals a use of a deprecated structure."""
    assert connected.deprecation_signals("TestService", "DeprecatedStruct")


@should
def test_deprecated_struct_field(connected):
    """A client signals a read of a deprecated field of a structure that is not deprecated, and
    no read of its other fields."""
    assert connected.deprecation_signals(
        "TestService", "PartlyDeprecatedStruct.OldValue"
    )
    assert not connected.deprecation_signals(
        "TestService", "PartlyDeprecatedStruct.Value"
    )


@should
def test_deprecated_exception(connected):
    """A client signals a use of a deprecated exception."""
    assert connected.deprecation_signals("TestService", "DeprecatedException")


# The members of the deprecated class with no deprecation of their own, and the calls that use them
INHERITED_CLASS_MEMBERS = {
    "method": ("InheritedMethod", "DeprecatedClass_InheritedMethod", True),
    "static-method": (
        "InheritedStaticMethod",
        "DeprecatedClass_static_InheritedStaticMethod",
        False,
    ),
    "property": ("InheritedProperty", "DeprecatedClass_get_InheritedProperty", True),
}


@should
@pytest.mark.parametrize("member", list(INHERITED_CLASS_MEMBERS))
def test_deprecated_class_member(connected, member):
    """A member of a deprecated class is deprecated, with no deprecation of its own."""
    name, procedure, instance = INHERITED_CLASS_MEMBERS[member]
    args = [deprecated_object(connected)] if instance else []
    assert connected.deprecation_signals(
        "TestService", "DeprecatedClass." + name, procedure, *args
    )


@should
def test_deprecated_enum_member(connected):
    """A value of a deprecated enumeration is deprecated, with no deprecation of its own."""
    assert connected.deprecation_signals("TestService", "DeprecatedEnum.ValueA")


@should
def test_deprecated_struct_member(connected):
    """A field of a deprecated structure is deprecated, with no deprecation of its own."""
    assert connected.deprecation_signals("TestService", "DeprecatedStruct.Value")


@should
def test_deprecated_service(connected):
    """A client signals a use of a deprecated service, and no use of a service that is not
    deprecated."""
    assert connected.deprecation_signals("DeprecatedService", "")
    assert not connected.deprecation_signals("TestService", "")


# The members of the deprecated service, and the procedures that use them
DEPRECATED_SERVICE_MEMBERS = {
    "procedure": ("Procedure", "Procedure"),
    "property": ("Property", "get_Property"),
    "class": ("ServiceClass", ""),
    "class-method": ("ServiceClass.StaticMethod", "ServiceClass_static_StaticMethod"),
    "enum": ("ServiceEnum", ""),
    "enum-value": ("ServiceEnum.Value", ""),
    "struct": ("ServiceStruct", ""),
    "struct-field": ("ServiceStruct.Field", ""),
    "exception": ("ServiceException", ""),
}


@should
@pytest.mark.parametrize("member", list(DEPRECATED_SERVICE_MEMBERS))
def test_deprecated_service_member(connected, member):
    """A member of a deprecated service is deprecated, with no deprecation of its own."""
    name, procedure = DEPRECATED_SERVICE_MEMBERS[member]
    assert connected.deprecation_signals("DeprecatedService", name, procedure)


# The deprecated procedures a program can stream or build the call of, the calls that use them,
# and the arguments of each call
DEPRECATED_CALLS = {
    "procedure": (
        "DeprecatedProcedure",
        "DeprecatedProcedure",
        lambda connected: [values.float32(1)],
    ),
    "property": ("DeprecatedProperty", "get_DeprecatedProperty", lambda connected: []),
    "method": (
        "DeprecatedClass.DeprecatedMethod",
        "DeprecatedClass_DeprecatedMethod",
        lambda connected: [deprecated_object(connected)],
    ),
}


def assert_signals_use(connected, member, use):
    """Check that a use of a deprecated procedure signals, and the same use of a procedure that
    is not deprecated does not."""
    name, procedure, args = DEPRECATED_CALLS[member]
    args = args(connected)
    assert connected.deprecation_signals("TestService", name, procedure, *args, use=use)
    assert not connected.deprecation_signals(
        "TestService", "FloatToString", "FloatToString", values.float32(1), use=use
    )


@should
@profile("streams")
@pytest.mark.parametrize("member", list(DEPRECATED_CALLS))
def test_deprecated_stream(connected, member):
    """A client signals a stream of a deprecated procedure, method or property."""
    assert_signals_use(connected, member, "stream")


@should
@profile("functions")
@pytest.mark.parametrize("member", list(DEPRECATED_CALLS))
def test_deprecated_call(connected, member):
    """A client signals the call of a deprecated procedure, method or property that it builds
    for a server side function."""
    assert_signals_use(connected, member, "call")


def assert_reason(signals, reason):
    """Check that each signal gives the reason, as the client writes names in it."""
    assert signals
    for signal in signals:
        assert reason in signal.replace("_", "").lower()


@should
def test_deprecated_reason(connected):
    """A signal gives the reason for the deprecation."""
    signals = connected.deprecation_signals(
        "TestService",
        "DeprecatedProcedure",
        "DeprecatedProcedure",
        values.float32(1),
        reason=True,
    )
    assert_reason(signals, "floattostring")


@should
def test_deprecated_reason_of_container(connected):
    """A member with no reason of its own takes the reason of its service or class."""
    signals = connected.deprecation_signals(
        "DeprecatedService", "Procedure", "Procedure", reason=True
    )
    assert_reason(signals, "use testservice instead")
    obj = deprecated_object(connected)
    signals = connected.deprecation_signals(
        "TestService",
        "DeprecatedClass.InheritedMethod",
        "DeprecatedClass_InheritedMethod",
        obj,
        reason=True,
    )
    assert_reason(signals, "use testclass instead")


@should
def test_deprecated_reason_of_member(connected):
    """A member with a reason of its own keeps it, in a deprecated class."""
    obj = deprecated_object(connected)
    signals = connected.deprecation_signals(
        "TestService",
        "DeprecatedClass.DeprecatedMethod",
        "DeprecatedClass_DeprecatedMethod",
        obj,
        reason=True,
    )
    assert_reason(signals, "getvalue")


@should
def test_deprecated_reason_of_class(connected):
    """A member with no reason of its own, of a deprecated class in a deprecated service, takes
    the reason of its class."""
    signals = connected.deprecation_signals(
        "DeprecatedService",
        "ReasonedClass.StaticMethod",
        "ReasonedClass_static_StaticMethod",
        reason=True,
    )
    assert_reason(signals, 'use "testservice.testclass" instead')


# Members that are not deprecated, and the calls that use them
NOT_DEPRECATED = {
    "procedure": ("FloatToString", "FloatToString", [values.float32(1)]),
    "property": ("StringProperty", "set_StringProperty", [values.string("value")]),
    "class": ("TestClass", "", []),
    "enum": ("TestEnum", "", []),
    "enum-value": ("TestEnum.ValueA", "", []),
    "struct": ("TestStruct", "", []),
    "struct-field": ("TestStruct.IntField", "", []),
    "exception": ("CustomException", "", []),
}


@pytest.mark.parametrize("member", list(NOT_DEPRECATED))
def test_not_deprecated(connected, member):
    """A client gives no signal for a member that is not deprecated."""
    name, procedure, args = NOT_DEPRECATED[member]
    assert not connected.deprecation_signals("TestService", name, procedure, *args)


section(
    "Service Definitions",
    "These requirements apply to a client that builds its services from the server's "
    "definitions when it connects.",
)


def with_definitions(client, server, scripted_server, *added):
    """Connect the client to a scripted server whose definitions are the KRPC service's and
    the added services, in order. Every call but GetServices gets a zero value."""
    services = KRPC.Services.FromString(definitions(server, "KRPC"))
    services.services.extend(added)
    return serve_definitions(client, scripted_server, services)


@profile("definitions")
@pytest.mark.parametrize("roster", list(ROSTERS))
def test_exposed_members(connected, control, roster):
    """The client exposes exactly the services the server defines, and for each service its
    procedures other than internal ones, properties, classes, enumerations and structures. A
    client in a language with exception types also exposes the service's exceptions. Each class
    exposes its methods, static methods and properties, including those another service adds,
    and each enumeration its values."""
    service, name = ROSTERS[roster]
    style = connected.conventions.naming
    exposed = connected.members(service, name)
    assert exposed is not None
    services = control.krpc.get_services()
    if not service:
        expected = {
            style.convert(x.name)
            for x in services.services
            if exposes_a_service(services, x)
        }
    else:
        expected = expected_members(services, service, name, style)
    assert exposed == expected


@profile("definitions")
def test_member_name_case(client, server, scripted_server):
    """Member names are in the case the language names its members in. In snake case, an
    underscore goes before each capital that starts a word, where a word starts after a lower
    case letter or a digit, or before the last capital of a run followed by a lower case
    letter. An underscore in the name is doubled."""
    names = NAMES[client.conventions.naming.case]
    enumeration = KRPC.Enumeration(name="Names")
    for i, name in enumerate(names):
        enumeration.values.add(name=name, value=i)
    with_definitions(
        client,
        server,
        scripted_server,
        KRPC.Service(name="Names", enumerations=[enumeration]),
    )
    try:
        assert client.members("Names", "Names") == set(names.values())
    finally:
        client.close()


@profile("definitions")
def test_internal_procedure(client, server, scripted_server):
    """The client leaves out a procedure the server marks internal. The client library calls
    an internal procedure itself, to implement streams, events and server side functions."""
    service = KRPC.Service(
        name="Internals",
        procedures=[
            KRPC.Procedure(name="Visible"),
            KRPC.Procedure(name="Concealed", internal=True),
        ],
    )
    with_definitions(client, server, scripted_server, service)
    try:
        style = client.conventions.naming
        assert client.members("Internals", "") == {style.convert("Visible")}
    finally:
        client.close()


# A type with the code 9999, from a later version of the protocol than any client knows.
# Encoded by hand, as the code is not a TypeCode.
UNKNOWN_TYPE = KRPC.Type.FromString(b"\x08\x8f\x4e")


def struct_type(service, name):
    return KRPC.Type(code=KRPC.Type.STRUCT, service=service, name=name)


@profile("definitions")
def test_unknown_type(client, server, scripted_server):
    """The client leaves out a member or a structure whose type has a code the protocol
    does not define, as a later version of the server can send, and anything that uses
    one. The rest of the service works."""
    service = KRPC.Service(name="Later")
    service.structs.add(name="Thing").fields.add(name="Value", type=UNKNOWN_TYPE)
    service.structs.add(name="Holder").fields.add(
        name="Thing", type=struct_type("Later", "Thing")
    )
    service.procedures.add(name="Unknown").parameters.add(name="x", type=UNKNOWN_TYPE)
    service.procedures.add(name="UsesThing").parameters.add(
        name="thing", type=struct_type("Later", "Thing")
    )
    service.procedures.add(name="DefaultsThing").parameters.add(
        name="thing",
        type=struct_type("Later", "Thing"),
        has_default_value=True,
        default_value=KRPC.Tuple().SerializeToString(),
    )
    service.procedures.add(
        name="UsesHolder", return_type=struct_type("Later", "Holder")
    )
    known = service.procedures.add(
        name="Known", return_type=KRPC.Type(code=KRPC.Type.STRING)
    )
    known.parameters.add(name="x", type=KRPC.Type(code=KRPC.Type.SINT32))
    with_definitions(client, server, scripted_server, service)
    try:
        style = client.conventions.naming
        assert client.members("Later") == {member_name("Known", style)}
        assert client.invoke("Later", "Known", values.sint32(1)) == ""
    finally:
        client.close()


@profile("definitions")
@pytest.mark.parametrize(
    "case",
    ["class", "enumeration", "structure", "enumeration-default", "wrong-kind"],
)
def test_undefined_type(client, server, scripted_server, case):
    """The client leaves out a member whose type names a class, an enumeration or a
    structure that no service defines, including a parameter with a default value. A
    type of the same name but another kind does not define it. The rest of the service
    works."""
    service = KRPC.Service(name="Later")
    code = {
        "class": KRPC.Type.CLASS,
        "enumeration": KRPC.Type.ENUMERATION,
        "structure": KRPC.Type.STRUCT,
        "enumeration-default": KRPC.Type.ENUMERATION,
        "wrong-kind": KRPC.Type.CLASS,
    }[case]
    owner = "Later" if case == "wrong-kind" else "Missing"
    if case == "wrong-kind":
        service.enumerations.add(name="Thing").values.add(name="One", value=1)
    parameter = service.procedures.add(name="UsesThing").parameters.add(
        name="thing", type=KRPC.Type(code=code, service=owner, name="Thing")
    )
    if case == "enumeration-default":
        parameter.has_default_value = True
        parameter.default_value = encode_sint32(1)
    known = service.procedures.add(
        name="Known", return_type=KRPC.Type(code=KRPC.Type.STRING)
    )
    known.parameters.add(name="x", type=KRPC.Type(code=KRPC.Type.SINT32))
    with_definitions(client, server, scripted_server, service)
    try:
        style = client.conventions.naming
        assert member_name("UsesThing", style) not in client.members("Later")
        assert member_name("Known", style) in client.members("Later")
        assert client.invoke("Later", "Known", values.sint32(1)) == ""
    finally:
        client.close()


@profile("definitions")
@pytest.mark.parametrize("case", ["list-element-types", "structure-twice"])
def test_malformed_definitions(client, server, scripted_server, case):
    """Service definitions the client cannot build its services from, such as a list type
    with two element types or a structure defined twice, make the connect raise the
    connection error."""
    service = KRPC.Service(name="Later")
    sint32 = KRPC.Type(code=KRPC.Type.SINT32)
    if case == "list-element-types":
        service.procedures.add(
            name="Broken",
            return_type=KRPC.Type(code=KRPC.Type.LIST, types=[sint32] * 2),
        )
    else:
        service.structs.add(name="Thing").fields.add(name="Value", type=sint32)
        service.structs.add(name="Thing").fields.add(name="Other", type=sint32)
    with pytest.raises(TesteeError) as info:
        with_definitions(client, server, scripted_server, service)
    assert_error_kind(client, info.value.error, pb.Error.CONNECTION)


@profile("definitions")
@pytest.mark.parametrize(
    "case", ["defined-before", "defined-after", "in-a-list", "same-service", "flags"]
)
def test_enumeration_default(client, server, scripted_server, case):
    """A procedure's parameter defaults to a value of an enumeration, alone or in a
    collection, or to a combination of the values of a flags enumeration. The client calls
    the procedure without the argument, and sends none. The enumeration is of the same
    service or another, in either order in the definitions."""
    modes = KRPC.Service(name="Modes")
    user = KRPC.Service(name="User")
    owner = user if case == "same-service" else modes
    enumeration = owner.enumerations.add(name="Mode", flags=case == "flags")
    enumeration.values.add(name="Slow", value=1)
    enumeration.values.add(name="Fast", value=2)
    typ = KRPC.Type(code=KRPC.Type.ENUMERATION, service=owner.name, name="Mode")
    # The enumeration value 2, or the combination of both values
    default = encode_sint32(3 if case == "flags" else 2)
    if case == "in-a-list":
        typ = KRPC.Type(code=KRPC.Type.LIST, types=[typ])
        default = KRPC.List(items=[default]).SerializeToString()
    user.procedures.add(name="Frob").parameters.add(
        name="mode", type=typ, has_default_value=True, default_value=default
    )
    order = [modes, user] if case == "defined-before" else [user, modes]
    scripted = with_definitions(client, server, scripted_server, *order)
    try:
        client.invoke("User", "Frob")
        (call,) = scripted.wait_for_call("Frob")
        assert list(call.arguments) == []
    finally:
        client.close()


@profile("definitions")
@pytest.mark.parametrize("case", ["defined-before", "defined-after"])
def test_structure_default(client, server, scripted_server, case):
    """A procedure's parameter defaults to a value of a structure, which holds a structure
    of another service. The client calls the procedure without the argument, and sends none.
    The other service comes before or after in the definitions."""
    inners = KRPC.Service(name="Inners")
    inners.structs.add(name="Inner").fields.add(
        name="Value", type=KRPC.Type(code=KRPC.Type.SINT32)
    )
    user = KRPC.Service(name="User")
    user.structs.add(name="Outer").fields.add(
        name="Inner", type=struct_type("Inners", "Inner")
    )
    inner = KRPC.Tuple(items=[encode_sint32(5)]).SerializeToString()
    user.procedures.add(name="Frob").parameters.add(
        name="outer",
        type=struct_type("User", "Outer"),
        has_default_value=True,
        default_value=KRPC.Tuple(items=[inner]).SerializeToString(),
    )
    order = [inners, user] if case == "defined-before" else [user, inners]
    scripted = with_definitions(client, server, scripted_server, *order)
    try:
        client.invoke("User", "Frob")
        (call,) = scripted.wait_for_call("Frob")
        assert list(call.arguments) == []
    finally:
        client.close()


section("Arguments")


def test_several_arguments(connected):
    """The client passes several arguments of different types."""
    result = connected.invoke(
        "TestService",
        "AddMultipleValues",
        values.float32(0.14159),
        values.sint32(1),
        values.sint64(2),
    )
    assert result == "3.14159"


@profile("definitions")
def test_long_request(client, server, scripted_server):
    """A call whose service name, procedure name and argument are longer than 127 bytes,
    with 200 arguments, arrives intact."""
    service = KRPC.Service(name="Service" + "x" * 200)
    procedure = service.procedures.add(name="Procedure" + "x" * 200)
    procedure.parameters.add(name="text", type=KRPC.Type(code=KRPC.Type.STRING))
    for i in range(199):
        procedure.parameters.add(name="x%d" % i, type=KRPC.Type(code=KRPC.Type.SINT32))
    services = KRPC.Services.FromString(definitions(server, "KRPC"))
    services.services.append(service)

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.serve()

    scripted = scripted_server(script)
    scripted.services = services.SerializeToString()
    client.connect(scripted.endpoint())
    try:
        text = "t" * 300
        arguments = [values.sint32(i) for i in range(199)]
        client.invoke(service.name, procedure.name, values.string(text), *arguments)
        (call,) = scripted.wait_for_call(procedure.name)
    finally:
        client.close()
    assert call.service == service.name
    assert [x.position for x in call.arguments] == list(range(200))
    assert call.arguments[0].value == encode_size(len(text)) + text.encode()
    assert [x.value for x in call.arguments[1:]] == [
        encode_sint32(i) for i in range(199)
    ]


@profile("defaults")
@pytest.mark.parametrize("kind", ["procedure", "method", "static-method"])
def test_omitted_arguments(connected, kind):
    """The client omits trailing arguments that have defaults, from a procedure, a
    method or a static method."""
    a, b, c = values.string("a"), values.string("b"), values.string("c")
    assert optional_arguments(connected, kind, a) == "afoobarnull"
    assert optional_arguments(connected, kind, a, b) == "abbarnull"
    assert optional_arguments(connected, kind, a, b, c) == "abcnull"
    obj = connected.invoke("TestService", "CreateTestObject", values.string("beta"))
    result = optional_arguments(connected, kind, a, b, c, values.obj(obj))
    assert result == "abcbeta"


@profile("defaults")
@pytest.mark.parametrize("case", list(DEFAULTS))
def test_default_values(connected, case):
    """An omitted argument takes its default, of any type. ``NaN``, the infinities and
    the extremes of each numeric type arrive intact."""
    procedure, expected = DEFAULTS[case]
    result = connected.invoke("TestService", procedure)
    assert same(result, expected), result


@profile("named-arguments")
@pytest.mark.parametrize("kind", ["procedure", "method", "static-method"])
def test_named_arguments(connected, kind):
    """The client passes arguments by name, and omits a default that is not trailing. A
    duplicate or unknown name raises the argument error."""
    a, c = values.string("a"), values.string("c")
    assert optional_arguments(connected, kind, a, named={"z": c}) == "afoocnull"
    assert optional_arguments(connected, kind, named={"z": c, "x": a}) == "afoocnull"
    obj = connected.invoke("TestService", "CreateTestObject", values.string("beta"))
    result = optional_arguments(connected, kind, a, named={"obj": values.obj(obj)})
    assert result == "afoobarbeta"
    with pytest.raises(TesteeError) as error:
        optional_arguments(connected, kind, a, named={"x": a})
    assert_argument_error(connected, error.value.error)
    with pytest.raises(TesteeError) as error:
        optional_arguments(connected, kind, a, named={"w": a})
    assert_argument_error(connected, error.value.error)


@profile("defaults", "skipped-arguments")
@pytest.mark.parametrize("kind", ["procedure", "method", "static-method"])
def test_skipped_arguments(connected, kind):
    """The client skips an argument that has a default, by position, and passes the
    arguments after it. Skipping an argument with no default, at a parameter that cannot
    hold null, raises the argument error."""
    a, b, c = values.string("a"), values.string("b"), values.string("c")
    skip = values.skipped()
    assert optional_arguments(connected, kind, a, skip, c) == "afoocnull"
    obj = connected.invoke("TestService", "CreateTestObject", values.string("beta"))
    result = optional_arguments(connected, kind, a, skip, skip, values.obj(obj))
    assert result == "afoobarbeta"
    with pytest.raises(TesteeError) as error:
        optional_arguments(connected, kind, skip, b)
    assert_argument_error(connected, error.value.error)


@profile("dynamic")
@pytest.mark.parametrize("kind", ["procedure", "method"])
def test_too_few_arguments(connected, kind):
    """Omitting an argument with no default raises the argument error."""
    with pytest.raises(TesteeError) as info:
        argument_count_call(connected, kind, 0)
    assert_argument_error(connected, info.value.error)


@profile("dynamic")
@pytest.mark.parametrize("kind", ["procedure", "method"])
def test_too_many_arguments(connected, kind):
    """Too many arguments raise the argument error."""
    with pytest.raises(TesteeError) as info:
        argument_count_call(connected, kind, 2)
    assert_argument_error(connected, info.value.error)


section("Responses")


# A procedure, and an encoded argument that is not a value of its parameter's type
UNDECODABLE_ARGUMENTS = {
    "left-over-bytes": ("Int32ToString", b"\x02\x00"),
    "truncated": ("DoubleToString", b"\x00\x00\x00\x00"),
    "sint32-out-of-range": ("Int32ToString", b"\x80\x80\x80\x80\x10"),
    "uint32-out-of-range": ("UInt32ToString", b"\x80\x80\x80\x80\x10"),
    "undeclared-enum": ("EnumToString", b"\xc6\x01"),
    "bool-out-of-range": ("BoolToString", b"\x02"),
    "unknown-field": ("IncrementList", b"\x10\x01"),
    "wrong-sized-tuple": (
        "IncrementTuple",
        KRPC.Tuple(items=[b"\x02"]).SerializeToString(),
    ),
}


def argument_error_of(server, call):
    """Send the call between two valid calls, and return the ``ArgumentException`` it fails
    with, having checked the other two ran."""
    valid = raw.procedure_call("TestService", "Int32ToString", b"\x02")
    first, failed, last = raw.request(server, valid, call, valid)
    assert (failed.error.service, failed.error.exception) == (
        "KRPC",
        "ArgumentException",
    )
    assert not first.HasField("error") and first.value == b"\x011"
    assert not last.HasField("error") and last.value == b"\x011"
    return failed.error


@pytest.mark.parametrize("case", list(UNDECODABLE_ARGUMENTS))
def test_undecodable_argument_fails_its_call(server, case):
    """A call with an argument the server cannot decode fails with the server's
    ``ArgumentException``, in the ``error`` field of its result. The other calls in the
    request run, and their results stay in order."""
    procedure, argument = UNDECODABLE_ARGUMENTS[case]
    argument_error_of(server, raw.procedure_call("TestService", procedure, argument))


@pytest.mark.parametrize("position", [1, 2**32 - 1])
def test_argument_position_out_of_range_fails_its_call(server, position):
    """A call with an argument position past the procedure's last parameter fails with the
    server's ``ArgumentException``, in the ``error`` field of its result. The description
    names the position and the procedure. The other calls in the request run, and their
    results stay in order."""
    call = raw.procedure_call("TestService", "Int32ToString", b"\x02")
    call.arguments.add(position=position, value=b"\x02")
    error = argument_error_of(server, call)
    assert str(position) in error.description
    assert "TestService.Int32ToString" in error.description


def test_duplicate_argument_position_fails_its_call(server):
    """A call with two arguments at one position fails with the server's
    ``ArgumentException``, in the ``error`` field of its result. The description names the
    position and the procedure. The other calls in the request run, and their results stay
    in order."""
    call = raw.procedure_call("TestService", "Int32ToString", b"\x02")
    call.arguments.add(position=0, value=b"\x04")
    error = argument_error_of(server, call)
    assert "position 0" in error.description
    assert "TestService.Int32ToString" in error.description


@profile("concurrency")
def test_unique_ids(client, server, scripted_server):
    """Each request has an id that no other waiting request has."""
    held = []
    scripted = scripted_server(held_calls(server, 3, list, held))
    client.connect(scripted.endpoint())
    try:
        calls = [client.start("caller%d" % i, invoke=to_string(i)) for i in range(3)]
        for i, call in enumerate(calls):
            assert call.result() == str(i)
    finally:
        client.close()
    assert len({x.id for x in held}) == 3


@profile("concurrency")
def test_match_by_id(client, server, scripted_server):
    """The client matches a response to its call by the id, in any order the responses
    arrive."""
    held = []
    scripted = scripted_server(
        held_calls(server, 3, lambda xs: list(reversed(xs)), held)
    )
    client.connect(scripted.endpoint())
    try:
        calls = [client.start("caller%d" % i, invoke=to_string(i)) for i in range(3)]
        for i, call in enumerate(calls):
            assert call.result() == str(i)
    finally:
        client.close()


@should
@profile("concurrency")
def test_abandoned_id(client, server, scripted_server):
    """A call interrupted while waiting abandons its id, and raises the language's error for
    an interrupted thread. The client discards the response to that id."""
    held = []
    scripted = scripted_server(held_calls(server, 2, list, held))
    client.connect(scripted.endpoint())
    try:
        abandoned = client.start("abandoned", invoke=to_string(1))
        assert wait_until(lambda: len(held) == 1)
        client.interrupt("abandoned")
        assert_interrupt_error(client, abandoned.error())
        # The response to the abandoned call arrives first
        assert client.invoke("TestService", "Int32ToString", values.sint32(2)) == "2"
    finally:
        client.close()


def test_unknown_id(client, server, scripted_server):
    """A response whose id matches no waiting or abandoned request fails the connection.
    The client sends a ``disconnect`` with the reason ``PROTOCOL_ERROR``."""
    services = definitions(server, "KRPC")
    reasons = []

    def script(server: ScriptedServer) -> None:
        server.services = services
        server.accept()
        while True:
            message = server.receive()
            if message is None:
                return
            if message.WhichOneof("content") == "disconnect":
                reasons.append(KRPC.Disconnect.Reason.Name(message.disconnect.reason))
                return
            response = server.response(message.request)
            if is_call(message.request.calls[0], "GetClientName"):
                response.id += 1000
            server.send(KRPC.Message(response=response))

    scripted = scripted_server(script)
    client.connect(scripted.endpoint())
    try:
        with pytest.raises(TesteeError) as info:
            client.invoke("KRPC", "GetClientName")
        assert info.value.error.kind == pb.Error.CONNECTION, info.value.error
    finally:
        client.close_quietly()
    scripted.join()
    assert reasons == ["PROTOCOL_ERROR"]


def test_slow_call(client, server, scripted_server):
    """A call waits for its response for as long as the procedure runs. The handshake
    timeout does not apply to the wait."""
    services = definitions(server, "KRPC")

    def script(server: ScriptedServer) -> None:
        server.services = services
        server.accept()
        while True:
            message = server.receive()
            if message is None or message.WhichOneof("content") != "request":
                return
            # A client that names procedures by id sends no names
            if message.request.calls[0].procedure != "GetServices":
                time.sleep(1.5)
            server.answer(message.request)

    scripted = scripted_server(script)
    client.connect(scripted.endpoint(), handshake_timeout=0.5)
    try:
        start = time.perf_counter()
        assert client.invoke("KRPC", "GetClientName") == ""
        assert time.perf_counter() - start >= 1.4
    finally:
        client.close()
