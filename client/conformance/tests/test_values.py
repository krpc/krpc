"""Value requirements. An argument comes back as the server's string for it, so a value that
the client encodes wrongly cannot pass by being decoded wrongly as well."""

import math
import sys

import pytest
import testee_pb2 as pb
from driver import profile, raw, section, should, values
from driver.errors import assert_error_kind, error_types
from driver.framing import encode_sint32
from driver.helpers import enum_value, ids, struct_value
from driver.scripted import ScriptedServer, definitions
from driver.testee import TesteeError

import krpc.schema.KRPC_pb2 as KRPC

# The name of each integer type in the procedures that convert it to and from a string, and
# the values to pass and read back. Each type has zero, the extremes, and values past the
# range of a smaller type or of the integers a double holds exactly.
INTEGERS = {
    "sint32": ("Int32", values.sint32, [0, 1, -1, 2**31 - 1, -(2**31)]),
    "sint64": (
        "Int64",
        values.sint64,
        [0, 1, -1, 2**31, -(2**31) - 1, 2**53 + 1, -(2**53) - 1, 2**63 - 1, -(2**63)],
    ),
    "uint32": ("UInt32", values.uint32, [0, 1, 2**31, 2**32 - 1]),
    "uint64": (
        "UInt64",
        values.uint64,
        [0, 1, 2**32, 2**53 + 1, 2**63 - 1, 2**63, 2**64 - 1],
    ),
}

# The values of each floating point type to pass and read back
FLOATING_POINT = {
    "double": (
        "Double",
        values.double,
        [
            3.14159,
            -2.5,
            42.0,
            0.0,
            -0.0,
            math.inf,
            -math.inf,
            math.nan,
            sys.float_info.max,
            -sys.float_info.max,
            sys.float_info.min,
            5e-324,
        ],
    ),
    "float": (
        "Float",
        values.float32,
        [
            values.single(3.14159),
            -2.5,
            42.0,
            0.0,
            -0.0,
            math.inf,
            -math.inf,
            math.nan,
            values.single(3.4028234663852886e38),
            -values.single(3.4028234663852886e38),
            values.single(1.1754943508222875e-38),
            values.single(1.401298464324817e-45),
        ],
    ),
}


def same_float(result, expected):
    """Whether two floating point values are the same, with NaN the same as NaN and zero
    told apart from negative zero."""
    if math.isnan(expected):
        return math.isnan(result)
    return result == expected and math.copysign(1, result) == math.copysign(1, expected)


def dotnet_string(value):
    """A floating point value as a string the server parses."""
    if math.isnan(value):
        return "NaN"
    if math.isinf(value):
        return "Infinity" if value > 0 else "-Infinity"
    return repr(value)


STRINGS = {
    "empty": "",
    "ascii": "kRPC",
    "non-ascii": "Conformance \u00fc \u65e5\u672c\u8a9e",
    "astral": "\U0001f680 \U0001d11e",
    "combining": "e\u0301",
    "byte-order-mark": "\ufeffkRPC",
    "nul": "a\x00b",
    "control": "".join(chr(x) for x in range(1, 32)) + "\x7f",
    "line-endings": "a\r\nb\rc\nd\n\re\u2028f\u2029g\x85h",
    "long": "x" * 300,
    "very-long": "\u65e5\u672c" * 50000,
}

BYTES = {
    "empty": b"",
    "nul": b"\x00",
    "all-values": bytes(range(256)),
    "invalid-utf8": b"\xff\xfe\xc3\x28",
    "very-long": bytes(range(256)) * 400,
}


ENUM_NAMES = ["ValueA", "ValueB", "ValueC"]


