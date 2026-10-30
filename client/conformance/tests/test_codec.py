"""Codec requirements: values encoded with the client's encoder and decoded with its decoder,
against fixed vectors. The values pass through no server.

The codec types are the type names the Encode and Decode commands take. A testee builds the
client's type for each.
"""

import math
import struct
import sys

import pytest
import testee_pb2 as pb
from driver import profile, section, should, untested, values
from driver.errors import assert_error_kind
from driver.framing import encode_size
from driver.helpers import enum_value, ids, struct_value
from driver.testee import TesteeError


def object_value(object_id):
    return values.make_object("TestService", "TestClass", object_id)


def check_vectors(connected, typ, vectors):
    """Encode each value and compare with its encoding, then decode the encoding and compare
    with the value it reads back as."""
    for value, decoded, encoded in vectors:
        assert connected.encode(typ, value).hex() == encoded, (typ, decoded)
        result = connected.decode(typ, bytes.fromhex(encoded))
        if isinstance(decoded, tuple):
            result = tuple(result)
        if typ == "bytes":
            result = values.as_bytes(result)
        elif typ == "list(nullable bytes)":
            result = [None if x is None else values.as_bytes(x) for x in result]
        assert ids(result) == decoded, typ


section("Scalars")


# The encoding of each integer value. Each type has zero, the extremes, the edges of the one
# byte encodings, and values past the range of a smaller type or of the integers a double holds
# exactly.
INTEGERS = {
    "sint32": (
        values.sint32,
        [
            (0, "00"),
            (1, "02"),
            (-1, "01"),
            (63, "7e"),
            (-64, "7f"),
            (64, "8001"),
            (300, "d804"),
            (2**31 - 1, "feffffff0f"),
            (-(2**31), "ffffffff0f"),
        ],
    ),
    "sint64": (
        values.sint64,
        [
            (0, "00"),
            (-1, "01"),
            (2**31, "8080808010"),
            (-(2**31) - 1, "8180808010"),
            (2**53 + 1, "8280808080808020"),
            (-(2**53) - 1, "8180808080808020"),
            (2**63 - 1, "feffffffffffffffff01"),
            (-(2**63), "ffffffffffffffffff01"),
        ],
    ),
    "uint32": (
        values.uint32,
        [
            (0, "00"),
            (1, "01"),
            (127, "7f"),
            (128, "8001"),
            (2**31, "8080808008"),
            (2**32 - 1, "ffffffff0f"),
        ],
    ),
    "uint64": (
        values.uint64,
        [
            (0, "00"),
            (2**32, "8080808010"),
            (2**53 + 1, "8180808080808010"),
            (2**63 - 1, "ffffffffffffffff7f"),
            (2**63, "80808080808080808001"),
            (2**64 - 1, "ffffffffffffffffff01"),
        ],
    ),
}

# The encoding of each floating point value
FLOATING_POINT = {
    "double": (
        values.double,
        [
            (0.0, "0000000000000000"),
            (-0.0, "0000000000000080"),
            (-1.0, "000000000000f0bf"),
            (3.14159265359, "ea2e4454fb210940"),
            (math.inf, "000000000000f07f"),
            (-math.inf, "000000000000f0ff"),
            (sys.float_info.max, "ffffffffffffef7f"),
            (-sys.float_info.max, "ffffffffffffefff"),
            (sys.float_info.min, "0000000000001000"),
            (5e-324, "0100000000000000"),
        ],
    ),
    "float": (
        values.float32,
        [
            (0.0, "00000000"),
            (-0.0, "00000080"),
            (-1.0, "000080bf"),
            (values.single(3.14159265359), "db0f4940"),
            (math.inf, "0000807f"),
            (-math.inf, "000080ff"),
            (values.single(3.4028234663852886e38), "ffff7f7f"),
            (-values.single(3.4028234663852886e38), "ffff7fff"),
            (values.single(1.1754943508222875e-38), "00008000"),
            (values.single(1.401298464324817e-45), "01000000"),
        ],
    ),
}

