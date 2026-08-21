"""Support utilities for compiling python functions into server side
expressions: an index of the procedures provided by the server, and helpers
for working with protocol buffer type messages."""

from __future__ import annotations
from typing import Any, Dict, Optional, List, Tuple, TYPE_CHECKING

from krpc.attributes import Attributes
from krpc.service import _member_name
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


NUMERIC_CODES = (
    KRPC.Type.DOUBLE,
    KRPC.Type.FLOAT,
    KRPC.Type.SINT32,
    KRPC.Type.SINT64,
    KRPC.Type.UINT32,
    KRPC.Type.UINT64,
)


class Result:
    """The result of compiling an AST node: either a value known at compile
    time, or a server side expression along with the protocol buffer type of
    the value it evaluates to, when known."""

    def __init__(
        self,
        value: object = None,
        is_value: bool = False,
        expression: Any = None,
        ptype: Optional[KRPC.Type] = None,
    ):
        self.value = value
        self.is_value = is_value
        self.expression = expression
        self.ptype = ptype


def build_ptype(code: int, types: Optional[List[KRPC.Type]] = None) -> KRPC.Type:
    """Build a protocol buffer type message"""
    typ = KRPC.Type()
    typ.code = code  # type: ignore[assignment]
    if types:
        typ.types.extend(types)
    return typ


class Metadata:
    """An index of the procedures provided by the server, keyed by service,
    class and pythonic member name. Built from the service definitions
    reported by the server."""

    def __init__(self, client: Client):
        # (service, class name or None, python member name, kind) -> procedure
        # where kind is 'getter', 'method' or 'static'
        self.members: Dict[
            Tuple[str, Optional[str], str, str], Tuple[str, KRPC.Procedure]
        ] = {}
        # (service, structure name) -> the names the structure declares its fields
        # with, in order. A field is named to the server by the name it is declared
        # with, rather than by the pythonic name a value's attribute has
        self.struct_fields: Dict[Tuple[str, str], List[str]] = {}
        for service in client.krpc.get_services().services:
            for struct in service.structs:
                self.struct_fields[(service.name, struct.name)] = [
                    field.name for field in struct.fields
                ]
            for procedure in service.procedures:
                name = procedure.name
                key: Optional[Tuple[str, Optional[str], str, str]] = None
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


def remote_type(types: Any, ptype: Optional[KRPC.Type]) -> Any:
    """Build a KRPC.Type remote object describing the given protocol buffer
    type, using the KRPC.Type factory methods. Raises ValueError for types
    that have no remote representation."""
    if ptype is None:
        raise ValueError("cannot determine the type of a value")
    code = ptype.code
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
        return types.tuple_type([remote_type(types, sub) for sub in ptype.types])
    if code == KRPC.Type.LIST:
        return types.list_type(remote_type(types, ptype.types[0]))
    if code == KRPC.Type.SET:
        return types.set_type(remote_type(types, ptype.types[0]))
    if code == KRPC.Type.DICTIONARY:
        return types.dictionary_type(
            remote_type(types, ptype.types[0]),
            remote_type(types, ptype.types[1]),
        )
    raise ValueError("unsupported type")


def promote(
    left: Optional[KRPC.Type], right: Optional[KRPC.Type]
) -> Optional[KRPC.Type]:
    """The type that operands of the given types are promoted to by the
    server's binary operators, following C#'s numeric promotion rules.
    None when unknown."""
    if left is None or right is None:
        return None
    if left.code == right.code:
        return left
    if left.code in NUMERIC_CODES and right.code in NUMERIC_CODES:
        codes = (left.code, right.code)
        for code in (KRPC.Type.DOUBLE, KRPC.Type.FLOAT):
            if code in codes:
                return build_ptype(code)
        if KRPC.Type.UINT64 in codes:
            # Only an unsigned operand converts to an unsigned 64 bit integer
            if KRPC.Type.UINT32 in codes:
                return build_ptype(KRPC.Type.UINT64)
            return None
        if KRPC.Type.SINT64 in codes or KRPC.Type.UINT32 in codes:
            return build_ptype(KRPC.Type.SINT64)
        return build_ptype(KRPC.Type.SINT32)
    return None
