from .nodes import *
from krpc.types import ValueType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class CsharpDomain(object):
    name = 'csharp'
    prettyname = 'C#'
    sphinxname = 'cs'
    codeext = 'cs'

    def __init__(self, args):
        self._currentmodule = None
        self.macros = args.csharp_macros

    def currentmodule(self, name):
        self._currentmodule = name
        return '' #return '.. currentmodule:: %s' % name

    _type_map = {
        'int32': 'int',
        'int64': 'Int64',
        'uint32': 'UInt32',
        'uint64': 'UInt64',
        'bytes': 'byte[]',
        'string': 'string',
        'float': 'float',
        'double': 'double',
        'bool': 'bool'
    }

    def type(self, typ):
        if isinstance(typ, ValueType):
            return self._type_map[typ.protobuf_type]
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1])
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1])
        elif isinstance(typ, ListType):
            return 'IList'
        elif isinstance(typ, DictionaryType):
            return 'IDictionary'
        elif isinstance(typ, SetType):
            return 'ISet'
        elif isinstance(typ, TupleType):
            return 'Tuple'
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def return_type(self, typ):
        return self.type(typ)

    def parameter_type(self, typ):
        return self.type(typ)

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.type(typ)
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'IList<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'IDictionary<%s, %s>' % (self.type_description(typ.key_type), self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'ISet<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'Tuple<%s>' % ','.join(self.type_description(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def value(self, value):
        return value

    def ref(self, obj):
        return self.shorten_ref(obj.fullname)

    def shorten_ref(self, name):
        name = name.split('.')
        if name[0] == self._currentmodule:
            del name[0]
        return '.'.join(name)

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
        return '*%s*' % name

    def code(self, value):
        return '``%s``' % self.value(value)

    def math(self, value):
        return ':math:`%s`' % value