# A quiet NaN of each sign, as encoded by a server, and a signaling NaN
NANS = {
    "double": ["000000000000f87f", "000000000000f8ff", "000000000000f47f"],
    "float": ["0000c07f", "0000c0ff", "0000a07f"],
}


@pytest.mark.parametrize("typ", list(INTEGERS))
def test_integers(connected, typ):
    """An integer of each type encodes to, and decodes from, its zigzag or plain varint. The
    vectors include zero, the minimum and maximum, the edges of the one byte encodings, and
    values past the range of a smaller type or of the integers a ``double`` holds exactly. A
    decoded value is an integer."""
    value, cases = INTEGERS[typ]
    for x, encoded in cases:
        assert connected.encode(typ, value(x)).hex() == encoded, x
        result = connected.decode(typ, bytes.fromhex(encoded))
        assert result == x
        assert type(result) is int, result


@pytest.mark.parametrize("typ", list(FLOATING_POINT))
def test_floating_point(connected, typ):
    """A ``double`` or ``float`` encodes to, and decodes from, its IEEE 754 bytes. The vectors
    include negative zero, the infinities, the largest value, the smallest normal value and
    the smallest subnormal value. A ``NaN`` of either sign, quiet or signaling, decodes as a
    ``NaN``, and a ``NaN`` encodes to a ``NaN``. A decoded value is a floating point
    number."""
    value, cases = FLOATING_POINT[typ]
    form = "<d" if typ == "double" else "<f"
    for x, encoded in cases:
        assert connected.encode(typ, value(x)).hex() == encoded, x
        result = connected.decode(typ, bytes.fromhex(encoded))
        assert result == x
        assert math.copysign(1, result) == math.copysign(1, x), result
        assert isinstance(result, float), result
    for data in NANS[typ]:
        assert math.isnan(connected.decode(typ, bytes.fromhex(data)))
    assert math.isnan(struct.unpack(form, connected.encode(typ, value(math.nan)))[0])


def test_bool(connected):
    """A ``bool`` encodes as the varint 1 or 0."""
    check_vectors(
        connected,
        "bool",
        [(values.boolean(True), True, "01"), (values.boolean(False), False, "00")],
    )


def test_enum(connected):
    """An enumeration value encodes as its value, as a ``sint32``."""
    check_vectors(
        connected,
        "TestEnum",
        [
            (enum_value(0), values.Enum(0), "00"),
            (enum_value(2), values.Enum(2), "04"),
        ],
    )


# Values outside the range of each integer type, each in a Value that carries it
OUT_OF_RANGE = {
    "sint32": [values.uint64(2**31), values.sint64(-(2**31) - 1)],
    "sint64": [values.uint64(2**63)],
    "uint32": [values.sint32(-1), values.uint64(2**32)],
    "uint64": [values.sint32(-1)],
}


@profile("dynamic")
@pytest.mark.parametrize("typ", list(OUT_OF_RANGE))
def test_out_of_range(connected, typ):
    """In a dynamically typed language, encoding an integer outside the range of its type
    raises the encoding or argument error. This includes a negative value for an unsigned
    type."""
    for value in OUT_OF_RANGE[typ]:
        with pytest.raises(TesteeError) as info:
            connected.encode(typ, value)
        assert_error_kind(
            connected, info.value.error, pb.Error.ENCODING, pb.Error.ARGUMENT
        )


section("Strings and Bytes")


STRINGS = {
    "empty": "",
    "ascii": "kRPC",
    "non-ascii": "ü ™ 日本",
    "astral": "\U0001f680",
    "nul": "a\x00b",
    "long": "x" * 300,
    "very-long": "日本" * 50000,
}

BYTES = {
    "empty": b"",
    "nul": b"\x00",
    "all-values": bytes(range(256)),
    "invalid-utf8": b"\xff\xfe\xc3\x28",
    "very-long": bytes(range(256)) * 400,
}