NULLABLE = {
    "double": ("EchoNullableDouble", values.double(2.5), 2.5),
    "float": ("EchoNullableFloat", values.float32(2.5), 2.5),
    "sint32": ("EchoNullableInt", values.sint32(42), 42),
    "sint64": ("EchoNullableLong", values.sint64(-(2**40)), -(2**40)),
    "uint32": ("EchoNullableUInt", values.uint32(42), 42),
    "uint64": ("EchoNullableULong", values.uint64(2**40), 2**40),
    "bool": ("EchoNullableBool", values.boolean(True), True),
    "enum": ("EchoNullableEnum", enum_value(2), values.Enum(2)),
    "string": ("EchoNullableString", values.string("alpha"), "alpha"),
    "bytes": ("EchoNullableBytes", values.data(b"\xca\xfe"), b"\xca\xfe"),
    "list": ("EchoNullableList", values.list_of([values.sint32(1)]), [1]),
    "set": ("EchoNullableSet", values.set_of([values.sint32(1)]), {1}),
    "dictionary": (
        "EchoNullableDictionary",
        values.dictionary([(values.string("a"), values.sint32(1))]),
        {"a": 1},
    ),
    "tuple": (
        "EchoNullableTuple",
        values.tuple_of(values.sint32(1), values.sint64(2)),
        (1, 2),
    ),
    "struct": (
        "StructEchoNullable",
        struct_value(42, "alpha", 2, [1, 2]),
        values.Struct((42, "alpha", values.Enum(2), [1, 2])),
    ),
}


# A procedure with a parameter of each type that cannot hold null
NOT_NULLABLE = {
    "double": "DoubleToString",
    "float": "FloatToString",
    "sint32": "Int32ToString",
    "sint64": "Int64ToString",
    "uint32": "UInt32ToString",
    "uint64": "UInt64ToString",
    "bool": "BoolToString",
    "enum": "EnumToString",
    "string": "set_StringProperty",
    "bytes": "BytesToHexString",
    "object": "NotNullableObject",
    "list": "IncrementList",
    "set": "IncrementSet",
    "dictionary": "IncrementDictionary",
    "tuple": "IncrementTuple",
    "struct": "StructEcho",
}


def not_nullable_slots():
    """A procedure, and an argument holding null at a position that cannot hold one, for
    each kind of position."""
    one, null = values.sint32(1), values.null()
    return {
        "list-element": ("IncrementList", values.list_of([one, null])),
        "string-element": (
            "EmptyListDefault",
            values.list_of([values.string("a"), null]),
        ),
        "object-element": (
            "AddToObjectList",
            values.list_of([null]),
            values.string("beta"),
        ),
        "set-element": ("IncrementSet", values.set_of([one, null])),
        "dictionary-key": (
            "IncrementDictionary",
            values.dictionary([(null, one)]),
        ),
        "dictionary-value": (
            "IncrementDictionary",
            values.dictionary([(values.string("a"), null)]),
        ),
        "tuple-item": ("IncrementTuple", values.tuple_of(one, null)),
        "struct-field": (
            "StructEcho",
            values.struct(
                "TestService",
                "TestStruct",
                null,
                values.string("alpha"),
                enum_value(1),
                values.list_of([]),
            ),
        ),
        "nested-element": (
            "IncrementNestedCollection",
            values.dictionary([(values.string("a"), values.list_of([one, null]))]),
        ),
    }


SLOTS = [
    "list-element",
    "string-element",
    "object-element",
    "set-element",
    "dictionary-key",
    "dictionary-value",
    "tuple-item",
    "struct-field",
    "nested-element",
]


def assert_value_error(client, error):
    """The error is the encoding or argument error."""
    assert_error_kind(client, error, pb.Error.ENCODING, pb.Error.ARGUMENT)


section("Scalars")


@pytest.mark.parametrize("typ", list(INTEGERS))
def test_integers(connected, typ):
    """The integer types are ``sint32``, ``sint64``, ``uint32`` and ``uint64``. An
    integer of each type arrives intact in both directions. This includes zero, the
    minimum and maximum, and values past the range of a smaller type or of the integers a
    ``double`` holds exactly. A result reads back as an integer."""
    name, value, cases = INTEGERS[typ]
    for x in cases:
        assert connected.invoke("TestService", name + "ToString", value(x)) == str(x)
        result = connected.invoke(
            "TestService", "StringTo" + name, values.string(str(x))
        )
        assert result == x
        assert type(result) is int, result


