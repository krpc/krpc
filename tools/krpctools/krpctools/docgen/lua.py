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
from krpc.utils import snake_case

from ..lang.lua import LuaLanguage
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
    Struct,
    StructField,
)


class LuaDomain(Domain[LuaLanguage]):
    name = "lua"
    prettyname = "Lua"
    sphinxname = "lua"
    highlight = "lua"
    codeext = "lua"
    language = LuaLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. currentmodule:: %s" % name

    def type_description(self, typ):
        description = self._type_description(typ)
        # Lua writes a value that can be null as the type or Types.none
        return "%s or ``Types.none``" % description if typ.nullable else description

    def _type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return ":class:`krpc.schema.%s`" % typ.python_type.__name__
        if isinstance(typ, ClassType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, EnumerationType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, StructType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, ListType):
            return "List"
        if isinstance(typ, DictionaryType):
            return "Map"
        if isinstance(typ, SetType):
            return "Set"
        if isinstance(typ, TupleType):
            return "Tuple"
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if any(
            isinstance(obj, cls)
            for cls in (
                Procedure,
                Property,
                ClassMethod,
                ClassStaticMethod,
                ClassProperty,
                EnumerationValue,
                StructField,
            )
        ):
            name = name.split(".")
            name[-1] = snake_case(name[-1])
            name = ".".join(name)
        return self.shorten_ref(name)

    # sphinx-lua cannot resolve references written in the shortened, module-relative form the other
    # domains use, so Lua references are left fully qualified.
    def shorten_ref(self, name, obj=None):
        return name

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty, EnumerationValue, StructField)):
            prefix = "attr"
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = "meth"
        elif isinstance(obj, (Class, Enumeration, Struct, ExceptionNode)):
            prefix = "class"
        else:
            raise RuntimeError(str(obj))
        if isinstance(obj, Property):
            # Service properties are registered with the service name doubled
            # (Service.Service.name, unlike service procedures which register
            # as Service.name), so the short form only resolves on the
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

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
