from krpc.docgen.domain import Domain
from krpc.docgen.nodes import *
from krpc.docgen.utils import snakecase
from krpc.types import ValueType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class CppDomain(Domain):
    name = 'cpp'
    prettyname = 'C++'
    sphinxname = 'cpp'
    codeext = 'cpp'

    type_map = {
        'string': 'std::string',
        'bytes': 'std::string',
    }

    value_map = {
        'null': 'NULL'
    }

    def __init__(self, macros):
        super(CppDomain, self).__init__(macros)

    def type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map.get(typ.protobuf_type, typ.protobuf_type)
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1]).replace('.', '::')
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1]).replace('.', '::')
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % (self.type(typ.key_type), self.type(typ.value_type))
        elif isinstance(typ, SetType):
            return 'std::set<%s>' % self.type(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % ', '.join(self.type(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def type_description(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % (self.type_description(typ.key_type), self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'std::set<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % ', '.join(self.type_description(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if isinstance(obj, Procedure) or isinstance(obj, Property) or \
           isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod) or isinstance(obj, ClassProperty) or \
           isinstance(obj, EnumerationValue):
            name = name.split('.')
            name[-1] = snakecase(name[-1])
            name = '.'.join(name)
        return self.shorten_ref(name).replace('.', '::')

    def see(self, obj):
        if isinstance(obj, Property) or isinstance(obj, ClassProperty):
            prefix = 'func'
        elif isinstance(obj, Procedure) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod):
            prefix = 'func'
        elif isinstance(obj, Class):
            prefix = 'class'
        elif isinstance(obj, Enumeration):
            prefix = 'enum'
        elif isinstance(obj, EnumerationValue):
            prefix = 'enumerator'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def paramref(self, name):
        return super(CppDomain, self).paramref(snakecase(name))
