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
from .language import Language, float32_literal


class CsharpLanguage(Language):

    keywords = set(
        [
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            "while",
        ]
    )

    type_map = {
        Type.DOUBLE: "double",
        Type.FLOAT: "float",
        Type.SINT32: "int",
        Type.SINT64: "long",
        Type.UINT32: "uint",
        Type.UINT64: "ulong",
        Type.BOOL: "bool",
        Type.STRING: "string",
        Type.BYTES: "byte[]",
    }

    # The named constants are already of the type they belong to, so the "f" suffix that a
    # FLOAT literal needs must not be appended to them.
    special_values = {
        (Type.DOUBLE, "nan"): "double.NaN",
        (Type.DOUBLE, "inf"): "double.PositiveInfinity",
        (Type.DOUBLE, "-inf"): "double.NegativeInfinity",
        (Type.DOUBLE, "max"): "double.MaxValue",
        (Type.DOUBLE, "lowest"): "double.MinValue",
        (Type.FLOAT, "nan"): "float.NaN",
        (Type.FLOAT, "inf"): "float.PositiveInfinity",
        (Type.FLOAT, "-inf"): "float.NegativeInfinity",
        (Type.FLOAT, "max"): "float.MaxValue",
        (Type.FLOAT, "lowest"): "float.MinValue",
        (Type.SINT32, "max"): "int.MaxValue",
        (Type.SINT32, "min"): "int.MinValue",
        (Type.SINT64, "max"): "long.MaxValue",
        (Type.SINT64, "min"): "long.MinValue",
        (Type.UINT32, "max"): "uint.MaxValue",
        (Type.UINT64, "max"): "ulong.MaxValue",
    }

    def parse_type(self, typ):
        return self._parse_type(typ)

    def _parse_type(self, typ, interface=True):
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType) and typ.protobuf_type.code == Type.EVENT:
            return "global::KRPC.Client.Event"
        if isinstance(typ, MessageType):
            return "global::KRPC.Schema.KRPC.%s" % typ.python_type.__name__
        if isinstance(typ, TupleType):
            return "systemAlias::Tuple<%s>" % ",".join(
                self._at_position(t) for t in typ.value_types
            )
        if isinstance(typ, ListType):
            if interface:
                name = "IList"
            else:
                name = "List"
            return "global::System.Collections.Generic.%s<%s>" % (
                name,
                self._at_position(typ.value_type),
            )
        if isinstance(typ, SetType):
            if interface:
                name = "genericCollectionsAlias::ISet"
            else:
                name = "global::System.Collections.Generic.HashSet"
            return "%s<%s>" % (name, self._parse_type(typ.value_type))
        if isinstance(typ, DictionaryType):
            if interface:
                name = "IDictionary"
            else:
                name = "Dictionary"
            return "global::System.Collections.Generic.%s<%s,%s>" % (
                name,
                self._parse_type(typ.key_type),
                self._at_position(typ.value_type),
            )
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return "global::KRPC.Client.Services.%s.%s" % (
                typ.protobuf_type.service,
                typ.protobuf_type.name,
            )
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def _at_position(self, typ):
        """The C# type of a value at a position inside a collection. A value type takes the
        nullable form T? where the position can hold null; a reference type is nullable in C#
        already, and the generated stub names the position for the encoder instead."""
        name = self._parse_type(typ)
        if typ.nullable and self.takes_the_nullable_form(typ):
            return name + "?"
        return name

    @staticmethod
    def takes_the_nullable_form(typ):
        """Whether a value at a nullable position is written T?, which a C# value type is
        and a reference type is not."""
        if isinstance(typ, (EnumerationType, StructType)):
            return True
        return isinstance(typ, ValueType) and typ.protobuf_type.code not in (
            Type.STRING,
            Type.BYTES,
        )

    def parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return '"%s"' % value
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BOOL:
            return "true" if value else "false"
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.FLOAT:
            return float32_literal(value) + "f"
        if isinstance(typ, ClassType) and value is None:
            return "null"
        if isinstance(typ, EnumerationType):
            return "%s.%s" % (
                self.parse_type(typ),
                self.parse_enum_value_name(value.name),
            )
        if value is None:
            return "null"
        if isinstance(typ, StructType):
            values = (
                self.parse_default_value(x, typ.field_types[i])
                for i, x in enumerate(value)
            )
            return "new %s (%s)" % (self.parse_type(typ), ", ".join(values))
        if isinstance(typ, TupleType):
            values = (
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            )
            return "new %s (%s)" % (self._parse_type(typ, False), ", ".join(values))
        if isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "new %s { %s }" % (self._parse_type(typ, False), ", ".join(values))
        if isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "new %s { %s }" % (self._parse_type(typ, False), ", ".join(values))
        if isinstance(typ, DictionaryType):
            entries = (
                "{ %s, %s }"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "new %s {%s}" % (self._parse_type(typ, False), ", ".join(entries))
        return str(value)