@pytest.mark.parametrize("typ", list(FLOATING_POINT))
def test_floating_point(connected, typ):
    """A ``double`` or ``float`` arrives intact in both directions. This includes
    negative zero, the infinities, ``NaN``, the largest value, the smallest normal value
    and the smallest subnormal value. A result reads back as a floating point number,
    also when its value is a whole number."""
    name, value, cases = FLOATING_POINT[typ]
    for x in cases:
        text = connected.invoke("TestService", name + "ToString", value(x))
        result = float(text)
        if typ == "float":
            result = values.single(result)
        assert same_float(result, x), (x, text)
        result = connected.invoke(
            "TestService", "StringTo" + name, values.string(dotnet_string(x))
        )
        assert same_float(result, x), (x, result)
        assert isinstance(result, float), result


def test_bool(connected):
    """A ``bool`` arrives intact in both directions."""
    for x in (True, False):
        assert connected.invoke("TestService", "BoolToString", values.boolean(x)) == (
            str(x)
        )
        result = connected.invoke("TestService", "StringToBool", values.string(str(x)))
        assert result is x


def test_enum(connected):
    """An enumeration value arrives intact in both directions, also in a list."""
    for value, name in enumerate(ENUM_NAMES):
        assert (
            connected.invoke("TestService", "EnumToString", enum_value(value)) == name
        )
        result = connected.invoke("TestService", "StringToEnum", values.string(name))
        assert result == values.Enum(value)
        result = connected.invoke("TestService", "EnumEcho", enum_value(value))
        assert result == values.Enum(value)
    argument = values.list_of([enum_value(0), enum_value(2)])
    result = connected.invoke("TestService", "EnumListDefault", argument)
    assert result == [values.Enum(0), values.Enum(2)]


def assert_malformed_result(client, procedure, *arguments):
    """Check that a call raises the encoding error, and that the next call succeeds."""
    with pytest.raises(TesteeError) as info:
        client.invoke("TestService", procedure, *arguments)
    # A client without error types reports only an RPC error
    if error_types(client):
        assert info.value.error.kind == pb.Error.ENCODING, info.value.error
    assert client.invoke("TestService", "Int32ToString", values.sint32(1)) == ""


def test_undeclared_enum_result(client, server, scripted_server):
    """An enumeration result with a value the enumeration does not declare raises the
    encoding error. The next call succeeds."""
    calls = []

    def result(request):
        # The value 99 as a sint32, then an empty string
        value = b"\xc6\x01" if not calls else b"\x00"
        calls.append(request)
        return KRPC.ProcedureResult(value=value)

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.answer_each(result)

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    try:
        assert_malformed_result(client, "StringToEnum", values.string("ValueA"))
    finally:
        client.close()


section("Strings and Bytes")


@pytest.mark.parametrize("case", list(STRINGS))
def test_string(connected, case):
    """A string arrives intact in both directions. This includes the empty string,
    characters outside the Basic Multilingual Plane, combining characters, a byte order
    mark, ``NUL`` and the other control characters, each line ending, and a string over
    65,536 bytes."""
    value = STRINGS[case]
    connected.invoke("TestService", "set_StringProperty", values.string(value))
    assert connected.invoke("TestService", "get_StringProperty") == value


@pytest.mark.parametrize("case", list(BYTES))
def test_bytes(connected, case):
    """Bytes arrive intact in both directions. This includes no bytes, each byte value,
    data that is not valid UTF-8, and data over 65,536 bytes."""
    value = BYTES[case]
    hex_string = value.hex()
    assert connected.invoke("TestService", "BytesToHexString", values.data(value)) == (
        hex_string
    )
    result = connected.invoke(
        "TestService", "HexStringToBytes", values.string(hex_string)
    )
    assert values.as_bytes(result) == value


section("Collections and Structures")


