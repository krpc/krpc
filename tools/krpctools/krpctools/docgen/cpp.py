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
from .domain import Domain
from .nodes import (
    Procedure,
    Property,
    Class,
    ClassMethod,
    ClassStaticMethod,
    ClassProperty,
    Enumeration,
    EnumerationValue,
)
from ..lang.cpp import CppLanguage


class CppDomain(Domain):
    name = "cpp"
    prettyname = "C++"
    sphinxname = "cpp"
    highlight = "cpp"
    codeext = "cpp"
    language = CppLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. namespace:: krpc::services::%s" % name

    def method_name(self, name):
        if snake_case(name) in self.language.keywords:
            return "%s_" % name
        return name

    def enumeration_name(self, name):
        if snake_case(name) in self.language.keywords:
            return "%s_" % name
        return name

    def type_description(self, typ):
        if typ is None:
            return "void"
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return ":class:`krpc::schema::%s`" % typ.python_type.__name__
        if isinstance(typ, ClassType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, EnumerationType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, ListType):
            return "std::vector<%s>" % self.type_description(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "std::map<%s, %s>" % (
                self.type_description(typ.key_type),
                self.type_description(typ.value_type),
            )
        if isinstance(typ, SetType):
            return "std::set<%s>" % self.type_description(typ.value_type)
        if isinstance(typ, TupleType):
            return "std::tuple<%s>" % ", ".join(
                self.type_description(typ) for typ in typ.value_types
            )
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def ref(self, obj):
        name = obj.fullname.split(".")
        if any(
            isinstance(obj, cls)
            for cls in (
                Procedure,
                Property,
                ClassMethod,
                ClassStaticMethod,
                ClassProperty,
                EnumerationValue,
            )
        ):
            name[-1] = snake_case(name[-1])
        if isinstance(obj, (Property, ClassProperty)) and obj.getter is None:
            name[-1] = "set_" + name[-1]
        return self.shorten_ref(".".join(name)).replace(".", "::")

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty)):
            prefix = "func"
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = "func"
        elif isinstance(obj, Class):
            prefix = "class"
        elif isinstance(obj, Enumeration):
            prefix = "enum"
        elif isinstance(obj, EnumerationValue):
            prefix = "enumerator"
        else:
            raise RuntimeError(str(obj))
        return ":%s:`%s`" % (prefix, self.ref(obj))

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