@pytest.mark.parametrize("case", list(STRINGS))
def test_string(connected, case):
    """A string encodes as the length of its UTF-8 bytes, as a varint, then the bytes. The
    vectors include characters of each UTF-8 length, a NUL character, and lengths that take
    one, two and three bytes."""
    text = STRINGS[case]
    data = text.encode()
    encoded = (encode_size(len(data)) + data).hex()
    check_vectors(connected, "string", [(values.string(text), text, encoded)])


@pytest.mark.parametrize("case", list(BYTES))
def test_bytes(connected, case):
    """Bytes encode as their length, as a varint, then the bytes. The vectors include every
    byte value, bytes that are not valid UTF-8, and lengths that take one, two and three
    bytes."""
    data = BYTES[case]
    encoded = (encode_size(len(data)) + data).hex()
    check_vectors(connected, "bytes", [(values.data(data), data, encoded)])


section("Objects")


def test_object(connected):
    """An object encodes as its id, as a ``uint64``. Every id is an object, including 0, as a
    null is carried by the ``is_null`` flag or a presence bool. The vectors include an id past
    the range of a ``uint32``."""
    for object_id, encoded in [(0, "00"), (300, "ac02"), (2**32, "8080808010")]:
        obj = object_value(object_id)
        assert connected.encode("TestClass", obj).hex() == encoded
        result = connected.decode("TestClass", bytes.fromhex(encoded))
        assert isinstance(result, values.Object)
        assert result.id == object_id


section("Collections and Structures")


def item(data):
    """A length-delimited field 1 entry."""
    return b"\x0a" + encode_size(len(data)) + data


def entries(data):
    """The length-delimited field 1 entries of an encoded collection, as a sorted list."""
    result = []
    position = 0
    while position < len(data):
        assert data[position] == 0x0A
        size, shift, position = 0, 0, position + 1
        while True:
            size |= (data[position] & 0x7F) << shift
            shift += 7
            position += 1
            if data[position - 1] & 0x80 == 0:
                break
        result.append(data[position : position + size])
        position += size
    assert position == len(data)
    return sorted(result)


# An item longer than 127 bytes, whose length takes two bytes
LONG = "k" * 200

COLLECTIONS = {
    "list(sint32)": [
        (values.list_of([]), [], ""),
        (
            values.list_of([values.sint32(x) for x in (1, 2, 3)]),
            [1, 2, 3],
            "0a01020a01040a0106",
        ),
    ],
    "list(string)": [
        (
            values.list_of([values.string(""), values.string("a")]),
            ["", "a"],
            "0a01000a020161",
        ),
        (
            values.list_of([values.string(LONG)]),
            [LONG],
            item(encode_size(len(LONG)) + LONG.encode()).hex(),
        ),
    ],
    "set(sint32)": [
        (values.set_of([]), frozenset(), ""),
        (values.set_of([values.sint32(1)]), frozenset([1]), "0a0102"),
    ],
    "dictionary(string,sint32)": [
        (values.dictionary([]), {}, ""),
        (
            values.dictionary([(values.string(""), values.sint32(0))]),
            {"": 0},
            "0a060a0100120100",
        ),
    ],
    "tuple(sint32,bool)": [
        (
            values.tuple_of(values.sint32(1), values.boolean(False)),
            (1, False),
            "0a01020a0100",
        ),
        (
            values.tuple_of(values.sint32(-1), values.boolean(True)),
            (-1, True),
            "0a01010a0101",
        ),
    ],
    "tuple(uint32)": [
        (values.tuple_of(values.uint32(1)), (1,), "0a0101"),
    ],
    "tuple(uint32,string,bool)": [
        (
            values.tuple_of(
                values.uint32(1), values.string("foo"), values.boolean(False)
            ),
            (1, "foo", False),
            "0a01010a0403666f6f0a0100",
        ),
    ],
    "tuple(sint32,sint64)": [
        (
            values.tuple_of(values.sint32(-1), values.sint64(2)),
            (-1, 2),
            "0a01010a0104",
        ),
    ],
}

