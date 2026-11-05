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
    Service,
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

    def type_at_position(self, typ):
        # Lua writes a value that can be null as the type or Types.none
        name = self.type(typ)
        return "%s | Types.none" % name if typ.nullable else name

    def return_type(self, typ):
        # A null result is nil
        if typ is not None and typ.nullable:
            return "%s | nil" % self.type(typ)
        return super().return_type(typ)

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

    # Lua references stay fully qualified, so a link names the service it leads to
    def shorten_ref(self, name, obj=None):
        return name

    def see(self, obj):
        if isinstance(obj, Service):
            return ":mod:`%s`" % obj.name
        if isinstance(obj, (Property, ClassProperty, StructField)):
            prefix = "attr"
        elif isinstance(obj, EnumerationValue):
            prefix = "data"
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = "meth"
        elif isinstance(obj, (Class, Enumeration, Struct, ExceptionNode)):
            prefix = "class"
        else:
            raise RuntimeError(str(obj))
        return ":%s:`%s`" % (prefix, self.ref(obj))

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