@pytest.mark.parametrize("kind", ["list", "set", "dictionary", "tuple", "nested"])
def test_collections(connected, kind):
    """Lists, sets, dictionaries and tuples arrive intact in both directions, including
    empty and nested ones."""
    if kind == "list":
        argument = values.list_of([values.sint32(x) for x in (1, -2, 3)])
        assert connected.invoke("TestService", "IncrementList", argument) == [2, -1, 4]
        empty = values.list_of([])
        assert connected.invoke("TestService", "IncrementList", empty) == []
        strings = values.list_of([values.string("foo"), values.string("bar")])
        result = connected.invoke("TestService", "EmptyListDefault", strings)
        assert result == ["foo", "bar"]
    elif kind == "set":
        argument = values.set_of([values.sint32(x) for x in (1, -2, 3)])
        assert connected.invoke("TestService", "IncrementSet", argument) == {2, -1, 4}
    elif kind == "dictionary":
        argument = values.dictionary(
            [(values.string(k), values.sint32(v)) for k, v in (("a", 1), ("b", -2))]
        )
        assert connected.invoke("TestService", "IncrementDictionary", argument) == {
            "a": 2,
            "b": -1,
        }
    elif kind == "tuple":
        argument = values.tuple_of(values.sint32(1), values.sint64(-123456789000))
        result = connected.invoke("TestService", "IncrementTuple", argument)
        assert tuple(result) == (2, -123456788999)
    else:
        argument = values.dictionary(
            [
                (
                    values.string("a"),
                    values.list_of([values.sint32(0), values.sint32(1)]),
                ),
                (values.string("b"), values.list_of([])),
            ]
        )
        result = connected.invoke("TestService", "IncrementNestedCollection", argument)
        assert result == {"a": [1, 2], "b": []}


@pytest.mark.parametrize("kind", ["list", "set", "dictionary", "tuple"])
def test_collections_of_objects(connected, kind):
    """Lists, sets, dictionaries and tuples of objects arrive intact in both
    directions."""
    alpha = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))

    def value_of(obj):
        return connected.invoke("TestService", "TestClass_GetValue", values.obj(obj))

    if kind == "list":
        argument = values.list_of([values.obj(alpha)])
        result = connected.invoke(
            "TestService", "AddToObjectList", argument, values.string("beta")
        )
        assert [value_of(x) for x in result] == ["value=alpha", "value=beta"]
        assert result[0].id == alpha.id
    elif kind == "set":
        argument = values.set_of([values.obj(alpha)])
        result = connected.invoke(
            "TestService", "AddToObjectSet", argument, values.string("beta")
        )
        assert sorted(value_of(x) for x in result) == ["value=alpha", "value=beta"]
    elif kind == "dictionary":
        argument = values.dictionary([(values.string("alpha"), values.obj(alpha))])
        result = connected.invoke(
            "TestService", "AddToObjectDictionary", argument, values.string("beta")
        )
        assert {k: value_of(v) for k, v in result.items()} == {
            "alpha": "value=alpha",
            "beta": "value=beta",
        }
    else:
        beta = connected.invoke(
            "TestService", "CreateTestObject", values.string("beta")
        )
        argument = values.tuple_of(values.obj(alpha), values.obj(beta))
        result = connected.invoke("TestService", "SwapObjectTuple", argument)
        assert [x.id for x in result] == [beta.id, alpha.id]


@pytest.mark.parametrize("kind", ["struct", "nested", "list"])
def test_structures(connected, kind):
    """Structures, nested structures and collections of structures arrive intact in both
    directions."""
    expected = values.Struct((42, "alpha", values.Enum(2), [1, 2]))
    if kind == "struct":
        argument = struct_value(42, "alpha", 2, [1, 2])
        assert connected.invoke("TestService", "StructEcho", argument) == expected
    elif kind == "nested":
        obj = connected.invoke("TestService", "CreateTestObject", values.string("beta"))
        argument = values.struct(
            "TestService",
            "TestNestedStruct",
            struct_value(42, "alpha", 2, [1, 2]),
            values.obj(obj),
            values.string("val"),
        )
        result = connected.invoke("TestService", "NestedStructEcho", argument)
        assert isinstance(result, values.Struct)
        assert result.fields[0] == expected
        assert ids(result.fields[1]) == obj.id
        assert result.fields[2] == "val"
    else:
        argument = values.list_of(
            [struct_value(1, "a", 0, []), struct_value(-1, "b", 1, [3])]
        )
        assert connected.invoke("TestService", "IncrementListOfStructs", argument) == [
            values.Struct((2, "a", values.Enum(0), [])),
            values.Struct((0, "b", values.Enum(1), [3])),
        ]