# Sets and dictionaries of several elements, and of elements longer than a byte or longer
# than 127 bytes. The order of the elements is the encoder's, so the encoded elements are
# compared in any order.
UNORDERED = {
    "set(sint32)": [
        (
            values.set_of([values.sint32(x) for x in (1, 2, 3)]),
            frozenset([1, 2, 3]),
            "0a01020a01040a0106",
        ),
        (values.set_of([values.sint32(300)]), frozenset([300]), "0a02d804"),
    ],
    "dictionary(string,sint32)": [
        (
            values.dictionary(
                [
                    (values.string(k), values.sint32(v))
                    for k, v in (("foo", 42), ("bar", 365), ("baz", 3))
                ]
            ),
            {"foo": 42, "bar": 365, "baz": 3},
            "0a0a0a04036261721202da050a090a040362617a1201060a090a0403666f6f120154",
        ),
        (
            values.dictionary([(values.string(LONG), values.sint32(0))]),
            {LONG: 0},
            item(item(encode_size(len(LONG)) + LONG.encode()) + b"\x12\x01\x00").hex(),
        ),
    ],
}


@pytest.mark.parametrize("typ", list(COLLECTIONS))
def test_collections(connected, typ):
    """Lists, sets, dictionaries and tuples encode to, and decode from, their given
    encodings. The vectors include empty collections, empty items, and items longer than
    127 bytes. The entries of a dictionary and the items of a set may be encoded in any
    order."""
    check_vectors(connected, typ, COLLECTIONS[typ])
    for value, decoded, encoded in UNORDERED.get(typ, []):
        data = bytes.fromhex(encoded)
        assert entries(connected.encode(typ, value)) == entries(data), typ
        assert connected.decode(typ, data) == decoded, typ


def test_dictionary_value_left_out(connected):
    """A dictionary entry with no value field decodes with the value that encodes to no
    bytes, such as an empty list. A protobuf encoder leaves out a field holding no
    bytes."""
    data = bytes.fromhex("0a040a020161")
    assert connected.decode("dictionary(string,list(uint32))", data) == {"a": []}


def test_structures(connected):
    """A structure encodes as a tuple of its fields, in the order the structure declares
    them."""
    check_vectors(
        connected,
        "TestStruct",
        [
            (
                struct_value(1, "foo", 2, [1]),
                values.Struct((1, "foo", values.Enum(2), [1])),
                "0a01020a0403666f6f0a01040a030a0102",
            ),
            (
                struct_value(0, "", 0, []),
                values.Struct((0, "", values.Enum(0), [])),
                "0a01000a01000a01000a00",
            ),
        ],
    )


# Encodings with an item more than the type has, the values they decode to, and a value with
# an item more
EXTRA_ITEMS = {
    "tuple": (
        "tuple(sint32,bool)",
        "0a01020a01000a0100",
        (1, False),
        values.tuple_of(values.sint32(1), values.boolean(False), values.boolean(False)),
    ),
    "struct": (
        "TestStruct",
        "0a01020a0403666f6f0a01040a030a01020a0100",
        values.Struct((1, "foo", values.Enum(2), [1])),
        values.struct(
            "TestService",
            "TestStruct",
            values.sint32(1),
            values.string("foo"),
            enum_value(2),
            values.list_of([values.sint32(1)]),
            values.boolean(False),
        ),
    ),
}


@pytest.mark.parametrize("kind", list(EXTRA_ITEMS))
def test_extra_items(connected, kind):
    """An encoded tuple or structure with more items than its type decodes, and the extra
    items are ignored. In a
    dynamically typed language, encoding a tuple or structure value with more items than
    its type raises the encoding or argument error."""
    typ, data, decoded, value = EXTRA_ITEMS[kind]
    result = connected.decode(typ, bytes.fromhex(data))
    if isinstance(decoded, tuple):
        result = tuple(result)
    assert result == decoded
    if connected.has_profile("dynamic"):
        with pytest.raises(TesteeError) as info:
            connected.encode(typ, value)
        assert_error_kind(
            connected, info.value.error, pb.Error.ENCODING, pb.Error.ARGUMENT
        )


