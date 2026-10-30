"""Server side function requirements.

Functions are built through the client's KRPC.Expression and KRPC.Type stubs. A function that
calls ConformanceService.Value lets a test choose when its value changes or fails.
"""

import math

import pytest
import testee_pb2 as pb
from driver import profile, section, values
from driver.errors import assert_argument_error, assert_error_kind, is_removed_error
from driver.functions import Functions
from driver.helpers import ended, name, raised
from driver.testee import REPLY_TIMEOUT, wait_until


@pytest.fixture
def functions(connected):
    return Functions(connected)


# The cases of a function value type
VALUE_TYPES = [
    "double",
    "nullable sint32",
    "list(sint32)",
    "empty list(sint32)",
    "list(nullable sint32)",
    "TestClass",
    "list(nullable TestClass)",
    "dictionary(string,nullable TestClass)",
    "tuple(sint32,nullable TestClass)",
    "TestStruct",
]

# The extremes of each integer type
SINT32 = (-(2**31), 2**31 - 1)
SINT64 = (-(2**63), 2**63 - 1)

# Values of each type, by case, with their type. The object is made by the test.
TYPES = {
    "double": ("double", values.double(1.5)),
    "double-nan": ("double", values.double(math.nan)),
    "double-infinity": ("double", values.double(math.inf)),
    "double-negative-infinity": ("double", values.double(-math.inf)),
    "float": ("float", values.float32(1.5)),
    "float-nan": ("float", values.float32(math.nan)),
    "float-infinity": ("float", values.float32(math.inf)),
    "float-negative-infinity": ("float", values.float32(-math.inf)),
    "sint32-min": ("sint32", values.sint32(SINT32[0])),
    "sint32-max": ("sint32", values.sint32(SINT32[1])),
    "sint64-min": ("sint64", values.sint64(SINT64[0])),
    "sint64-max": ("sint64", values.sint64(SINT64[1])),
    "uint32-max": ("uint32", values.uint32(2**32 - 1)),
    "uint64-max": ("uint64", values.uint64(2**64 - 1)),
    "bool": ("bool", values.boolean(True)),
    "string": ("string", values.string("foo")),
    "bytes": ("bytes", values.data(b"\x00\xff")),
    "object": ("TestClass", None),
    "enum": ("TestEnum", values.enum("TestService", "TestEnum", 1)),
    "struct": (
        "TestStruct",
        values.struct(
            "TestService",
            "TestStruct",
            values.sint32(1),
            values.string("foo"),
            values.enum("TestService", "TestEnum", 2),
            values.list_of([values.sint32(3)]),
        ),
    ),
    "tuple": (
        "tuple(sint32,bool)",
        values.tuple_of(values.sint32(1), values.boolean(True)),
    ),
    "list": ("list(sint32)", values.list_of([values.sint32(1), values.sint32(2)])),
    "set": ("set(sint32)", values.set_of([values.sint32(1), values.sint32(2)])),
    "nested-dictionary": (
        "dictionary(string,list(sint32))",
        values.dictionary([(values.string("a"), values.list_of([values.sint32(1)]))]),
    ),
    "null": ("nullable sint32", values.null()),
}

# The parameter type and the arguments of a run, for each wrong set of arguments
ARGUMENT_ERRORS = {
    "too-few": ("sint32", []),
    "too-many": (
        "sint32",
        [("sint32", values.sint32(1)), ("sint32", values.sint32(2))],
    ),
    "wrong-type": ("sint32", [("string", values.string("foo"))]),
    "null": ("sint32", [("nullable sint32", values.null())]),
    "list-item-type": (
        "list(sint32)",
        [("list(string)", values.list_of([values.string("foo")]))],
    ),
    "null-list-item": (
        "list(sint32)",
        [("list(nullable sint32)", values.list_of([values.sint32(1), values.null()]))],
    ),
    "short-tuple": (
        "tuple(sint32,bool)",
        [("tuple(sint32)", values.tuple_of(values.sint32(1)))],
    ),
    "out-of-range": ("sint32", [("sint64", values.sint64(2**31))]),
    "out-of-range-list-item": (
        "list(sint32)",
        [("list(sint64)", values.list_of([values.sint64(1), values.sint64(2**31)]))],
    ),
}