section("Equality and Ordering")


# A structure, and one that differs from it in each field
STRUCT_FIELDS = (42, "alpha", 2, [1, 2])
OTHER_STRUCT_FIELDS = {
    "int": (43, "alpha", 2, [1, 2]),
    "string": (42, "beta", 2, [1, 2]),
    "enum": (42, "alpha", 1, [1, 2]),
    "list": (42, "alpha", 2, [1, 3]),
}


def test_enum_equality(connected):
    """Two enumeration values with the same value are equal. Values that differ are
    not."""
    assert connected.compare(enum_value(2), enum_value(2))
    assert not connected.compare(enum_value(2), enum_value(1))


@pytest.mark.parametrize("field", list(OTHER_STRUCT_FIELDS))
def test_structure_equality(connected, field):
    """Two structures with equal fields are equal. Structures that differ in a field are
    not."""
    a = struct_value(*STRUCT_FIELDS)
    assert connected.compare(a, struct_value(*STRUCT_FIELDS))
    assert not connected.compare(a, struct_value(*OTHER_STRUCT_FIELDS[field]))


def test_structure_hash(connected):
    """Equal structures hash equal, in a language that hashes values."""
    a = connected.hash(struct_value(*STRUCT_FIELDS))
    assert a == connected.hash(struct_value(*STRUCT_FIELDS))


@profile("ordering")
def test_structure_sorting(connected):
    """Structures order by their fields in turn, as tuples of the same values do. Each of
    the language's comparison operators agrees with this order."""
    first = struct_value(1, "b", 2, [1, 2])
    second = struct_value(2, "a", 2, [1, 2])
    third = struct_value(2, "b", 2, [1, 2])
    assert connected.order(first, second) == -1
    assert connected.order(second, first) == 1
    assert connected.order(second, third) == -1
    assert connected.order(third, second) == 1


section("Messages")


def test_message_result(connected, server):
    """A procedure that returns a kRPC protocol message, such as ``KRPC.GetStatus``,
    returns the decoded message."""
    status = KRPC.Status.FromString(connected.invoke("KRPC", "GetStatus"))
    expected = KRPC.Status.FromString(raw.call(server, "KRPC", "GetStatus").value)
    assert status.version == expected.version
    assert status.protocol_version == expected.protocol_version
    assert status.bytes_read > 0


section("Null")


@pytest.mark.parametrize("typ", list(NULLABLE))
def test_nullable(connected, typ):
    """A parameter or result that can hold null passes and returns both null and a
    value, for each type."""
    procedure, argument, expected = NULLABLE[typ]
    assert connected.invoke("TestService", procedure, values.null()) is None
    result = connected.invoke("TestService", procedure, argument)
    if isinstance(expected, tuple):
        result = tuple(result)
    elif isinstance(expected, bytes):
        result = values.as_bytes(result)
    assert result == expected


@pytest.mark.parametrize("kind", ["procedure", "method", "static-method"])
def test_nullable_object(connected, kind):
    """An object parameter or result that can hold null passes and returns both null and
    an object, for a procedure, a method and a static method."""
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))

    def echo(value):
        if kind == "procedure":
            connected.invoke("TestService", "set_ObjectProperty", value)
            return connected.invoke("TestService", "get_ObjectProperty")
        if kind == "method":
            return connected.invoke(
                "TestService", "TestClass_EchoNullableObject", values.obj(obj), value
            )
        return connected.invoke(
            "TestService", "TestClass_static_StaticNullableObject", value
        )

    assert echo(values.null()) is None
    assert echo(values.obj(obj)).id == obj.id
    if kind == "procedure":
        assert connected.invoke("TestService", "EchoTestObject", values.null()) is None
        result = connected.invoke("TestService", "EchoTestObject", values.obj(obj))
        assert result.id == obj.id
    elif kind == "method":
        result = connected.invoke(
            "TestService", "TestClass_ObjectToString", values.obj(obj), values.null()
        )
        assert result == "alphanull"


