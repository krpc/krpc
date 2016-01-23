from .python import PythonDomain
from .nodes import *
from .utils import snakecase
from krpc.types import ValueType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class CppDomain(PythonDomain):
    name = 'cpp'
    prettyname = 'C++'
    sphinxname = 'cpp'
    codeext = 'cpp'

    _type_map = {
        'string': 'std::string',
        'bytes': 'std::string',
    }

    _value_map = {
        'null': 'NULL'
    }

    def __init__(self, args):
        self._currentmodule = None
        self.macros = args.cpp_macros

    def currentmodule(self, name):
        self._currentmodule = name
        return ''

    def type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self._type_map.get(typ.protobuf_type, typ.protobuf_type)
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1]).replace('.', '::')
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1]).replace('.', '::')
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % (self.type(typ.key_type), self.type(typ.value_type))
        elif isinstance(typ, SetType):
            raise RuntimeError('not supported')
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % ', '.join(self.type(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def typedesc(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.typedesc(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % (self.typedesc(typ.key_type), self.typedesc(typ.value_type))
        elif isinstance(typ, SetType):
            raise RuntimeError('not supported')
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % ', '.join(self.typedesc(typ) for typ in typ.value_types)
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

    def shorten_ref(self, name):
        return name

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
