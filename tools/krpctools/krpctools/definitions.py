from krpc.definitions import (
    CLASS,
    ENUMERATION,
    EXCEPTION,
    STRUCT,
    Definition,
    register_all,
    topological_order,
)
from krpc.types import Types, TupleType, ListType, SetType, DictionaryType, StructType
from .utils import as_type, as_protobuf_type


class Definitions:
    """The service definitions a single run of a generator was given, with a type registry in
    which everything they define is registered.

    The registry belongs to the run rather than to the process, so that generating from one set
    of definitions does not see the types of another."""

    def __init__(self, services_info):
        self._services = services_info
        self._types = Types()
        register_all(self._types, _definitions(services_info))

    @property
    def types(self):
        """The type registry the definitions were registered in"""
        return self._types

    @property
    def services(self):
        """The definitions themselves, keyed by service name"""
        return self._services

    def as_type(self, type_info):
        """Convert a type parsed from a service definitions file into a type object"""
        return as_type(self._types, type_info)

    @staticmethod
    def collection_types(types):
        """The structural types among the given types, and the ones they contain, innermost
        first, so that a type always follows the types it is composed of. A structure counts as
        one, as it is composed of the types of its fields in the same way."""
        return topological_order(
            [typ for typ in types if _is_collection(typ)],
            lambda typ: typ.protobuf_type.SerializeToString(),
            _contained_collection_types,
        )


def _definitions(services_info):
    """A definition record for everything the given services define"""
    for service_name, info in services_info.items():
        for name in info.get("classes", {}):
            yield Definition(
                CLASS, service_name, name, [], _register_class(service_name, name)
            )
        for name, enumeration in info.get("enumerations", {}).items():
            yield Definition(
                ENUMERATION,
                service_name,
                name,
                [],
                _register_enumeration(service_name, name, enumeration),
            )
        for name in info.get("exceptions", {}):
            yield Definition(
                EXCEPTION,
                service_name,
                name,
                [],
                _register_exception(service_name, name),
            )
        for name, struct in info.get("structs", {}).items():
            # A structure is registered after the definitions its field types name, as
            # building its fields resolves those types
            yield Definition(
                STRUCT,
                service_name,
                name,
                [as_protobuf_type(field["type"]) for field in struct["fields"]],
                _register_struct(service_name, name, struct),
            )


def _register_class(service, name):
    def register(types):
        types.class_type(service, name)

    return register


def _register_enumeration(service, name, enumeration):
    # The values keep the names they are declared with, as each language names them its own way
    values = dict(
        (
            value["name"],
            {"value": value["value"], "doc": value["documentation"]},
        )
        for value in enumeration["values"]
    )

    def register(types):
        types.enumeration_type(service, name).set_values(values)

    return register


def _register_struct(service, name, struct):
    # The fields keep the names they are declared with, as each language names them its own way
    fields = [(field["name"], field["type"]) for field in struct["fields"]]

    def register(types):
        types.struct_type(service, name).set_fields(
            [
                (field_name, as_type(types, type_info))
                for field_name, type_info in fields
            ]
        )

    return register


def _register_exception(service, name):
    def register(types):
        types.exception_type(service, name)

    return register


def _is_collection(typ):
    return isinstance(typ, (TupleType, ListType, SetType, DictionaryType, StructType))


def _contained_collection_types(typ):
    if isinstance(typ, TupleType):
        contained = typ.value_types
    elif isinstance(typ, StructType):
        contained = typ.field_types
    elif isinstance(typ, (ListType, SetType)):
        contained = [typ.value_type]
    elif isinstance(typ, DictionaryType):
        contained = [typ.key_type, typ.value_type]
    else:
        contained = []
    return [x for x in contained if _is_collection(x)]