def test_nullable_fields(connected):
    """A structure field that can hold null passes and returns both null and a
    value."""
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))

    def echo(*fields):
        argument = values.struct(
            "TestService", "TestNullableStruct", values.sint32(42), *fields
        )
        result = connected.invoke("TestService", "NullableStructEcho", argument)
        assert isinstance(result, values.Struct)
        return ids(result.fields)

    assert echo(*[values.null()] * 4) == (42, None, None, None, None)
    fields = [values.sint32(7), enum_value(1), values.string("beta"), values.obj(obj)]
    assert echo(*fields) == (42, 7, values.Enum(1), "beta", obj.id)


@pytest.mark.parametrize("kind", ["list", "dictionary", "tuple", "nested", "struct"])
def test_nullable_elements(connected, kind):
    """Null list elements, dictionary values and tuple items arrive intact, also inside
    another collection or a structure."""
    obj = connected.invoke("TestService", "CreateTestObject", values.string("alpha"))
    if kind == "list":
        ints = values.list_of([values.sint32(1), values.null(), values.sint32(3)])
        assert connected.invoke("TestService", "EchoListOfNullableInts", ints) == [
            1,
            None,
            3,
        ]
        objects = values.list_of([values.obj(obj), values.null()])
        result = connected.invoke("TestService", "EchoListOfNullableObjects", objects)
        assert ids(result) == [obj.id, None]
    elif kind == "dictionary":
        argument = values.dictionary(
            [(values.string("a"), values.obj(obj)), (values.string("b"), values.null())]
        )
        result = connected.invoke(
            "TestService", "EchoDictionaryOfNullableObjects", argument
        )
        assert ids(result) == {"a": obj.id, "b": None}
    elif kind == "tuple":
        argument = values.tuple_of(values.sint32(1), values.null())
        result = connected.invoke(
            "TestService", "EchoTupleWithANullableObject", argument
        )
        assert tuple(result) == (1, None)
        argument = values.tuple_of(values.sint32(1), values.obj(obj))
        result = connected.invoke(
            "TestService", "EchoTupleWithANullableObject", argument
        )
        assert ids(tuple(result)) == (1, obj.id)
    elif kind == "nested":
        argument = values.list_of(
            [values.list_of([values.null(), values.obj(obj)]), values.list_of([])]
        )
        result = connected.invoke(
            "TestService", "EchoNestedListOfNullableObjects", argument
        )
        assert ids(result) == [[None, obj.id], []]
    else:
        argument = values.struct(
            "TestService",
            "TestNestedNullableStruct",
            values.list_of([values.null(), values.obj(obj)]),
            values.string("beta"),
        )
        result = connected.invoke("TestService", "EchoNestedNullableStruct", argument)
        assert ids(result.fields) == ([None, obj.id], "beta")


def answering(client, server, scripted_server, results):
    """Connect the client to a script that answers each call with the next of the results,
    then with an empty string. Returns the requests the script answers."""
    requests = []

    def result(request):
        answered = len(requests)
        requests.append(request)
        if answered < len(results):
            return results[answered]
        return KRPC.ProcedureResult(value=b"\x00")

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.answer_each(result)

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    return requests


@pytest.mark.parametrize("kind", ["sint32", "object"])
def test_null_flag(client, server, scripted_server, kind):
    """A null argument or result sets the ``is_null`` flag and has no value. A null object
    is never the id 0. A value at a position that can hold null, outside a collection or
    structure, has no presence bool."""
    if kind == "sint32":
        procedure, argument, encoded = "EchoNullableInt", values.sint32(42), b"\x54"
    else:
        procedure = "EchoTestObject"
        argument = values.make_object("TestService", "TestClass", 42)
        encoded = b"\x2a"
    requests = answering(
        client,
        server,
        scripted_server,
        [KRPC.ProcedureResult(is_null=True), KRPC.ProcedureResult(value=encoded)],
    )
    try:
        assert client.invoke("TestService", procedure, values.null()) is None
        result = client.invoke("TestService", procedure, argument)
        assert (result.id if kind == "object" else result) == 42
        null, value = (request.calls[0].arguments for request in requests)
        assert [(x.position, x.is_null, x.value) for x in null] == [(0, True, b"")]
        assert [(x.position, x.is_null, x.value) for x in value] == [
            (0, False, encoded)
        ]
    finally:
        client.close()


