"""Support utilities for compiling Python functions into server side
expressions: an index of the procedures provided by the server, and helpers
for working with protocol buffer type messages."""

from __future__ import annotations

from collections.abc import Sequence
from typing import TYPE_CHECKING, Any

import krpc.schema.KRPC_pb2 as KRPC
from krpc.attributes import Attributes
from krpc.service import _member_name
from krpc.types import WrappedClass

if TYPE_CHECKING:
    from krpc.client import Client


def unwrap_class(value: object) -> object:
    """The value, with a wrapped class replaced by the class it wraps. A class
    accessed through a service with the pre-generated stubs is a WrappedClass
    around a subclass of the stub class."""
    if isinstance(value, WrappedClass):
        return value._class_type
    return value


NUMERIC_CODES = (
    KRPC.Type.DOUBLE,
    KRPC.Type.FLOAT,
    KRPC.Type.SINT32,
    KRPC.Type.SINT64,
    KRPC.Type.UINT32,
    KRPC.Type.UINT64,
)


# The name each numeric type is known by, used when reporting a value that
# would narrow one
NUMERIC_NAMES = {
    KRPC.Type.DOUBLE: "double",
    KRPC.Type.FLOAT: "float",
    KRPC.Type.SINT32: "int",
    KRPC.Type.SINT64: "long",
    KRPC.Type.UINT32: "uint",
    KRPC.Type.UINT64: "ulong",
}


class Result:
    """The result of compiling an AST node: either a value known at compile
    time, or a server side expression along with the protocol buffer type of
    the value it evaluates to, when known."""

    def __init__(
        self,
        value: object = None,
        is_value: bool = False,
        expression: Any = None,
        ptype: KRPC.Type | None = None,
    ):
        self.value = value
        self.is_value = is_value
        self.expression = expression
        self.ptype = ptype


def build_ptype(code: int, types: list[KRPC.Type] | None = None) -> KRPC.Type:
    """Build a protocol buffer type message"""
    typ = KRPC.Type()
    typ.code = code  # pyright: ignore[reportAttributeAccessIssue]
    if types:
        typ.types.extend(types)
    return typ


class Metadata:
    """An index of the procedures provided by the server, keyed by service,
    class and pythonic member name. Built from the service definitions
    reported by the server."""

    def __init__(self, client: Client):
        # (service, class name or None, Python member name, kind) -> procedure
        # where kind is 'getter', 'setter', 'method' or 'static'
        self.members: dict[
            tuple[str, str | None, str, str], tuple[str, KRPC.Procedure]
        ] = {}
        # (service, structure name) -> the names the structure declares its fields
        # with, in order. A field is named to the server by the name it is declared
        # with, which can differ from the pythonic name of a value's attribute
        self.struct_fields: dict[tuple[str, str], list[str]] = {}
        # (service, exception name) of every exception the services declare
        self.exceptions: list[tuple[str, str]] = []
        # (service, exception name) -> the exception's mapped type, where it has one
        self.mapped_types: dict[tuple[str, str], str] = {}
        # (service, procedure name) -> procedure
        self.procedures: dict[tuple[str, str], KRPC.Procedure] = {}
        for service in client.krpc.get_services().services:
            for exception in service.exceptions:
                self.exceptions.append((service.name, exception.name))
                if exception.mapped_type:
                    self.mapped_types[(service.name, exception.name)] = (
                        exception.mapped_type
                    )
            for struct in service.structs:
                self.struct_fields[(service.name, struct.name)] = [
                    field.name for field in struct.fields
                ]
            for procedure in service.procedures:
                name = procedure.name
                self.procedures[(service.name, name)] = procedure
                key: tuple[str, str | None, str, str] | None = None
                if Attributes.is_a_class_member(name):
                    class_name = Attributes.get_class_name(name)
                    member = _member_name(Attributes.get_class_member_name(name))
                    if Attributes.is_a_class_property_getter(name):
                        key = (service.name, class_name, member, "getter")
                    elif Attributes.is_a_class_property_setter(name):
                        key = (service.name, class_name, member, "setter")
                    elif Attributes.is_a_class_static_method(name):
                        key = (service.name, class_name, member, "static")
                    elif Attributes.is_a_class_method(name):
                        key = (service.name, class_name, member, "method")
                elif Attributes.is_a_property_getter(name):
                    member = _member_name(Attributes.get_property_name(name))
                    key = (service.name, None, member, "getter")
                elif Attributes.is_a_property_setter(name):
                    member = _member_name(Attributes.get_property_name(name))
                    key = (service.name, None, member, "setter")
                elif Attributes.is_a_procedure(name):
                    key = (service.name, None, _member_name(name), "method")
                if key is not None:
                    self.members[key] = (service.name, procedure)


