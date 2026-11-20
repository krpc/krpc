import base64
import enum
import json
import math

from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ClassType,
    DictionaryType,
    EnumerationType,
    ListType,
    SetType,
    StructType,
    TupleType,
    ValueType,
)
from krpc.utils import snake_case

from .language import Language, float32_literal

# The classes that build server side functions. Nothing runs a function over HTTP, so a
# procedure that takes or returns one has no path
EXCLUDED_CLASSES = {("KRPC", "Expression"), ("KRPC", "Type")}


def kebab_case(name):
    """A name in kebab case, the words of its snake case joined by '-'"""
    return snake_case(name).replace("_", "-")


class HttpLanguage(Language):
    """The JSON the HTTP server carries values as."""

    type_map = {
        Type.DOUBLE: "double",
        Type.FLOAT: "float",
        Type.SINT32: "int32",
        Type.SINT64: "int64",
        Type.UINT32: "uint32",
        Type.UINT64: "uint64",
        Type.BOOL: "bool",
        Type.STRING: "string",
        Type.BYTES: "bytes",
    }

    @staticmethod
    def supported(typ):
        """Whether a value of the type can be carried in JSON"""
        if isinstance(typ, ClassType):
            name = (typ.protobuf_type.service, typ.protobuf_type.name)
            return name not in EXCLUDED_CLASSES
        if isinstance(typ, (ValueType, EnumerationType, StructType)):
            return True
        if isinstance(typ, TupleType):
            return all(HttpLanguage.supported(x) for x in typ.value_types)
        if isinstance(typ, (ListType, SetType)):
            return HttpLanguage.supported(typ.value_type)
        if isinstance(typ, DictionaryType):
            return HttpLanguage.supported(typ.key_type) and HttpLanguage.supported(
                typ.value_type
            )
        return False

    def parse_type(self, typ) -> str:
        if isinstance(typ, ValueType):
            name = self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, (ClassType, EnumerationType, StructType)):
            name = "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
        elif isinstance(typ, ListType):
            name = "list<%s>" % self.parse_type(typ.value_type)
        elif isinstance(typ, SetType):
            name = "set<%s>" % self.parse_type(typ.value_type)
        elif isinstance(typ, TupleType):
            name = "tuple<%s>" % ", ".join(self.parse_type(x) for x in typ.value_types)
        elif isinstance(typ, DictionaryType):
            name = "dictionary<%s, %s>" % (
                self.parse_type(typ.key_type),
                self.parse_type(typ.value_type),
            )
        else:
            raise RuntimeError("Unknown type '%s'" % str(typ))
        return name + "?" if typ.nullable else name

    def parse_default_value(self, value, typ):
        return self._json(value, typ)

    def _json(self, value, typ):  # noqa: PLR0911
        if value is None:
            return "null"
        if isinstance(typ, ValueType):
            code = typ.protobuf_type.code
            if code in (Type.DOUBLE, Type.FLOAT):
                if math.isnan(value):
                    return '"NaN"'
                if math.isinf(value):
                    return '"Infinity"' if value > 0 else '"-Infinity"'
                return float32_literal(value) if code == Type.FLOAT else repr(value)
            if code == Type.BYTES:
                return json.dumps(base64.b64encode(value).decode("ascii"))
            return json.dumps(value)
        if isinstance(typ, EnumerationType):
            names = [snake_case(x) for x in self.enum_value_names(value)]
            if isinstance(value, enum.Flag):
                return json.dumps(names)
            return json.dumps(names[0])
        if isinstance(typ, StructType):
            fields = (
                "%s: %s"
                % (
                    json.dumps(snake_case(name)),
                    self._json(value[i], typ.field_types[i]),
                )
                for i, name in enumerate(typ.field_names)
            )
            return "{%s}" % ", ".join(fields)
        if isinstance(typ, TupleType):
            return "[%s]" % ", ".join(
                self._json(x, typ.value_types[i]) for i, x in enumerate(value)
            )
        if isinstance(typ, (ListType, SetType)):
            return "[%s]" % ", ".join(self._json(x, typ.value_type) for x in value)
        if isinstance(typ, DictionaryType):
            if (
                isinstance(typ.key_type, ValueType)
                and typ.key_type.protobuf_type.code == Type.STRING
            ):
                return "{%s}" % ", ".join(
                    "%s: %s" % (json.dumps(k), self._json(v, typ.value_type))
                    for k, v in value.items()
                )
            return "[%s]" % ", ".join(
                "[%s, %s]"
                % (self._json(k, typ.key_type), self._json(v, typ.value_type))
                for k, v in value.items()
            )
        return json.dumps(value)
