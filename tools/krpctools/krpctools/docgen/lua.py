from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue


class LuaDomain(Domain):
    name = 'lua'
    prettyname = 'Lua'
    sphinxname = 'lua'
    highlight = 'lua'
    codeext = 'lua'

    value_map = {
        'null': 'nil',
        'true': 'True',
        'false': 'False'
    }

    type_map = {
        Type.DOUBLE: 'number',
        Type.FLOAT: 'number',
        Type.SINT32: 'number',
        Type.SINT64: 'number',
        Type.UINT32: 'number',
        Type.UINT64: 'number',
        Type.BOOL: 'boolean',
        Type.STRING: 'string',
        Type.BYTES: 'string'
    }

    def currentmodule(self, name):
        super(LuaDomain, self).currentmodule(name)
        return '.. currentmodule:: %s' % name

    def type(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                '%s.%s' % (typ.protobuf_type.service, typ.protobuf_type.name))
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
            return self.type_map[typ.protobuf_type.code]
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
