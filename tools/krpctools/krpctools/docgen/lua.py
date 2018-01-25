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
        super(LuaDomain, self).currentmodule(name)
        return '.. currentmodule:: %s' % name

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return ':class:`krpc.schema.KRPC.%s`' % typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'List of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'Map from %s to %s' % \
                (self.type_description(typ.key_type),
                 self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'Set of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'Tuple of (%s)' % \
                ', '.join(self.type_description(typ)
                          for typ in typ.value_types)
        else:
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

    def paramref(self, name):
        return super(LuaDomain, self).paramref(snake_case(name))