# The KRPC.Type static method for each scalar type
SCALAR_TYPES = {
    "double": "Double",
    "float": "Float",
    "sint32": "Int",
    "sint64": "Long",
    "uint32": "UInt",
    "uint64": "ULong",
    "bool": "Bool",
    "string": "String",
    "bytes": "Bytes",
}

# The KRPC.Type static method for each TestService type
SERVICE_TYPES = {
    "TestClass": "ClassType",
    "TestEnum": "EnumerationType",
    "TestStruct": "StructType",
}


def split_types(names):
    """The type names in a comma separated list, splitting only at the top level."""
    result, depth, start = [], 0, 0
    for i, c in enumerate(names):
        depth += {"(": 1, ")": -1}.get(c, 0)
        if c == "," and depth == 0:
            result.append(names[start:i])
            start = i + 1
    return [*result, names[start:]]


def krpc_type(functions, typ):
    """The KRPC.Type for a type the driver names."""
    if typ in SCALAR_TYPES:
        return functions.type(SCALAR_TYPES[typ])
    if typ in SERVICE_TYPES:
        return functions.type(
            SERVICE_TYPES[typ], values.string("TestService"), values.string(typ)
        )
    if typ.startswith("nullable "):
        return functions.type(
            "NullableType", krpc_type(functions, typ.removeprefix("nullable "))
        )
    kind, items = typ[:-1].split("(", 1)
    items = [krpc_type(functions, x) for x in split_types(items)]
    if kind == "tuple":
        return functions.type("TupleType", values.list_of(values.obj(x) for x in items))
    method = {"list": "ListType", "set": "SetType", "dictionary": "DictionaryType"}
    return functions.type(method[kind], *items)


def typed_function(connected, functions, case):
    """A function of a value type, its type and its value."""
    if case == "double":
        return functions.expression("ConstantDouble", values.double(2.5)), case, 2.5
    if case == "nullable sint32":
        nullable = krpc_type(functions, case)
        return functions.expression("ConstantNull", nullable), case, None
    if case == "list(sint32)":
        items = [functions.expression("ConstantInt", values.sint32(x)) for x in (1, 2)]
        function = functions.expression(
            "CreateList", values.list_of(values.obj(x) for x in items)
        )
        return function, case, [1, 2]
    if case == "empty list(sint32)":
        function = connected.call_function(
            "TestService", "EchoNullableList", values.list_of([])
        )
        return function, "list(sint32)", []
    if case == "list(nullable sint32)":
        function = connected.call_function(
            "TestService",
            "EchoListOfNullableInts",
            values.list_of([values.sint32(5), values.null()]),
        )
        return function, case, [5, None]
    if case == "list(nullable TestClass)":
        alpha = connected.invoke(
            "TestService", "CreateTestObject", values.string("alpha")
        )
        function = connected.call_function(
            "TestService",
            "EchoListOfNullableObjects",
            values.list_of([values.obj(alpha), values.null()]),
        )
        return function, case, ["value=alpha", None]
    if case == "dictionary(string,nullable TestClass)":
        alpha = connected.invoke(
            "TestService", "CreateTestObject", values.string("alpha")
        )
        function = connected.call_function(
            "TestService",
            "EchoDictionaryOfNullableObjects",
            values.dictionary(
                [
                    (values.string("a"), values.obj(alpha)),
                    (values.string("b"), values.null()),
                ]
            ),
        )
        return function, case, {"a": "value=alpha", "b": None}
    if case == "tuple(sint32,nullable TestClass)":
        function = connected.call_function(
            "TestService",
            "EchoTupleWithANullableObject",
            values.tuple_of(values.sint32(1), values.null()),
        )
        return function, case, (1, None)
    if case == "TestStruct":
        struct = TYPES["struct"][1]
        function = connected.call_function("TestService", "StructEcho", struct)
        return function, case, values.native(struct)
    assert case == "TestClass", case
    function = connected.call_function(
        "TestService", "CreateTestObject", values.string("alpha")
    )
    return function, case, "value=alpha"


# The cases of a type that does not describe a function's value
MISMATCHES = [
    "string-for-sint32",
    "list-for-sint32",
    "short-tuple",
    "list-for-set",
    "dictionary-value-type",
    "sint32-for-enum",
    "list-for-list-of-nullable",
]


