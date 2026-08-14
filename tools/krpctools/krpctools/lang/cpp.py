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


class CppLanguage(Language):

    keywords = set(
        [
            "alignas",
            "alignof",
            "and",
            "and_eq",
            "asm",
            "auto",
            "bitand",
            "bitor",
            "bool",
            "break",
            "case",
            "catch",
            "char",
            "char16_t",
            "char32_t",
            "class",
            "compl",
            "concept",
            "const",
            "constexpr",
            "const_cast",
            "continue",
            "decltype",
            "default",
            "delete",
            "do",
            "double",
            "dynamic_cast",
            "else",
            "enum",
            "explicit",
            "export",
            "extern",
            "false",
            "float",
            "for",
            "friend",
            "goto",
            "if",
            "inline",
            "int",
            "long",
            "mutable",
            "namespace",
            "new",
            "noexcept",
            "not",
            "not_eq",
            "nullptr",
            "operator",
            "or",
            "or_eq",
            "private",
            "protected",
            "public",
            "register",
            "reinterpret_cast",
            "requires",
            "return",
            "short",
            "signed",
            "sizeof",
            "static",
            "static_assert",
            "static_cast",
            "struct",
            "switch",
            "template",
            "this",
            "thread_local",
            "throw",
            "true",
            "try",
            "typedef",
            "typeid",
            "typename",
            "union",
            "unsigned",
            "using",
            "virtual",
            "void",
            "volatile",
            "wchar_t",
            "while",
            "xor",
            "xor_eq",
        ]
    )

    type_map = {
        Type.DOUBLE: "double",
        Type.FLOAT: "float",
        Type.SINT32: "int32_t",
        Type.SINT64: "int64_t",
        Type.UINT32: "uint32_t",
        Type.UINT64: "uint64_t",
        Type.BOOL: "bool",
        Type.STRING: "std::string",
        Type.BYTES: "std::string",
    }

    value_map = {"null": "NULL"}

    # <limits> spells every one of these, and names the exact type from the type map rather
    # than relying on a <cstdint> or <cmath> macro that happens to match it. max and min are
    # wrapped in parentheses because windows.h defines function-like macros of both names,
    # which would otherwise expand over the call in any header included after it.
    special_values = {
        (Type.DOUBLE, "nan"): "std::numeric_limits<double>::quiet_NaN()",
        (Type.DOUBLE, "inf"): "std::numeric_limits<double>::infinity()",
        (Type.DOUBLE, "-inf"): "-std::numeric_limits<double>::infinity()",
        (Type.DOUBLE, "max"): "(std::numeric_limits<double>::max)()",
        (Type.DOUBLE, "lowest"): "std::numeric_limits<double>::lowest()",
        (Type.FLOAT, "nan"): "std::numeric_limits<float>::quiet_NaN()",
        (Type.FLOAT, "inf"): "std::numeric_limits<float>::infinity()",
        (Type.FLOAT, "-inf"): "-std::numeric_limits<float>::infinity()",
        (Type.FLOAT, "max"): "(std::numeric_limits<float>::max)()",
        (Type.FLOAT, "lowest"): "std::numeric_limits<float>::lowest()",
        (Type.SINT32, "max"): "(std::numeric_limits<int32_t>::max)()",
        (Type.SINT32, "min"): "(std::numeric_limits<int32_t>::min)()",
        (Type.SINT64, "max"): "(std::numeric_limits<int64_t>::max)()",
        (Type.SINT64, "min"): "(std::numeric_limits<int64_t>::min)()",
        (Type.UINT32, "max"): "(std::numeric_limits<uint32_t>::max)()",
        (Type.UINT64, "max"): "(std::numeric_limits<uint64_t>::max)()",
    }

    def parse_name(self, name):
        return super().parse_name(snake_case(name))

    def parse_type(self, typ):
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType) and typ.protobuf_type.code == Type.EVENT:
            return "::krpc::Event"
        if isinstance(typ, MessageType):
            return "krpc::schema::%s" % typ.python_type.__name__
        if isinstance(typ, ListType):
            return "std::vector<%s>" % self.parse_type(typ.value_type)
        if isinstance(typ, SetType):
            return "std::set<%s>" % self.parse_type(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "std::map<%s, %s>" % (
                self.parse_type(typ.key_type),
                self.parse_type(typ.value_type),
            )
        if isinstance(typ, TupleType):
            return "std::tuple<%s>" % ", ".join(
                self.parse_type(t) for t in typ.value_types
            )
        if isinstance(typ, (ClassType, EnumerationType)):
            name = "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
            return self.shorten_ref(name).replace(".", "::")
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return '"%s"' % value
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BOOL:
            return "true" if value else "false"
        if isinstance(typ, ClassType) and value is None:
            return self.parse_type(typ) + "()"
        if isinstance(typ, EnumerationType):
            return "%s::%s" % (
                self.parse_type(typ),
                self.parse_enum_value_name(value.name),
            )
        if value is None:
            return self.parse_type(typ) + "()"
        # A collection is written as a braced initializer list. Parentheses would name a
        # constructor instead, so std::vector<int32_t>(1, 2, 3) is not the vector of those
        # three values it appears to be.
        if isinstance(typ, TupleType):
            values = (
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            )
            return "%s{%s}" % (self.parse_type(typ), ", ".join(values))
        if isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "%s{%s}" % (self.parse_type(typ), ", ".join(values))
        if isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "%s{%s}" % (self.parse_type(typ), ", ".join(values))
        if isinstance(typ, DictionaryType):
            entries = (
                "{%s, %s}"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "%s{%s}" % (self.parse_type(typ), ", ".join(entries))
        return str(value)

    def shorten_ref(self, name):
        name = name.split(".")
        if name[0] == self.module:
            del name[0]
        return ".".join(name)
