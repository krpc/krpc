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
from krpc.utils import lower_camel_case

from ..lang.csharp import CsharpLanguage, type_name
from .domain import Domain
from .nodes import (
    Class,
    ClassMethod,
    ClassProperty,
    ClassStaticMethod,
    Enumeration,
    EnumerationValue,
    ExceptionNode,
    Procedure,
    Property,
    Service,
    Struct,
    StructField,
)


def _initializer(values):
    """A collection initializer holding the given values"""
    values = ", ".join(values)
    return "{ %s }" % values if values else "{ }"


class CsharpDomain(Domain[CsharpLanguage]):
    name = "csharp"
    prettyname = "C#"
    sphinxname = "cs"
    highlight = "csharp"
    codeext = "cs"
    language = CsharpLanguage()

    def raised_as(self, exception):
        typ = self.language.exception_type(
            exception.service_name, exception.name, exception.mapped_type
        )
        if typ is not None:
            return (
                "The client throws :type:`%s` for this exception, and has no class for it."
                % (typ.removeprefix("global::"))
            )
        return "A subclass of :type:`KRPC.Client.ServerException`."

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. namespace:: KRPC.Client.Services.%s" % name

    def type(self, typ):
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, StreamType):
            return self.language.type_map[Type.UINT64]
        if isinstance(typ, EventType):
            return "KRPC.Client.Event"
        if isinstance(typ, MessageType):
            return "KRPC.Schema.KRPC.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return self.shorten_ref(
                "%s.%s" % (typ.protobuf_type.service, type_name(typ.protobuf_type.name))
            )
        if isinstance(typ, ListType):
            return "System.Collections.Generic.IList<%s>" % self.type_at_position(
                typ.value_type
            )
        if isinstance(typ, DictionaryType):
            return "System.Collections.Generic.IDictionary<%s,%s>" % (
                self.type(typ.key_type),
                self.type_at_position(typ.value_type),
            )
        if isinstance(typ, SetType):
            return "System.Collections.Generic.ISet<%s>" % self.type(typ.value_type)
        if isinstance(typ, TupleType):
            return "System.Tuple<%s>" % ",".join(
                self.type_at_position(t) for t in typ.value_types
            )
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def type_at_position(self, typ):
        # The docs name a type without its namespace alias, so the nullable form is built on
        # the name this domain gives rather than the one the client is generated with
        name = self.type(typ)
        if typ.nullable and self.language.takes_the_nullable_form(typ):
            return name + "?"
        return name

    def default_parameter_type(self, typ):
        """The type of a parameter that has a default value"""
        name = self.parameter_type(typ)
        if (
            isinstance(typ, StructType)
            and self.language.default_is_null(typ)
            and not name.endswith("?")
        ):
            return name + "?"
        return name

    def signature_default_value(self, value, typ):
        """The default value of a parameter, as the stub declares it"""
        if self.language.default_is_null(typ):
            return "null"
        return self.default_value(value, typ)

    def default_is_null(self, typ):
        return self.language.default_is_null(typ)

    def default_value(self, value, typ):
        if isinstance(typ, StructType):
            values = (
                self.default_value(x, typ.field_types[i]) for i, x in enumerate(value)
            )
            return "new %s(%s)" % (self.type(typ), ", ".join(values))
        if isinstance(typ, EnumerationType):
            return " | ".join(
                "%s.%s" % (self.type(typ), self.language.parse_enum_value_name(name))
                for name in self.language.enum_value_names(value)
            )
        if isinstance(typ, TupleType):
            if value is None:
                value = tuple()
            values = (
                self.default_value(x, typ.value_types[i]) for i, x in enumerate(value)
            )
            return "new %s(%s)" % (self.type(typ), ", ".join(values))
        if isinstance(typ, ListType):
            if value is None:
                value = []
            values = (self.default_value(x, typ.value_type) for x in value)
            return "new System.Collections.Generic.List<%s> %s" % (
                self.type(typ.value_type),
                _initializer(values),
            )
        if isinstance(typ, SetType):
            if value is None:
                value = set()
            values = (self.default_value(x, typ.value_type) for x in value)
            return "new System.Collections.Generic.HashSet<%s> %s" % (
                self.type(typ.value_type),
                _initializer(values),
            )
        if isinstance(typ, DictionaryType):
            if value is None:
                value = {}
            entries = (
                "{ %s, %s }"
                % (
                    self.default_value(k, typ.key_type),
                    self.default_value(v, typ.value_type),
                )
                for k, v in value.items()
            )
            return "new System.Collections.Generic.Dictionary<%s,%s> %s" % (
                self.type(typ.key_type),
                self.type(typ.value_type),
                _initializer(entries),
            )
        return self.language.parse_default_value(value, typ)

    def see(self, obj):
        # A service is documented as a class in its own namespace
        if isinstance(obj, Service):
            return ":type:`%s <%s.%s>`" % (obj.name, obj.name, obj.name)
        if isinstance(obj, (Property, ClassProperty)):
            prefix = "prop"
        elif isinstance(obj, EnumerationValue):
            prefix = "value"
        elif isinstance(obj, StructField):
            prefix = "prop"
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = "meth"
        elif isinstance(obj, (Class, Enumeration, Struct, ExceptionNode)):
            prefix = "type"
        else:
            raise RuntimeError(str(obj))
        if isinstance(obj, (Procedure, Property)):
            # Service members are documented on the service class, so their
            # reference target is Service.Service.Member. Sphinx resolves it
            # relative to the current namespace, which only matches on the
            # service's own page; give the explicit target so references from
            # other services' pages resolve too.
            service = obj.fullname.split(".")[0]
            return ":%s:`%s <%s.%s>`" % (
                prefix,
                self.ref(obj),
                service,
                self.ref(obj),
            )
        return ":%s:`%s`" % (prefix, self.ref(obj))

    def member_name(self, obj):
        """The name of a member in the generated code, escaped as a keyword or a member of the
        client's base classes is, or as a member named as the type that holds it"""
        if isinstance(obj, EnumerationValue):
            return self.language.parse_enum_value_name(obj.name)
        name = self.language.parse_name(obj.name)
        owner = "Service"
        if not isinstance(obj, (Procedure, Property)):
            owner = type_name(obj.fullname.split(".")[1])
        return name + "_" if name == owner else name

    def field_parameter_name(self, field):
        """The name of the constructor parameter of a structure field"""
        return self.language.parse_name(lower_camel_case(self.member_name(field)))

    @staticmethod
    def type_name(obj):
        """The name of a class, enumeration, structure or exception in the generated code"""
        return type_name(obj.name)

    def ref(self, obj):
        parts = obj.fullname.split(".")
        # A type, and a member of one, is named as the generated code names the type
        if not isinstance(obj, (Service, Procedure, Property)):
            parts[1] = type_name(parts[1])
        if not isinstance(obj, (Service, Class, Enumeration, Struct, ExceptionNode)):
            parts[-1] = self.member_name(obj)
        return self.shorten_ref(".".join(parts), obj)

    def shorten_ref(self, name, obj=None):
        # Only drop service name for non-service members
        if obj and isinstance(obj, (Procedure, Property)):
            return name
        return super().shorten_ref(name, obj)