def refused_before_sending(client, server, scripted_server, procedure, *arguments):
    """Invoke a procedure on a script that records each request, and check the invocation
    raises the encoding or argument error and sends nothing. The next call is the only
    request the script receives."""
    requests = []

    def result(request):
        requests.append(request)
        # An empty string, for the next call
        return KRPC.ProcedureResult(value=b"\x00")

    def script(scripted: ScriptedServer) -> None:
        scripted.accept()
        scripted.answer_each(result)

    scripted = scripted_server(script)
    scripted.services = definitions(server, "KRPC", "TestService")
    client.connect(scripted.endpoint())
    try:
        with pytest.raises(TesteeError) as info:
            client.invoke("TestService", procedure, *arguments)
        assert_value_error(client, info.value.error)
        assert client.invoke("TestService", "Int32ToString", values.sint32(1)) == ""
        assert len(requests) == 1, requests
    finally:
        client.close()


@pytest.mark.parametrize("typ", list(NOT_NULLABLE))
def test_null_for_non_nullable(client, server, scripted_server, typ):
    """A null argument for a parameter that cannot hold null raises the encoding or
    argument error, before the request is sent. This holds for each type the language
    can pass null for."""
    refused_before_sending(
        client, server, scripted_server, NOT_NULLABLE[typ], values.null()
    )


@pytest.mark.parametrize("slot", SLOTS)
def test_null_in_non_nullable_slot(client, server, scripted_server, slot):
    """A null in a collection or structure, at a position that cannot hold null, raises
    the encoding or argument error, before the request is sent. Set elements and
    dictionary keys never hold null."""
    procedure, *arguments = not_nullable_slots()[slot]
    refused_before_sending(client, server, scripted_server, procedure, *arguments)


def test_null_result_for_non_nullable(client, server, scripted_server):
    """A null result for a procedure whose return value cannot be null raises the
    encoding error. The next call succeeds."""
    answering(client, server, scripted_server, [KRPC.ProcedureResult(is_null=True)])
    try:
        assert_malformed_result(client, "Int32ToString", values.sint32(1))
    finally:
        client.close()


def test_null_result_with_a_value(client, server, scripted_server):
    """A result with the ``is_null`` flag set and a value raises the encoding error. The
    next call succeeds."""
    result = KRPC.ProcedureResult(is_null=True, value=encode_sint32(42))
    answering(client, server, scripted_server, [result])
    try:
        assert_malformed_result(client, "EchoNullableInt", values.sint32(42))
    finally:
        client.close()


section(
    "Argument Types",
    "These requirements apply to a dynamically typed language. In a statically typed "
    "language, the compiler checks the same.",
)


# Integers of each type, including the extremes of the 64-bit types
INTEGERS_TO_FLOAT = {
    "sint32": [values.sint32(42), values.sint32(-42)],
    "sint64": [
        values.sint64(2**53 + 1),
        values.sint64(2**24 + 1),
        values.sint64(-(2**63)),
        values.sint64(2**63 - 1),
    ],
    "uint64": [values.uint64(2**64 - 1)],
}


@profile("dynamic")
@pytest.mark.parametrize("integer", list(INTEGERS_TO_FLOAT))
@pytest.mark.parametrize("typ", ["double", "float"])
def test_integer_to_float(connected, typ, integer):
    """An integer passed for a floating point parameter is sent as the nearest value of
    the parameter's type. This includes the smallest and largest ``sint64`` and the
    largest ``uint64``."""
    name = "DoubleToString" if typ == "double" else "FloatToString"
    nearest = float if typ == "double" else (lambda x: values.single(float(x)))
    for value in INTEGERS_TO_FLOAT[integer]:
        x = int(getattr(value, value.WhichOneof("value")))
        text = connected.invoke("TestService", name, value)
        assert nearest(float(text)) == nearest(x), (x, text)
    result = connected.invoke(
        "TestService",
        "AddMultipleValues",
        values.sint32(1),
        values.sint32(2),
        values.sint32(3),
    )
    assert result == "6"


