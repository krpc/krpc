"""Building the Values a test passes to a testee, and reading the ones it gets back.

A scalar argument names its type, as the testee converts it without knowing the parameter it
is for. A value the testee returns is read back as a plain Python value, with an integer of any
type as an int and a floating point value of either type as a float. A list reads back as a
list, a set as a frozenset, a tuple as a tuple and a dictionary as a dict.
"""

from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass, field
from struct import pack, unpack

import testee_pb2 as pb

# The largest single precision float
FLOAT_MAX = unpack("<f", bytes.fromhex("ffff7f7f"))[0]


def single(value: float) -> float:
    """A double as the nearest float, as a float result reads back."""
    return unpack("f", pack("f", value))[0]


@dataclass(frozen=True)
class Object:
    """An object handle the testee holds, and the server's id for the object."""

    handle: int
    id: int


@dataclass(frozen=True)
class EventHandle:
    """An event handle the testee holds."""

    handle: int


@dataclass(frozen=True)
class Enum:
    """An enum value. Equal to another by value, as a testee may not report its type."""

    value: int
    service: str = field(default="", compare=False)
    name: str = field(default="", compare=False)


@dataclass(frozen=True)
class Struct:
    """A struct value, with its fields in the order the struct declares them. Equal to another
    by its fields, as a testee may not report its type."""

    fields: tuple[object, ...]
    service: str = field(default="", compare=False)
    name: str = field(default="", compare=False)


def double(value: float) -> pb.Value:
    return pb.Value(double=value)


def float32(value: float) -> pb.Value:
    return pb.Value(float=value)


def sint32(value: int) -> pb.Value:
    return pb.Value(sint32=value)


def sint64(value: int) -> pb.Value:
    return pb.Value(sint64=value)


def uint32(value: int) -> pb.Value:
    return pb.Value(uint32=value)


def uint64(value: int) -> pb.Value:
    return pb.Value(uint64=value)


def boolean(value: bool) -> pb.Value:
    return pb.Value(bool=value)


def string(value: str) -> pb.Value:
    return pb.Value(string=value)


def data(value: bytes) -> pb.Value:
    return pb.Value(bytes=value)


def null() -> pb.Value:
    return pb.Value(null=True)


def obj(value: Object) -> pb.Value:
    return pb.Value(object=pb.Object(handle=value.handle, id=value.id))


def make_object(service: str, name: str, object_id: int) -> pb.Value:
    """An object of the named class with the given id, which the testee makes with the client's
    constructor."""
    return pb.Value(object=pb.Object(id=object_id, service=service, name=name))


def enum(service: str, name: str, value: int) -> pb.Value:
    return pb.Value(enum=pb.Enum(service=service, name=name, value=value))


def list_of(items: Iterable[pb.Value]) -> pb.Value:
    return pb.Value(list=pb.Items(items=items))


def set_of(items: Iterable[pb.Value]) -> pb.Value:
    return pb.Value(set=pb.Items(items=items))


def tuple_of(*items: pb.Value) -> pb.Value:
    return pb.Value(tuple=pb.Items(items=items))


def dictionary(entries: Iterable[tuple[pb.Value, pb.Value]]) -> pb.Value:
    return pb.Value(
        dictionary=pb.Entries(entries=[pb.Entry(key=k, value=v) for k, v in entries])
    )


def struct(service: str, name: str, *fields: pb.Value) -> pb.Value:
    return pb.Value(struct=pb.Struct(service=service, name=name, fields=fields))


def as_bytes(value: object) -> bytes:
    """A bytes result. A client whose language has no bytes type returns bytes as a string,
    which reads back as a str when it is UTF-8."""
    if isinstance(value, str):
        return value.encode("utf-8")
    assert isinstance(value, bytes)
    return value


def native(value: pb.Value) -> object:
    """A Value the testee returned, as a Python value. None for null, and for no value."""
    case = value.WhichOneof("value")
    if case is None or case == "null":
        return None
    if case == "object":
        return Object(value.object.handle, value.object.id)
    if case == "event":
        return EventHandle(value.event)
    if case == "enum":
        return Enum(value.enum.value, value.enum.service, value.enum.name)
    if case == "list":
        return [native(x) for x in value.list.items]
    if case == "set":
        return frozenset(native(x) for x in value.set.items)
    if case == "tuple":
        return tuple(native(x) for x in value.tuple.items)
    if case == "dictionary":
        return {native(x.key): native(x.value) for x in value.dictionary.entries}
    if case == "struct":
        fields = tuple(native(x) for x in value.struct.fields)
        return Struct(fields, value.struct.service, value.struct.name)
    return getattr(value, case)
