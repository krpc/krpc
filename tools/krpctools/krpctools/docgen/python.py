from krpc.utils import snake_case
from krpc.types import ValueType, MessageType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType
from .domain import Domain
from .nodes import Procedure, Property, Class, ClassMethod, ClassStaticMethod, ClassProperty
from .nodes import Enumeration, EnumerationValue

class PythonDomain(Domain):
    name = 'python'
    prettyname = 'Python'
    sphinxname = 'py'
    codeext = 'py'

    value_map = {
        'null': 'None',
        'true': 'True',
        'false': 'False'
    }

    def __init__(self, macros):
        super(PythonDomain, self).__init__(macros)

    def currentmodule(self, name):
        super(PythonDomain, self).currentmodule(name)
        return '.. currentmodule:: %s' % name

    def type(self, typ):
        if isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, MessageType):
            return 'krpc.schema.%s' % typ.protobuf_type
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

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, MessageType):
            return ':class:`krpc.schema.%s`' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'list of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'dict from %s to %s' % (self.type_description(typ.key_type),
                                           self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'set of %s' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'tuple of (%s)' % ', '.join(self.type_description(typ) for typ in typ.value_types)
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

    def see(self, obj):
        if any(isinstance(obj, cls) for cls in (Property, ClassProperty, EnumerationValue)):
            prefix = 'attr'
        elif any(isinstance(obj, cls) for cls in (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = 'meth'
        elif any(isinstance(obj, cls) for cls in (Class, Enumeration)):
            prefix = 'class'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def paramref(self, name):
        return super(PythonDomain, self).paramref(snake_case(name))