@profile("dynamic")
@pytest.mark.parametrize(
    "position", ["first", "later", "numeric-string", "bool", "list-item", "tuple-item"]
)
def test_wrong_type(connected, position):
    """A value of the wrong type for its parameter, or for an item of a collection,
    raises the encoding or argument error. A string that holds a number is not
    converted to the number, and a bool is not converted to a number."""
    with pytest.raises(TesteeError) as info:
        if position == "first":
            connected.invoke("TestService", "FloatToString", values.string("foo"))
        elif position == "numeric-string":
            connected.invoke("TestService", "Int32ToString", values.string("42"))
        elif position == "bool":
            connected.invoke("TestService", "FloatToString", values.boolean(True))
        elif position == "list-item":
            argument = values.list_of([values.sint32(1), values.string("foo")])
            connected.invoke("TestService", "IncrementList", argument)
        elif position == "tuple-item":
            argument = values.tuple_of(values.sint32(1), values.string("foo"))
            connected.invoke("TestService", "IncrementTuple", argument)
        else:
            connected.invoke(
                "TestService",
                "AddMultipleValues",
                values.float32(0.5),
                values.string("foo"),
                values.sint64(2),
            )
    assert_value_error(connected, info.value.error)


# A procedure, and a number outside the range of its parameter's type
OUT_OF_RANGE = {
    "sint32-above": ("Int32ToString", values.uint64(2**31)),
    "sint32-below": ("Int32ToString", values.sint64(-(2**31) - 1)),
    "sint64-above": ("Int64ToString", values.uint64(2**63)),
    "uint32-above": ("UInt32ToString", values.uint64(2**32 + 1)),
    "uint32-negative": ("UInt32ToString", values.sint32(-1)),
    "uint64-negative": ("UInt64ToString", values.sint32(-1)),
    "float-above": ("FloatToString", values.double(1e300)),
    "float-below": ("FloatToString", values.double(-1e300)),
}


@profile("dynamic")
@pytest.mark.parametrize("case", list(OUT_OF_RANGE))
def test_out_of_range(connected, case):
    """A number outside the range of its parameter's type raises the encoding or
    argument error. This includes a finite ``double`` beyond the largest ``float``,
    passed for a ``float`` parameter."""
    procedure, value = OUT_OF_RANGE[case]
    with pytest.raises(TesteeError) as info:
        connected.invoke("TestService", procedure, value)
    assert_value_error(connected, info.value.error)


@profile("dynamic")
def test_undeclared_enum_value(connected):
    """An enumeration value that the enumeration does not declare raises the encoding or
    argument error."""
    with pytest.raises(TesteeError) as info:
        connected.invoke("TestService", "EnumToString", enum_value(99))
    assert_value_error(connected, info.value.error)


# A procedure, and a string, bytes or dictionary for its list, set or tuple parameter
WRONG_COLLECTIONS = {
    "string-for-list": ("EmptyListDefault", values.string("abc")),
    "string-for-set": ("EchoStringSet", values.string("abc")),
    "string-for-tuple": ("IncrementTuple", values.string("12")),
    "bytes-for-list": ("IncrementList", values.data(b"\x01\x02")),
    "dictionary-for-list": (
        "EmptyListDefault",
        values.dictionary([(values.string("a"), values.string("b"))]),
    ),
}


@should
@profile("dynamic")
@pytest.mark.parametrize("case", list(WRONG_COLLECTIONS))
def test_wrong_collection(connected, case):
    """A string, bytes or a dictionary passed for a list, set or tuple raises the
    encoding or argument error."""
    procedure, value = WRONG_COLLECTIONS[case]
    with pytest.raises(TesteeError) as info:
        connected.invoke("TestService", procedure, value)
    assert_value_error(connected, info.value.error)
