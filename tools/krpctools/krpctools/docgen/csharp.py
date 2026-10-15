from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ClassType,
    DictionaryType,
    EnumerationType,
    ListType,
    MessageType,
    SetType,
    StructType,
    TupleType,
    ValueType,
)

from ..lang.csharp import CsharpLanguage
from .domain import Domain
from .nodes import (
    Class,
    ClassMethod,
    ClassProperty,
    ClassStaticMethod,
    Enumeration,
    EnumerationValue,
    Procedure,
    Property,
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

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. namespace:: KRPC.Client.Services.%s" % name

    def type(self, typ):
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType) and typ.protobuf_type.code == Type.EVENT:
            return "KRPC.Client.Event"
        if isinstance(typ, MessageType):
            return "KRPC.Schema.KRPC.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return self.shorten_ref(
                "%s.%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
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
            return "%s.%s" % (
                self.type(typ),
                self.language.parse_enum_value_name(value.name),
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
        if isinstance(obj, (Property, ClassProperty)):
            prefix = "prop"
        elif isinstance(obj, EnumerationValue):
            prefix = "value"
        elif isinstance(obj, StructField):
            prefix = "prop"
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = "meth"
        elif isinstance(obj, (Class, Enumeration, Struct)):
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
                obj.fullname,
            )
        return ":%s:`%s`" % (prefix, self.ref(obj))

    def shorten_ref(self, name, obj=None):
        # Only drop service name for non-service members
        if obj and isinstance(obj, (Procedure, Property)):
            return name
        return super().shorten_ref(name, obj)
