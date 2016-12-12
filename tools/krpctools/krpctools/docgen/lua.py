from krpc.utils import snake_case
from krpc.types import ValueType, MessageType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType
from .domain import Domain
from .nodes import Procedure, Property, Class, ClassMethod, ClassStaticMethod, ClassProperty
from .nodes import Enumeration, EnumerationValue

class LuaDomain(Domain):
    name = 'lua'
    prettyname = 'Lua'
    sphinxname = 'lua'
    codeext = 'lua'

    value_map = {
        'null': 'nil',
        'true': 'True',
        'false': 'False'
    }

    type_map = {
        'double': 'number',
        'float': 'number',
        'int32': 'number',
        'int64': 'number',
        'uint32': 'number',
        'uint64': 'number',
        'bool': 'boolean',
        'string': 'string',
        'bytes': 'string'
    }

    def __init__(self, macros):
        super(LuaDomain, self).__init__(macros)

    def currentmodule(self, name):
        super(LuaDomain, self).currentmodule(name)
        return '.. currentmodule:: %s' % name

    def type(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type]
        elif isinstance(typ, MessageType):
            return 'krpc.schema.%s' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1])
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1])
        elif isinstance(typ, ListType):
            return 'List'
        elif isinstance(typ, DictionaryType):
            return 'Map'
        elif isinstance(typ, SetType):
            return 'Set'
        elif isinstance(typ, TupleType):
            return 'Tuple'
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type]
        elif isinstance(typ, MessageType):
            return ':class:`%s`' % 'krpc.schema.%s' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'List of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'Map from %s to %s' % (self.type_description(typ.key_type),
                                          self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'Set of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'Tuple of (%s)' % ', '.join(self.type_description(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if any(isinstance(obj, cls) for cls in
               (Procedure, Property, ClassMethod, ClassStaticMethod, ClassProperty, EnumerationValue)):
            name = name.split('.')
            name[-1] = snake_case(name[-1])
            name = '.'.join(name)
        return self.shorten_ref(name)

    # FIXME: reference shortening does not work with sphinx-lua
    def shorten_ref(self, name, obj=None):
        return name

    def see(self, obj):
        if isinstance(obj, Property) or isinstance(obj, ClassProperty) or isinstance(obj, EnumerationValue):
            prefix = 'attr'
        elif isinstance(obj, Procedure) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod):
            prefix = 'meth'
        elif isinstance(obj, Class) or isinstance(obj, Enumeration):
            prefix = 'class'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def paramref(self, name):
        return super(LuaDomain, self).paramref(snake_case(name))