# Encodings with too few items, and a value with too few
TOO_FEW_ITEMS = {
    "tuple": (
        "tuple(sint32,bool)",
        ["", "0a0102"],
        values.tuple_of(values.sint32(1)),
    ),
    "struct": (
        "TestStruct",
        ["", "0a01020a0403666f6f0a0104"],
        values.struct(
            "TestService", "TestStruct", values.sint32(1), values.string("foo")
        ),
    ),
}


@pytest.mark.parametrize("kind", list(TOO_FEW_ITEMS))
def test_too_few_items(connected, kind):
    """Decoding a tuple or structure with fewer items than its type raises the encoding
    error. In a dynamically typed language, encoding a tuple or structure value with too
    few items raises the encoding or argument error."""
    typ, encodings, value = TOO_FEW_ITEMS[kind]
    for data in encodings:
        with pytest.raises(TesteeError) as info:
            connected.decode(typ, bytes.fromhex(data))
        assert_error_kind(connected, info.value.error, pb.Error.ENCODING)
    if connected.has_profile("dynamic"):
        with pytest.raises(TesteeError) as info:
            connected.encode(typ, value)
        assert_error_kind(
            connected, info.value.error, pb.Error.ENCODING, pb.Error.ARGUMENT
        )


section("Null")


NULLABLE = {
    "list(nullable sint32)": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (values.list_of([values.sint32(0)]), [0], "0a020100"),
        (
            values.list_of([values.sint32(1), values.null(), values.sint32(3)]),
            [1, None, 3],
            "0a0201020a01000a020106",
        ),
    ],
    "list(nullable bool)": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (values.list_of([values.boolean(False)]), [False], "0a020100"),
        (values.list_of([values.boolean(True)]), [True], "0a020101"),
    ],
    "list(nullable bytes)": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (values.list_of([values.data(b"")]), [b""], "0a020100"),
        (values.list_of([values.data(b"\xca\xfe")]), [b"\xca\xfe"], "0a040102cafe"),
    ],
    "tuple(sint32,nullable string)": [
        (values.tuple_of(values.sint32(1), values.null()), (1, None), "0a01020a0100"),
        (
            values.tuple_of(values.sint32(1), values.string("")),
            (1, ""),
            "0a01020a020100",
        ),
        (
            values.tuple_of(values.sint32(1), values.string("foo")),
            (1, "foo"),
            "0a01020a050103666f6f",
        ),
    ],
    "dictionary(string,nullable sint32)": [
        (
            values.dictionary([(values.string("a"), values.null())]),
            {"a": None},
            "0a070a020161120100",
        ),
        (
            values.dictionary([(values.string("a"), values.sint32(0))]),
            {"a": 0},
            "0a080a02016112020100",
        ),
    ],
    "list(nullable TestClass)": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (values.list_of([object_value(300)]), [300], "0a0301ac02"),
    ],
    "dictionary(string,nullable TestClass)": [
        (
            values.dictionary([(values.string(""), values.null())]),
            {"": None},
            "0a060a0100120100",
        ),
    ],
    "tuple(sint32,nullable TestClass)": [
        (values.tuple_of(values.sint32(1), values.null()), (1, None), "0a01020a0100"),
    ],
    # A nullable position holding a collection carries the presence bool ahead of the
    # collection's own encoding, which is empty for an empty collection
    "list(nullable list(uint32))": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (values.list_of([values.list_of([])]), [[]], "0a0101"),
        (
            values.list_of([values.list_of([values.uint32(1)])]),
            [[1]],
            "0a04010a0101",
        ),
    ],
    "list(list(nullable TestClass))": [
        (
            values.list_of([values.list_of([values.null()]), values.list_of([])]),
            [[None], []],
            "0a030a01000a00",
        ),
    ],
    "list(nullable TestStruct)": [
        (values.list_of([values.null()]), [None], "0a0100"),
        (
            values.list_of([struct_value(1, "foo", 2, [1])]),
            [values.Struct((1, "foo", values.Enum(2), [1]))],
            "0a12010a01020a0403666f6f0a01040a030a0102",
        ),
    ],
    "TestNullableStruct": [
        (
            values.struct(
                "TestService",
                "TestNullableStruct",
                values.sint32(1),
                values.null(),
                values.null(),
                values.null(),
                values.null(),
            ),
            values.Struct((1, None, None, None, None)),
            "0a01020a01000a01000a01000a0100",
        ),
        (
            values.struct(
                "TestService",
                "TestNullableStruct",
                values.sint32(1),
                values.sint32(0),
                enum_value(0),
                values.string(""),
                object_value(300),
            ),
            values.Struct((1, 0, values.Enum(0), "", 300)),
            "0a01020a0201000a0201000a0201000a0301ac02",
        ),
    ],
}


