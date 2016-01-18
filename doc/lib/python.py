from .nodes import *
from .utils import snakecase
from krpc.types import ValueType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class PythonDomain(object):
    name = 'python'
    prettyname = 'Python'
    sphinxname = 'py'
    codeext = 'py'

    _value_map = {
        'null': 'None',
        'true': 'True',
        'false': 'False'
    }

    def __init__(self, args):
        self._currentmodule = None
        self.macros = args.python_macros

    def currentmodule(self, name):
        self._currentmodule = name
        return '.. currentmodule:: %s' % name

    def type(self, typ):
        if isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1])
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1])
        elif isinstance(typ, ListType):
            return 'list'
        elif isinstance(typ, DictionaryType):
            return 'dict'
        elif isinstance(typ, SetType):
            return 'set'
        elif isinstance(typ, TupleType):
            return 'tuple'
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def return_type(self, typ):
        return self.type(typ)

    def parameter_type(self, typ):
        return self.type(typ)

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'list of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'dict from %s to %s' % (self.type_description(typ.key_type), self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'set of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'tuple of (%s)' % ', '.join(self.type_description(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def value(self, value):
        return self._value_map.get(value, value)

    def ref(self, obj):
        name = obj.fullname
        if isinstance(obj, Procedure) or isinstance(obj, Property) or \
           isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod) or isinstance(obj, ClassProperty) or \
           isinstance(obj, EnumerationValue):
            name = name.split('.')
            name[-1] = snakecase(name[-1])
            name = '.'.join(name)
        return self.shorten_ref(name)

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
        return '*%s*' % snakecase(name)

    def code(self, value):
        return '``%s``' % self.value(value)

    def math(self, value):
        return ':math:`%s`' % value