def remote_type(
    types: Any,
    ptype: KRPC.Type | None,
    cache: dict[bytes, Any] | None = None,
) -> Any:
    """Build a KRPC.Type remote object describing the given protocol buffer
    type, using the KRPC.Type factory methods. Raises ValueError for types
    that have no remote representation.

    Each factory call is a round trip to the server. A function names the same
    few types over and over, so the object built for a type is kept in cache."""
    if ptype is None:
        raise ValueError("cannot determine the type of a value")
    if cache is None:
        cache = {}
    key = ptype.SerializeToString()
    if key in cache:
        return cache[key]
    remote = _build_remote_type(types, ptype, cache)
    cache[key] = remote
    return remote


def _build_remote_type(types: Any, ptype: KRPC.Type, cache: dict[bytes, Any]) -> Any:
    code = ptype.code
    if ptype.nullable:
        return types.nullable_type(
            remote_type(types, with_nullable(ptype, False), cache)
        )
    if code == KRPC.Type.DOUBLE:
        return types.double()
    if code == KRPC.Type.FLOAT:
        return types.float()
    if code == KRPC.Type.SINT32:
        return types.int()
    if code == KRPC.Type.SINT64:
        return types.long()
    if code == KRPC.Type.UINT32:
        return types.u_int()
    if code == KRPC.Type.UINT64:
        return types.u_long()
    if code == KRPC.Type.BOOL:
        return types.bool()
    if code == KRPC.Type.STRING:
        return types.string()
    if code == KRPC.Type.BYTES:
        return types.bytes()
    if code == KRPC.Type.CLASS:
        return types.class_type(ptype.service, ptype.name)
    if code == KRPC.Type.ENUMERATION:
        return types.enumeration_type(ptype.service, ptype.name)
    if code == KRPC.Type.STRUCT:
        return types.struct_type(ptype.service, ptype.name)
    if code == KRPC.Type.TUPLE:
        return types.tuple_type([remote_type(types, sub, cache) for sub in ptype.types])
    if code == KRPC.Type.LIST:
        return types.list_type(remote_type(types, ptype.types[0], cache))
    if code == KRPC.Type.SET:
        return types.set_type(remote_type(types, ptype.types[0], cache))
    if code == KRPC.Type.DICTIONARY:
        return types.dictionary_type(
            remote_type(types, ptype.types[0], cache),
            remote_type(types, ptype.types[1], cache),
        )
    raise ValueError("unsupported type")


def promote(left: KRPC.Type | None, right: KRPC.Type | None) -> KRPC.Type | None:
    """The type that operands of the given types are promoted to by the
    server's binary operators, following C#'s numeric promotion rules. An
    operator takes the value a nullable number or bool holds, so the result is
    not nullable. None when unknown."""
    if left is None or right is None:
        return None
    if left.code in NUMERIC_CODES and right.code in NUMERIC_CODES:
        code = common_numeric_code({left.code, right.code})
        return None if code is None else build_ptype(code)
    if left.code == KRPC.Type.BOOL and right.code == KRPC.Type.BOOL:
        return build_ptype(KRPC.Type.BOOL)
    if left == right:
        return left
    return None