def mismatched_function(connected, functions, case):
    """A function, and a type that does not describe its value."""
    one = functions.expression("ConstantInt", values.sint32(1))
    if case == "string-for-sint32":
        return one, "string"
    if case == "list-for-sint32":
        return one, "list(sint32)"
    if case == "short-tuple":
        true = functions.expression("ConstantBool", values.boolean(True))
        items = values.list_of([values.obj(one), values.obj(true)])
        return functions.expression("CreateTuple", items), "tuple(sint32)"
    if case == "list-for-set":
        return (
            functions.expression("CreateSet", values.set_of([values.obj(one)])),
            "list(sint32)",
        )
    if case == "dictionary-value-type":
        key = functions.expression("ConstantString", values.string("a"))
        function = functions.expression(
            "CreateDictionary",
            values.list_of([values.obj(key)]),
            values.list_of([values.obj(one)]),
        )
        return function, "dictionary(string,list(sint32))"
    if case == "sint32-for-enum":
        function = functions.expression(
            "ConstantEnum",
            values.string("TestService"),
            values.string("TestEnum"),
            values.sint32(1),
        )
        return function, "sint32"
    assert case == "list-for-list-of-nullable", case
    function = connected.call_function(
        "TestService",
        "EchoListOfNullableInts",
        values.list_of([values.sint32(5), values.null()]),
    )
    return function, "list(sint32)"


# The types that a null value can be read at, in a language whose type cannot hold null
NOT_NULLABLE = ["double", "TestClass"]


def null_function(connected, request, typ):
    """A function whose value is null, of the given type, and a function that sets the
    value and returns it, as typed_value gives it."""
    if typ == "double":
        function = connected.call_function(
            "ConformanceService", "Double", values.string(name(request))
        )

        def set_double():
            connected.invoke(
                "ConformanceService",
                "SetDouble",
                values.string(name(request)),
                values.double(1.5),
            )
            return 1.5

        return function, set_double
    assert typ == "TestClass", typ
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))
    function = connected.call_function(
        "TestService", "TestClass_get_ObjectProperty", values.obj(obj)
    )

    def set_object():
        connected.invoke(
            "TestService",
            "TestClass_set_ObjectProperty",
            values.obj(obj),
            values.obj(obj),
        )
        return "value=alpha"

    return function, set_object


def typed_value(connected, typ, value):
    """A function's value, with an object read through a call."""
    if value is None:
        return None
    if typ == "TestClass":
        return connected.invoke("TestService", "TestClass_GetValue", values.obj(value))
    if typ == "list(nullable TestClass)":
        return [
            None if x is None else typed_value(connected, "TestClass", x) for x in value
        ]
    if typ == "dictionary(string,nullable TestClass)":
        return {k: typed_value(connected, "TestClass", x) for k, x in value.items()}
    return value


section("Streams")


@profile("functions", "streams")
def test_stream(connected, functions, control, request):
    """A stream of a function delivers the function's value as it changes."""
    two = functions.expression("ConstantInt", values.sint32(2))
    double = functions.expression("Multiply", functions.value(name(request)), two)
    stream = connected.add_function_stream(double, "sint32")
    assert stream.get() == 0
    control.conformance_service.set_value(name(request), 21)
    assert wait_until(lambda: stream.get() == 42)


@profile("functions", "streams")
def test_stream_error(connected, functions, control, request):
    """An error from a function ends its stream with that error, and leaves other
    streams running."""
    failing = connected.add_function_stream(
        functions.value(name(request, "failing")), "sint32"
    )
    other = connected.add_function_stream(
        functions.value(name(request, "other")), "sint32"
    )
    assert failing.get() == 0
    assert other.get() == 0
    control.conformance_service.fail_value(
        name(request, "failing"), "Failed by the test"
    )
    assert wait_until(lambda: ended(failing))
    error = raised(failing.get)
    assert error.kind == pb.Error.RPC
    assert "Failed by the test" in error.message
    control.conformance_service.set_value(name(request, "other"), 1)
    assert wait_until(lambda: other.get() == 1)


