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
from .language import Language, float32_literal


class PythonLanguage(Language):

    keywords = keyword.kwlist

    value_map = {"null": "None", "true": "True", "false": "False"}

    # Python builds NaN and the infinities from a string, and takes the finite extremes
    # from krpc.limits, which names them because the language does not: its integers are
    # unbounded and its floats are doubles, so nothing in the language or its standard
    # library names the width of a wire type.
    special_values = {
        (Type.DOUBLE, "nan"): 'float("nan")',
        (Type.DOUBLE, "inf"): 'float("inf")',
        (Type.DOUBLE, "-inf"): '-float("inf")',
        (Type.DOUBLE, "max"): "krpc.limits.DOUBLE_MAX",
        (Type.DOUBLE, "lowest"): "krpc.limits.DOUBLE_LOWEST",
        (Type.FLOAT, "nan"): 'float("nan")',
        (Type.FLOAT, "inf"): 'float("inf")',
        (Type.FLOAT, "-inf"): '-float("inf")',
        (Type.FLOAT, "max"): "krpc.limits.FLOAT_MAX",
        (Type.FLOAT, "lowest"): "krpc.limits.FLOAT_LOWEST",
        (Type.SINT32, "max"): "krpc.limits.SINT32_MAX",
        (Type.SINT32, "min"): "krpc.limits.SINT32_MIN",
        (Type.SINT64, "max"): "krpc.limits.SINT64_MAX",
        (Type.SINT64, "min"): "krpc.limits.SINT64_MIN",
        (Type.UINT32, "max"): "krpc.limits.UINT32_MAX",
        (Type.UINT64, "max"): "krpc.limits.UINT64_MAX",
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
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.FLOAT:
            return float32_literal(value)
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
