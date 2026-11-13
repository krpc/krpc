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

from ..lang.js import JsLanguage, accessor_name
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


class JsDomain(Domain[JsLanguage]):
    name = "js"
    prettyname = "JavaScript"
    sphinxname = "js"
    highlight = "js"
    codeext = "js"
    language = JsLanguage()

    def raised_as(self, exception):
        alias = self.language.exception_alias(
            exception.service_name, exception.name, exception.mapped_type
        )
        if alias is not None:
            return "An alias of :class:`~@krpc-org/krpc.%s`." % alias
        native = self.language.native_exception(exception.mapped_type)
        if native is not None:
            return "A subclass of the builtin ``%s`` alone." % native
        return "A subclass of :class:`~@krpc-org/krpc.ServerError`."

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. module:: %s\n   :no-index:" % name

    @staticmethod
    def method_name(name):
        return lower_camel_case(name)

    @staticmethod
    def accessors(prop):
        """The signatures of the getter and setter methods of a property"""
        signatures = []
        if prop.getter is not None:
            signatures.append("%s()" % accessor_name("get", prop.name))
        if prop.setter is not None:
            signatures.append("%s(value)" % accessor_name("set", prop.name))
        return signatures

    def type_description(self, typ):
        """A type as text holding at least one literal or reference, so that Sphinx renders it
        as written, rather than looking it up as the name of a type"""
        description = self._type_description(typ, False)
        return "%s or ``null``" % description if typ.nullable else description

    def parameter_type(self, typ):
        description = self._type_description(typ, True)
        return "%s or ``null``" % description if typ.nullable else description

    def _type_description(self, typ, argument):
        def item(x):
            description = self._type_description(x, argument)
            return "%s or ``null``" % description if x.nullable else description

        if isinstance(typ, ValueType):
            if typ.protobuf_type.code in (Type.SINT64, Type.UINT64) and argument:
                return "``number`` or ``bigint``"
            return "``%s``" % self.language.parse_type(typ)
        if isinstance(typ, EventType):
            return ":class:`~@krpc-org/krpc.Event`"
        if isinstance(typ, StreamType):
            return "``never``"
        if isinstance(typ, MessageType):
            return "``schema.%s``" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, ListType):
            return "``Array`` of %s" % item(typ.value_type)
        if isinstance(typ, TupleType):
            return "``[%s]``" % ", ".join(
                self._plain(x, argument) for x in typ.value_types
            )
        if isinstance(typ, SetType):
            return "``Set`` of %s" % item(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "``Map`` from %s to %s" % (item(typ.key_type), item(typ.value_type))
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def _plain(self, typ, argument):
        """A type inside a literal, which holds no references"""
        if isinstance(typ, ValueType):
            if typ.protobuf_type.code in (Type.SINT64, Type.UINT64) and argument:
                name = "number | bigint"
            else:
                name = self.language.parse_type(typ)
        elif isinstance(typ, (ClassType, EnumerationType, StructType)):
            name = self.type(typ)
        elif isinstance(typ, ListType):
            name = "%s[]" % self._plain(typ.value_type, argument)
        elif isinstance(typ, TupleType):
            name = "[%s]" % ", ".join(self._plain(x, argument) for x in typ.value_types)
        elif isinstance(typ, SetType):
            name = "Set<%s>" % self._plain(typ.value_type, argument)
        elif isinstance(typ, DictionaryType):
            name = "Map<%s, %s>" % (
                self._plain(typ.key_type, argument),
                self._plain(typ.value_type, argument),
            )
        else:
            name = self.language.parse_type(typ)
        return "%s | null" % name if typ.nullable else name

    def ref(self, obj):
        name = obj.fullname.split(".")
        if isinstance(obj, (Property, ClassProperty)):
            prefix = "get" if obj.getter is not None else "set"
            name[-1] = accessor_name(prefix, name[-1])
        elif isinstance(
            obj,
            (Procedure, ClassMethod, ClassStaticMethod, EnumerationValue, StructField),
        ):
            name[-1] = lower_camel_case(name[-1])
        return ".".join(name)

    def see(self, obj):
        if isinstance(obj, Service):
            return ":mod:`%s`" % obj.name
        if isinstance(obj, (Procedure, Property)):
            prefix = "func"
        elif isinstance(obj, (ClassMethod, ClassStaticMethod, ClassProperty)):
            prefix = "meth"
        elif isinstance(obj, (EnumerationValue, StructField)):
            prefix = "attr"
        elif isinstance(obj, (Class, Enumeration, Struct, ExceptionNode)):
            prefix = "class"
        else:
            raise RuntimeError(str(obj))
        return ":%s:`%s`" % (prefix, self.ref(obj))

    @staticmethod
    def paramref(name):
        return Domain.paramref(lower_camel_case(name))