def common_numeric_code(codes: set[int]) -> int | None:
    """The narrowest numeric type that every one of a set of numeric types
    widens to, or None when there is none. For two types it is C#'s binary
    numeric promotion."""
    for code in (KRPC.Type.DOUBLE, KRPC.Type.FLOAT):
        if code in codes:
            return code
    if KRPC.Type.UINT64 in codes:
        # Only an unsigned type converts to an unsigned 64 bit integer
        if codes <= {KRPC.Type.UINT32, KRPC.Type.UINT64}:
            return KRPC.Type.UINT64
        return None
    if KRPC.Type.SINT64 in codes or codes >= {KRPC.Type.SINT32, KRPC.Type.UINT32}:
        return KRPC.Type.SINT64
    if KRPC.Type.UINT32 in codes:
        return KRPC.Type.UINT32
    return KRPC.Type.SINT32


def with_nullable(ptype: KRPC.Type, nullable: bool) -> KRPC.Type:
    """A copy of a type, nullable or not at the outermost position."""
    result = KRPC.Type()
    result.CopyFrom(ptype)
    result.nullable = nullable
    return result


def same_shape(left: KRPC.Type, right: KRPC.Type) -> bool:
    """Whether two types are the same, apart from which positions are nullable."""
    return (
        left.code == right.code
        and left.service == right.service
        and left.name == right.name
        and len(left.types) == len(right.types)
        and all(same_shape(a, b) for a, b in zip(left.types, right.types))
    )


def join_ptype(left: KRPC.Type | None, right: KRPC.Type | None) -> KRPC.Type | None:
    """The type of a value that is one of two values, as the server joins the
    branches of a conditional."""
    return common_ptype([left, right])


def common_ptype(ptypes: Sequence[KRPC.Type | None]) -> KRPC.Type | None:
    """The type the server gives a value that is one of several values, such as
    the values of a collection or the values a function returns. It depends only
    on which types are present: nullable wherever any is, the widest of a set of
    numbers, and tuples and collections position by position. None when any is
    unknown, or when there is no common type."""
    known = [ptype for ptype in ptypes if ptype is not None]
    if not known or len(known) < len(ptypes):
        return None
    nullable = any(ptype.nullable for ptype in known)
    first = known[0]
    if all(ptype.code in NUMERIC_CODES for ptype in known):
        code = common_numeric_code({ptype.code for ptype in known})
        return None if code is None else with_nullable(build_ptype(code), nullable)
    if not all(
        ptype.code == first.code
        and ptype.service == first.service
        and ptype.name == first.name
        and len(ptype.types) == len(first.types)
        for ptype in known
    ):
        return None
    joined = with_nullable(first, nullable)
    del joined.types[:]
    for position in zip(*(ptype.types for ptype in known)):
        common = common_ptype(position)
        if common is None:
            return None
        joined.types.append(common)
    return joined


def is_no_more_nullable(ptype: KRPC.Type, than: KRPC.Type) -> bool:
    """Whether every nullable position of a type is also nullable in another
    type of the same shape."""
    if ptype.nullable and not than.nullable:
        return False
    return all(is_no_more_nullable(a, b) for a, b in zip(ptype.types, than.types))


# The types whose values the server holds by value, where a null is part of the
# value of the nullable form
_VALUE_TYPE_CODES = NUMERIC_CODES + (
    KRPC.Type.BOOL,
    KRPC.Type.ENUMERATION,
    KRPC.Type.STRUCT,
)


def plain_ptype(ptype: KRPC.Type) -> KRPC.Type:
    """The type the server gives a value it computes, nullable only where a
    value type is in its nullable form."""
    result = KRPC.Type()
    result.CopyFrom(ptype)

    def clear(typ: KRPC.Type) -> None:
        if typ.code not in _VALUE_TYPE_CODES:
            typ.nullable = False
        for element in typ.types:
            clear(element)

    clear(result)
    return result