@pytest.mark.parametrize("typ", list(NULLABLE))
def test_nullable(connected, typ):
    """A value at a position that can hold null carries a presence bool, which alone
    encodes a null. The bool comes ahead of the value's own encoding, including that of
    a collection or a structure. The vectors set apart a null from a zero, false, an empty
    string, empty bytes and an empty collection, in list items, tuple items, dictionary
    values and structure fields."""
    check_vectors(connected, typ, NULLABLE[typ])


section("Malformed Data")


# Data each type cannot decode from: data cut short, a varint cut short, a length past the end
# of the data, a nullable item with a presence bool and no value, with nothing at all, with a
# presence bool other than 0 or 1, or with data after a null, and a dictionary entry cut short
MALFORMED = {
    "double": ["00000000000000"],
    "float": ["000000"],
    "sint32": ["ff"],
    "uint32": ["ff"],
    "uint64": ["ffffffffff"],
    "string": ["0561"],
    "bytes": ["05ab"],
    "list(sint32)": ["0a05ab", "0a"],
    "list(nullable sint32)": ["0a00", "0a0101", "0a020254", "0a020000"],
    "dictionary(string,sint32)": ["0a010a"],
}


@pytest.mark.parametrize("typ", list(MALFORMED))
def test_malformed_data(connected, typ):
    """Decoding raises the encoding error for each of these:

    * data cut short, including a varint cut short
    * a length past the end of the data
    * a nullable item with no presence bool, with a presence bool and no value, with a
      presence bool other than 0 or 1, or with data after a null
    * a dictionary entry cut short"""
    decoded = []
    for data in MALFORMED[typ]:
        try:
            connected.decode(typ, bytes.fromhex(data))
        except TesteeError as error:
            assert_error_kind(connected, error.error, pb.Error.ENCODING)
        else:
            decoded.append(data)
    assert not decoded, "Decoded malformed data %s" % decoded


# The types whose values always encode to at least one byte
NOT_EMPTY = [
    "double",
    "float",
    "sint32",
    "sint64",
    "uint32",
    "uint64",
    "bool",
    "string",
    "bytes",
]


@pytest.mark.parametrize("typ", NOT_EMPTY)
def test_empty_data(connected, typ):
    """Decoding data with no bytes as a number, a ``bool``, a string or bytes raises the
    encoding error. Each of these types encodes to at least one byte."""
    with pytest.raises(TesteeError) as info:
        connected.decode(typ, b"")
    assert_error_kind(connected, info.value.error, pb.Error.ENCODING)


# Strings that are not valid UTF-8: a lone continuation byte, a lead byte with no continuation,
# an overlong encoding, and an encoded surrogate
INVALID_UTF8 = ["0180", "02c328", "02c0af", "03eda080"]


@should
def test_invalid_utf8(connected):
    """Decoding a string that is not valid UTF-8 raises the encoding error."""
    for data in INVALID_UTF8:
        with pytest.raises(TesteeError) as info:
            connected.decode("string", bytes.fromhex(data))
        assert_error_kind(connected, info.value.error, pb.Error.ENCODING)


section("Messages")


@untested("The protobuf library encodes a protobuf message")
def test_message():
    """A protobuf message encodes as its serialized bytes, and with its length prefix
    when framed."""
