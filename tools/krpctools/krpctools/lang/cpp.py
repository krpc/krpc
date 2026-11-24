import re

from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
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
from krpc.utils import snake_case

from .language import Language, float32_literal

# A string literal, which is left as it is, or a library name to qualify
_LIBRARY_NAMES = re.compile(
    r'"(?:\\.|[^"\\])*"|(?<![\w:])(std::|krpc::|u?int(?:8|16|32|64)_t\b)'
)

# The class a class with static methods nests, which a class of the same name cannot hold. A type
# of this name takes a trailing underscore
GENERATED_TYPE_NAMES = frozenset(["Bound"])


def type_name(name):
    """The name of a class, enumeration, structure or exception of a service, with a trailing
    underscore where it is the name of a class the generated code nests"""
    return name + "_" if name in GENERATED_TYPE_NAMES else name


HELPER_SUFFIXES = ("stream", "call", "call_template")


def helper_names(name, members):
    """The helpers the generated code declares beside a member, keyed by suffix. A helper named
    as another member of its scope, such as foo_stream beside foo, takes a second underscore,
    as in foo__stream"""
    helpers = {}
    for suffix in HELPER_SUFFIXES:
        helper = "%s_%s" % (name, suffix)
        helpers[suffix] = "%s__%s" % (name, suffix) if helper in members else helper
    return helpers


def _qualify_library_name(match):
    name = match.group(1)
    if name is None:
        return match.group(0)
    if name.endswith("_t"):
        return "::std::" + name
    return "::" + name


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
    ) | set(
        [
            # The static method of a class with static methods, which binds them to a
            # connection
            "bind",
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

    # The standard exception each mapped type is thrown as
    native_exceptions = {
        "System.InvalidOperationException": "std::logic_error",
        "System.ArgumentException": "std::invalid_argument",
        "System.ArgumentNullException": "std::invalid_argument",
        "System.ArgumentOutOfRangeException": "std::out_of_range",
        "System.Collections.Generic.KeyNotFoundException": "std::out_of_range",
        "System.IndexOutOfRangeException": "std::out_of_range",
        "System.DivideByZeroException": "std::domain_error",
        "System.OverflowException": "std::overflow_error",
    }

    # The locals of a generated function, and the connection a static method takes
    parameter_reserved = frozenset(
        ["_args", "_data", "_result", "_connection", "connection"]
    )

    def __init__(self, qualified=False):
        """With qualified, every type and value is spelled from the global namespace, so that
        no name a service declares can shadow it in the generated code"""
        super().__init__()
        self.qualified = qualified

    def qualify(self, source: str) -> str:
        if not self.qualified:
            return source
        return _LIBRARY_NAMES.sub(_qualify_library_name, source)

    def native_exception(self, mapped_type: str) -> str | None:
        native = self.native_exceptions.get(mapped_type)
        return None if native is None else self.qualify(native)

    def exception_alias(self, service: str, name: str, mapped_type: str) -> str | None:
        # The server removes a stream with this exception, thrown as the client's StreamError
        if (service, name) == ("KRPC", "StreamRemovedException"):
            return "::krpc::StreamError"
        return self.native_exception(mapped_type)

    def parse_name(self, name):
        return super().parse_name(snake_case(name))

    def parse_type(self, typ):
        return self.qualify(self._parse_nullable_type(typ))

    def _parse_nullable_type(self, typ):
        """The C++ type of a value. A value that can hold null is a std::optional, at a
        slot and at a position inside another value alike, as the type is all the codec
        has to read nullability from."""
        if typ is not None and typ.nullable:
            return "std::optional<%s>" % self._parse_type(typ)
        return self._parse_type(typ)

    def _parse_type(self, typ):
        """The C++ type a value is spelled with, leaving aside whether it can hold null"""
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        if isinstance(typ, StreamType):
            return self.type_map[Type.UINT64]
        if isinstance(typ, EventType):
            return "::krpc::Event"
        if isinstance(typ, MessageType):
            return "krpc::schema::%s" % typ.python_type.__name__
        if isinstance(typ, ListType):
            return "std::vector<%s>" % self._parse_nullable_type(typ.value_type)
        if isinstance(typ, SetType):
            return "std::set<%s>" % self._parse_nullable_type(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "std::map<%s, %s>" % (
                self._parse_nullable_type(typ.key_type),
                self._parse_nullable_type(typ.value_type),
            )
        if isinstance(typ, TupleType):
            return "std::tuple<%s>" % ", ".join(
                self._parse_nullable_type(t) for t in typ.value_types
            )
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            name = type_name(typ.protobuf_type.name)
            if self.qualified:
                return "krpc::services::%s::%s" % (typ.protobuf_type.service, name)
            name = "%s.%s" % (typ.protobuf_type.service, name)
            return self.shorten_ref(name).replace(".", "::")
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def parse_default_value(self, value, typ) -> str:
        return self.qualify(self._parse_default_value(value, typ))

    def _parse_default_value(self, value, typ):
        special = self.parse_special_value(value, typ)
        if special is not None:
            return special
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return '"%s"' % value
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BOOL:
            return "true" if value else "false"
        # Bytes are a std::string, given its length as they may hold a zero byte
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BYTES:
            escapes = "".join("\\x%02x" % x for x in value)
            return 'std::string("%s", %d)' % (escapes, len(value))
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.FLOAT:
            return float32_literal(value) + "f"
        # A default of null is the default constructed value, which for a nullable position
        # is the empty std::optional it is declared with
        if value is None:
            return self.parse_type(typ) + "()"
        if isinstance(typ, EnumerationType):
            return " | ".join(
                "%s::%s" % (self._parse_type(typ), self.parse_enum_value_name(name))
                for name in self.enum_value_names(value)
            )
        # A collection is written as a braced initializer list. Parentheses would name a
        # constructor instead, so std::vector<int32_t>(1, 2, 3) is not the vector of those
        # three values it appears to be.
        if isinstance(typ, StructType):
            values = (
                self.parse_default_value(x, typ.field_types[i])
                for i, x in enumerate(value)
            )
            return "%s{%s}" % (self._parse_type(typ), ", ".join(values))
        if isinstance(typ, TupleType):
            values = (
                self.parse_default_value(x, typ.value_types[i])
                for i, x in enumerate(value)
            )
            return "%s{%s}" % (self._parse_type(typ), ", ".join(values))
        if isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "%s{%s}" % (self._parse_type(typ), ", ".join(values))
        if isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return "%s{%s}" % (self._parse_type(typ), ", ".join(values))
        if isinstance(typ, DictionaryType):
            entries = (
                "{%s, %s}"
                % (
                    self.parse_default_value(k, typ.key_type),
                    self.parse_default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "%s{%s}" % (self._parse_type(typ), ", ".join(entries))
        return str(value)

    def shorten_ref(self, name):
        name = name.split(".")
        if name[0] == self.module:
            del name[0]
        return ".".join(name)
