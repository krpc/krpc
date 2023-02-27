from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue
from ..lang.lua import LuaLanguage


class LuaDomain(Domain):
    name = 'lua'
    prettyname = 'Lua'
    sphinxname = 'lua'
    highlight = 'lua'
    codeext = 'lua'
    language = LuaLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return '.. currentmodule:: %s' % name

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return ':class:`krpc.schema.KRPC.%s`' % typ.python_type.__name__
        if isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        if isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        if isinstance(typ, ListType):
            return 'List'
        if isinstance(typ, DictionaryType):
            return 'Map'
        if isinstance(typ, SetType):
            return 'Set'
        if isinstance(typ, TupleType):
            return 'Tuple'
        raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if any(isinstance(obj, cls) for cls in
               (Procedure, Property, ClassMethod, ClassStaticMethod,
                ClassProperty, EnumerationValue)):
            name = name.split('.')
            name[-1] = snake_case(name[-1])
            name = '.'.join(name)
        return self.shorten_ref(name)

    # FIXME: reference shortening does not work with sphinx-lua
    def shorten_ref(self, name, obj=None):
        return name

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty, EnumerationValue)):
            prefix = 'attr'
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = 'meth'
        elif isinstance(obj, (Class, Enumeration)):
            prefix = 'class'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
