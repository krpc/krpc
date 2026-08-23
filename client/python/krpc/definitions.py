from __future__ import annotations
from collections.abc import Hashable
from typing import (
    Callable,
    Dict,
    Iterable,
    Iterator,
    List,
    Optional,
    Set,
    Tuple,
    TypeVar,
    TYPE_CHECKING,
)
from krpc.decoder import Decoder
from krpc.types import (
    DictionaryType,
    EnumerationType,
    ListType,
    SetType,
    StructType,
    TupleType,
    TypeBase,
    Types,
)
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


CLASS = "class"
ENUMERATION = "enumeration"
EXCEPTION = "exception"
STRUCT = "struct"

# The kind of definition that each named protocol buffer type refers to
_KIND_FROM_CODE = {
    KRPC.Type.CLASS: CLASS,
    KRPC.Type.ENUMERATION: ENUMERATION,
    KRPC.Type.STRUCT: STRUCT,
}

# What identifies a definition, and therefore what a reference to one resolves against
Key = Tuple[str, str, str]

Node = TypeVar("Node")
NodeKey = TypeVar("NodeKey", bound=Hashable)


class Definition:
    """Something a service defines - a class, an enumeration or an exception - together with
    the types it refers to and how to register it in a type registry.

    Whatever a set of definitions was read from builds these, and register_all registers them
    in an order in which a definition is only registered once everything it refers to is.
    """

    def __init__(
        self,
        kind: str,
        service: str,
        name: str,
        references: Iterable[KRPC.Type],
        register: Callable[[Types], None],
    ) -> None:
        self.kind = kind
        self.service = service
        self.name = name
        self.references = list(references)
        self.register = register

    @property
    def key(self) -> Key:
        return (self.kind, self.service, self.name)

    def __str__(self) -> str:
        return "%s %s.%s" % (self.kind, self.service, self.name)


def register_all(types: Types, definitions: Iterable[Definition]) -> None:
    """Register the given definitions in the given type registry, each one after everything it
    refers to, so that every type in the registry can be used without regard to the order the
    definitions arrived in.

    Raises a RuntimeError if a definition refers to something none of the given definitions
    define, or if they refer to each other in a cycle."""
    index: Dict[Key, Definition] = {}
    for definition in definitions:
        if definition.key in index:
            raise RuntimeError("%s is defined more than once" % definition)
        index[definition.key] = definition

    def dependencies(definition: Definition) -> Iterator[Definition]:
        for kind, service, name in _referenced_keys(definition.references):
            referenced = index.get((kind, service, name))
            if referenced is None:
                raise RuntimeError(
                    "%s refers to the %s %s.%s, which none of the given service "
                    "definitions define" % (definition, kind, service, name)
                )
            yield referenced

    for definition in topological_order(index.values(), lambda x: x.key, dependencies):
        definition.register(types)


def topological_order(
    nodes: Iterable[Node],
    key: Callable[[Node], NodeKey],
    dependencies: Callable[[Node], Iterable[Node]],
) -> List[Node]:
    """Return the given nodes, and the nodes they depend on, ordered so that every node follows
    everything it depends on. Nodes that depend on nothing keep the order they were given in.

    Raises a RuntimeError if the dependencies contain a cycle."""
    ordered: List[Node] = []
    ordered_keys: Set[NodeKey] = set()
    path: List[Node] = []
    path_keys: Set[NodeKey] = set()

    def visit(node: Node) -> None:
        node_key = key(node)
        if node_key in ordered_keys:
            return
        if node_key in path_keys:
            start = next(i for i, x in enumerate(path) if key(x) == node_key)
            cycle = " -> ".join(str(x) for x in path[start:] + [node])
            raise RuntimeError("Cyclic dependency: " + cycle)
        path.append(node)
        path_keys.add(node_key)
        for dependency in dependencies(node):
            visit(dependency)
        path.pop()
        path_keys.remove(node_key)
        ordered.append(node)
        ordered_keys.add(node_key)

    for node in nodes:
        visit(node)
    return ordered


def decode_default_value(
    client: Optional[Client], value: bytes, typ: TypeBase, location: str
) -> object:
    """Decode the default value of a parameter from the encoded form a definition carries it in.

    location names the parameter the value belongs to, for example
    'TestService.EnumDefaultArg parameter x', and is included in the error raised when the value
    cannot be decoded."""
    _check_registered(typ, location)
    try:
        return Decoder.decode(client, value, typ)
    except ValueError as exn:
        raise RuntimeError("%s: %s" % (location, exn)) from exn


def _check_registered(typ: TypeBase, location: str) -> None:
    """Raise a RuntimeError if the given type, or a type it contains, is an enumeration whose
    values, or a structure whose fields, were never registered and are therefore unknown.
    """
    if isinstance(typ, EnumerationType):
        if not typ.has_values:
            service = typ.protobuf_type.service
            raise RuntimeError(
                "%s has the enumeration %s.%s in its type, but the definitions for service "
                "%s were not provided"
                % (location, service, typ.protobuf_type.name, service)
            )
    elif isinstance(typ, StructType):
        if not typ.has_fields:
            service = typ.protobuf_type.service
            raise RuntimeError(
                "%s has the struct %s.%s in its type, but the definitions for service "
                "%s were not provided"
                % (location, service, typ.protobuf_type.name, service)
            )
        for field_type in typ.field_types:
            _check_registered(field_type, location)
    elif isinstance(typ, TupleType):
        for value_type in typ.value_types:
            _check_registered(value_type, location)
    elif isinstance(typ, (ListType, SetType)):
        _check_registered(typ.value_type, location)
    elif isinstance(typ, DictionaryType):
        _check_registered(typ.key_type, location)
        _check_registered(typ.value_type, location)


def _referenced_keys(protobuf_types: Iterable[KRPC.Type]) -> Iterator[Key]:
    """The definitions that the given types refer to, including those they refer to through a
    collection type."""
    for protobuf_type in protobuf_types:
        kind = _KIND_FROM_CODE.get(protobuf_type.code)
        if kind is not None:
            yield (kind, protobuf_type.service, protobuf_type.name)
        yield from _referenced_keys(protobuf_type.types)