@profile("functions", "streams")
def test_two_streams_of_one_function(connected, functions, control, request):
    """Each stream added for one function is a separate stream. Removing one leaves the
    other, and reading the removed one raises the error for a removed stream."""
    function = functions.value(name(request))
    first = connected.add_function_stream(function, "sint32")
    second = connected.add_function_stream(function, "sint32")
    assert first.get() == 0
    assert second.get() == 0
    first.remove()
    control.conformance_service.set_value(name(request), 1)
    assert wait_until(lambda: second.get() == 1)
    assert is_removed_error(connected, raised(first.get))


section("Events")


@profile("events", "functions")
def test_add_event(connected, functions, control, request):
    """An event added with ``KRPC.AddEvent``, over a server side function, fires."""
    one = functions.expression("ConstantInt", values.sint32(1))
    value = functions.value(name(request))
    event = connected.add_function_event(functions.expression("Equal", value, one))
    control.conformance_service.set_value(name(request), 1)
    assert event.wait(REPLY_TIMEOUT)


@profile("events", "functions")
def test_two_events_of_one_function(connected, functions, control, request):
    """Each event added for one function is a separate event. Removing one leaves the
    other."""
    one = functions.expression("ConstantInt", values.sint32(1))
    function = functions.expression("Equal", functions.value(name(request)), one)
    first = connected.add_function_event(function)
    second = connected.add_function_event(function)
    first.remove()
    control.conformance_service.set_value(name(request), 1)
    assert second.wait(REPLY_TIMEOUT)


section("Running")


@profile("functions")
def test_run(connected, functions):
    """Running a function passes the arguments to its parameters in order, and returns
    its value."""
    x = functions.parameter("x", functions.type("Int"))
    n = functions.parameter("n", functions.type("Int"))
    function = functions.lambda_([x, n], functions.expression("Subtract", x, n))
    result = connected.run_function(
        function, "sint32", ("sint32", values.sint32(6)), ("sint32", values.sint32(2))
    )
    assert result == 4
    result = connected.run_function(
        function, "sint32", ("sint32", values.sint32(1)), ("sint32", values.sint32(-4))
    )
    assert result == 5


@profile("functions")
@pytest.mark.parametrize("case", list(TYPES))
def test_argument_types(connected, functions, case):
    """A run passes an argument of each type to its parameter, and decodes a value of
    each type. ``NaN``, the infinities, the extremes of each integer type and a null
    arrive intact."""
    typ, argument = TYPES[case]
    if case == "object":
        alpha = connected.invoke(
            "TestService", "CreateTestObject", values.string("alpha")
        )
        argument = values.obj(alpha)
    x = functions.parameter("x", krpc_type(functions, typ))
    result = connected.run_function(functions.lambda_([x], x), typ, (typ, argument))
    if case.endswith("-nan"):
        assert math.isnan(result), result
    elif case == "object":
        assert typed_value(connected, typ, result) == "value=alpha"
    else:
        assert result == values.native(argument), result


@profile("functions")
def test_run_a_call(connected):
    """The client builds a function that makes a call with ``KRPC.Expression.Call``, from
    the same call it builds for a stream of the procedure. Running the function returns the
    call's result."""
    function = connected.call_function(
        "TestService", "Int32ToString", values.sint32(42)
    )
    assert connected.run_function(function, "string") == "42"


@profile("functions")
def test_run_error(connected, functions, control, request):
    """An error from a function raises that error from the run."""
    control.conformance_service.fail_value(name(request), "Failed by the test")
    error = raised(
        lambda: connected.run_function(functions.value(name(request)), "sint32")
    )
    assert error.kind == pb.Error.RPC, error
    assert "Failed by the test" in error.message


@profile("functions")
@pytest.mark.parametrize("case", list(ARGUMENT_ERRORS))
def test_run_argument_errors(connected, functions, case):
    """A wrong number of arguments, an argument of the wrong type, a number out of the range
    of the parameter's type, or a null for a parameter that cannot hold null, raises the
    argument error. This includes a collection whose items do not match the
    parameter's type. The error can be the server's ``KRPC.ArgumentException``, for a client
    that maps it to its argument error."""
    typ, args = ARGUMENT_ERRORS[case]
    x = functions.parameter("x", krpc_type(functions, typ))
    function = functions.lambda_([x], x)
    error = raised(lambda: connected.run_function(function, typ, *args))
    assert_argument_error(connected, error)


