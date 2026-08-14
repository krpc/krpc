import keyword

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
from krpc.utils import snake_case
from .language import Language


class PythonLanguage(Language):

    keywords = keyword.kwlist

    value_map = {"null": "None", "true": "True", "false": "False"}

    # Python names none of the finite extremes; every one of them is exact as a literal,
    # as Python integers are unbounded and its floats are doubles.
    special_values = {
        (Type.DOUBLE, "nan"): 'float("nan")',
        (Type.DOUBLE, "inf"): 'float("inf")',
        (Type.DOUBLE, "-inf"): '-float("inf")',
        (Type.FLOAT, "nan"): 'float("nan")',
        (Type.FLOAT, "inf"): 'float("inf")',
        (Type.FLOAT, "-inf"): '-float("inf")',
    }

    def parse_name(self, name):
        return super().parse_name(snake_case(name))

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            # python3 fix: get type name from protobuf type code
            if typ.protobuf_type.code in (Type.SINT64, Type.UINT64):
                return "int"
            if typ.protobuf_type.code == Type.BYTES:
                return "bytes"
            if typ.protobuf_type.code == Type.DOUBLE:
                return "float"
            return typ.python_type.__name__
        if isinstance(typ, MessageType):
            return "krpc.schema.KRPC.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
            )
        if isinstance(typ, ListType):
            return "list"
        if isinstance(typ, DictionaryType):
            return "dict"
        if isinstance(typ, SetType):
            return "set"
        if isinstance(typ, TupleType):
            return "tuple"
        if typ is None:
            return "None"
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if value is None:
            return "None"
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return "'%s'" % value
        if isinstance(typ, EnumerationType):
            return "%s.%s" % (
                self.parse_type(typ),
                self.parse_enum_value_name(value.name),
            )
        if isinstance(typ, TupleType):
            values = [
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            ]
            # A tuple of one value needs the trailing comma to be a tuple at all
            return "(%s%s)" % (", ".join(values), "," if len(values) == 1 else "")
        if isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "[%s]" % ", ".join(values)
        if isinstance(typ, SetType):
            if not value:
                return "set()"
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "{%s}" % ", ".join(values)
        if isinstance(typ, DictionaryType):
            entries = (
                "%s: %s"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "{%s}" % ", ".join(entries)
        return str(value)

    def shorten_ref(self, name):
        name = name.split(".")
        if name[0] == self.module:
            del name[0]
        return ".".join(name)
