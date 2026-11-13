from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    STREAM_REMOVED,
    ClassType,
    DictionaryType,
    EnumerationType,
    EventType,
    ListType,
    MessageType,
    SetType,
    StreamType,
    StructType,
    TupleType,
    ValueType,
)
from krpc.utils import lower_camel_case

from .language import Language, float32_literal


def accessor_name(prefix, name):
    """The name of a property accessor, such as getRCSThrust for the getter of RCSThrust"""
    return prefix + name


# The JavaScript error each .NET exception the server maps one to extends, as the client
# declares in errors.ts
NATIVE_ERRORS = {
    "System.ArgumentException": "TypeError",
    "System.ArgumentNullException": "TypeError",
    "System.ArgumentOutOfRangeException": "RangeError",
    "System.NullReferenceException": "TypeError",
    "System.IndexOutOfRangeException": "RangeError",
    "System.DivideByZeroException": "RangeError",
    "System.OverflowException": "RangeError",
}


class JsLanguage(Language):
    value_map = {"null": "null", "true": "true", "false": "false"}

    special_values = {
        (Type.DOUBLE, "nan"): "NaN",
        (Type.DOUBLE, "inf"): "Infinity",
        (Type.DOUBLE, "-inf"): "-Infinity",
        (Type.DOUBLE, "max"): "Number.MAX_VALUE",
        (Type.DOUBLE, "lowest"): "-Number.MAX_VALUE",
        (Type.FLOAT, "nan"): "NaN",
        (Type.FLOAT, "inf"): "Infinity",
        (Type.FLOAT, "-inf"): "-Infinity",
    }

    def parse_name(self, name):
        return lower_camel_case(name)

    def native_exception(self, mapped_type):
        return NATIVE_ERRORS.get(mapped_type)

    def exception_alias(self, service, name, mapped_type):
        # The server removes a stream with this exception, thrown as the client's StreamError
        if (service, name) == STREAM_REMOVED:
            return "StreamError"
        return None

    def parse_type(self, typ):
        """The name of a type, as the documentation names it"""
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            code = typ.protobuf_type.code
            if code in (Type.SINT64, Type.UINT64):
                return "bigint"
            if code == Type.BOOL:
                return "boolean"
            if code == Type.STRING:
                return "string"
            if code == Type.BYTES:
                return "Uint8Array"
            return "number"
        if isinstance(typ, (StreamType, EventType)):
            return "never"
        if isinstance(typ, MessageType):
            return "schema.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
        if isinstance(typ, (ListType, TupleType)):
            return "Array"
        if isinstance(typ, SetType):
            return "Set"
        if isinstance(typ, DictionaryType):
            return "Map"
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if value is None:
            return "null"
        if isinstance(typ, ValueType):
            code = typ.protobuf_type.code
            if code == Type.STRING:
                return '"%s"' % value
            if code == Type.BOOL:
                return "true" if value else "false"
            if code == Type.BYTES:
                return "new Uint8Array([%s])" % ", ".join(str(x) for x in value)
            if code == Type.FLOAT:
                return float32_literal(value)
            if code in (Type.SINT64, Type.UINT64):
                return "%dn" % value
            return str(value)
        if isinstance(typ, EnumerationType):
            names = [
                "%s.%s" % (self.parse_type(typ), lower_camel_case(name))
                for name in self.enum_value_names(value)
            ]
            return names[0] + "".join(".union(%s)" % x for x in names[1:])
        if isinstance(typ, StructType):
            fields = (
                "%s: %s"
                % (
                    lower_camel_case(name),
                    self.parse_default_value(x, typ.field_types[i]),
                )
                for i, (name, x) in enumerate(zip(typ.field_names, value))
            )
            return "new %s({ %s })" % (self.parse_type(typ), ", ".join(fields))
        if isinstance(typ, TupleType):
            values = (
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            )
            return "[%s]" % ", ".join(values)
        if isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "[%s]" % ", ".join(values)
        if isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "new Set([%s])" % ", ".join(values)
        if isinstance(typ, DictionaryType):
            entries = (
                "[%s, %s]"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "new Map([%s])" % ", ".join(entries)
        return str(value)