@profile("functions", "streams")
def test_stream_with_parameters(connected, functions):
    """Adding a stream for a function with parameters raises the client's argument error."""
    flag = functions.parameter("b", functions.type("Bool"))
    function = functions.lambda_([flag], flag)
    assert_argument_error(
        connected, raised(lambda: connected.add_function_stream(function, "bool"))
    )


@profile("events", "functions")
def test_event_with_parameters(connected, functions):
    """Adding an event for a function with parameters raises the client's argument error."""
    flag = functions.parameter("b", functions.type("Bool"))
    function = functions.lambda_([flag], flag)
    assert_argument_error(
        connected, raised(lambda: connected.add_function_event(function))
    )


section("Values")


@profile("functions")
@pytest.mark.parametrize("case", VALUE_TYPES)
def test_value_types(connected, functions, case):
    """A run decodes the function's value at its type. This holds for a floating point
    number, a null, a list, an empty list, a list holding a null, an object, a structure,
    and a list, a dictionary and a tuple that each hold a null object."""
    function, typ, expected = typed_function(connected, functions, case)
    result = connected.run_function(function, typ)
    assert typed_value(connected, typ, result) == expected


@profile("functions", "streams")
@pytest.mark.parametrize("case", VALUE_TYPES)
def test_stream_value_types(connected, functions, case):
    """A stream decodes the function's value at its type, for each type a run decodes."""
    function, typ, expected = typed_function(connected, functions, case)
    stream = connected.add_function_stream(function, typ)
    assert typed_value(connected, typ, stream.get()) == expected


@profile("functions", "function-types")
@pytest.mark.parametrize("case", MISMATCHES)
def test_mismatched_type(connected, functions, case):
    """A run at a type that does not describe the function's value raises the argument
    error. This holds for a tuple of another length, a set read as a list, a dictionary of
    another value type, an enumeration read as an integer, and a list that can hold null
    read as one that cannot."""
    function, typ = mismatched_function(connected, functions, case)
    assert_argument_error(
        connected, raised(lambda: connected.run_function(function, typ))
    )


@profile("functions", "function-types", "streams")
@pytest.mark.parametrize("case", MISMATCHES)
def test_stream_mismatched_type(connected, functions, case):
    """Adding a stream at a type that does not describe the function's value raises the
    argument error, for each mismatch a run rejects."""
    function, typ = mismatched_function(connected, functions, case)
    assert_argument_error(
        connected, raised(lambda: connected.add_function_stream(function, typ))
    )


@profile("functions")
@pytest.mark.parametrize("typ", NOT_NULLABLE)
def test_null_for_non_nullable_type(connected, request, typ):
    """A function whose value is null, run at a type that cannot hold null, raises the
    encoding error. This holds for a ``double`` and for an object."""
    function, _ = null_function(connected, request, typ)
    error = raised(lambda: connected.run_function(function, typ))
    assert_error_kind(connected, error, pb.Error.ENCODING)


@profile("functions", "streams")
@pytest.mark.parametrize("typ", NOT_NULLABLE)
def test_stream_null_for_non_nullable_type(connected, request, typ):
    """Reading a stream of a function whose value is null, at a type that cannot hold null,
    raises the encoding error. This holds for a ``double`` and for an object."""
    function, _ = null_function(connected, request, typ)
    stream = connected.add_function_stream(function, typ)
    assert_error_kind(connected, raised(stream.get), pb.Error.ENCODING)


@profile("callbacks", "functions", "streams")
@pytest.mark.parametrize("typ", NOT_NULLABLE)
def test_null_in_callback(connected, request, typ):
    """A callback on a stream of a function, at a type that cannot hold null, is not called
    for a null value. It is called for the values that follow. This holds for a ``double``
    and for an object."""
    function, set_value = null_function(connected, request, typ)
    raised(lambda: connected.run_function(function, typ))
    stream = connected.add_function_stream(function, typ)
    # The stream has the null value when the callback is added
    raised(stream.get)
    callback = stream.add_callback()
    expected = set_value()
    calls = callback.wait_for_calls(1)
    assert [typed_value(connected, typ, x) for x in calls] == [expected]
