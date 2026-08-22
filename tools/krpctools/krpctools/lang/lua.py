# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ValueType,
    ClassType,
    EnumerationType,
    StructType,
    MessageType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
from krpc.utils import snake_case
from .language import Language, float32_literal


class LuaLanguage(Language):

    value_map = {"null": "nil", "true": "True", "false": "False"}

    type_map = {
        Type.DOUBLE: "number",
        Type.FLOAT: "number",
        Type.SINT32: "number",
        Type.SINT64: "number",
        Type.UINT32: "number",
        Type.UINT64: "number",
        Type.BOOL: "boolean",
        Type.STRING: "string",
        Type.BYTES: "string",
    }

    # Lua builds NaN by dividing zero by zero and has math.huge for the infinities, but
    # names no finite extreme of its own: math.maxinteger and math.mininteger arrive in
    # Lua 5.3 and the client targets 5.1 and 5.2. Those come from krpc.limits.
    special_values = {
        (Type.DOUBLE, "nan"): "0/0",
        (Type.DOUBLE, "inf"): "math.huge",
        (Type.DOUBLE, "-inf"): "-math.huge",
        (Type.DOUBLE, "max"): "krpc.limits.DOUBLE_MAX",
        (Type.DOUBLE, "lowest"): "krpc.limits.DOUBLE_LOWEST",
        (Type.FLOAT, "nan"): "0/0",
        (Type.FLOAT, "inf"): "math.huge",
        (Type.FLOAT, "-inf"): "-math.huge",
        (Type.FLOAT, "max"): "krpc.limits.FLOAT_MAX",
        (Type.FLOAT, "lowest"): "krpc.limits.FLOAT_LOWEST",
        (Type.SINT32, "max"): "krpc.limits.SINT32_MAX",
        (Type.SINT32, "min"): "krpc.limits.SINT32_MIN",
        (Type.SINT64, "max"): "krpc.limits.SINT64_MAX",
        (Type.SINT64, "min"): "krpc.limits.SINT64_MIN",
        (Type.UINT32, "max"): "krpc.limits.UINT32_MAX",
        (Type.UINT64, "max"): "krpc.limits.UINT64_MAX",
    }

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return "krpc.schema.KRPC.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
        if isinstance(typ, ListType):
            return "List"
        if isinstance(typ, DictionaryType):
            return "Map"
        if isinstance(typ, SetType):
            return "Set"
        if isinstance(typ, TupleType):
            return "Tuple"
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def parse_enum_value_name(self, name):
        return snake_case(name)

    def parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return "'%s'" % value
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BOOL:
            return "true" if value else "false"
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.FLOAT:
            return float32_literal(value)
        if isinstance(typ, EnumerationType):
            return "%s.%s" % (
                self.parse_type(typ),
                self.parse_enum_value_name(value.name),
            )
        if isinstance(typ, StructType):
            # A structure is written as its field values in order, which is the form the
            # client coerces to one
            values = (
                self.parse_default_value(x, typ.field_types[i])
                for i, x in enumerate(value)
            )
            return "{%s}" % ", ".join(values)
        if isinstance(typ, TupleType):
            values = (
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            )
            return "{%s}" % ", ".join(values)
        if isinstance(typ, (ListType, SetType)):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "{%s}" % ", ".join(values)
        if isinstance(typ, DictionaryType):
            entries = (
                "[%s] = %s"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "{%s}" % ", ".join(entries)
        return str(value)
