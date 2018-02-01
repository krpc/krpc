from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, ClassProperty
from .nodes import Enumeration, EnumerationValue
from ..lang.python import PythonLanguage


class PythonDomain(Domain):
    name = 'python'
    prettyname = 'Python'
    sphinxname = 'py'
    highlight = 'py'
    codeext = 'py'
    language = PythonLanguage()

    def currentmodule(self, name):
        super(PythonDomain, self).currentmodule(name)
        return '.. currentmodule:: %s' % name

    def method_name(self, name):
        name = snake_case(name)
        if name in self.language.keywords:
            return '%s_' % name
        return name

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.language.parse_type(typ)
        elif isinstance(typ, MessageType):
            return ':class:`krpc.schema.KRPC.%s`' % typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'list(%s)' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'dict(%s, %s)' % \
                (self.type_description(typ.key_type),
                 self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'set(%s)' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'tuple(%s)' % \
                ', '.join(self.type_description(typ)
                          for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if any(isinstance(obj, cls) for cls in
               (Procedure, Property, ClassMethod,
                ClassStaticMethod, ClassProperty, EnumerationValue)):
            name = name.split('.')
            name[-1] = snake_case(name[-1])
            name = '.'.join(name)
        return self.shorten_ref(name)

    def see(self, obj):
        if any(isinstance(obj, cls)
               for cls in (Property, ClassProperty, EnumerationValue)):
            prefix = 'attr'
        elif any(isinstance(obj, cls)
                 for cls in (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = 'meth'
        elif any(isinstance(obj, cls) for cls in (Class, Enumeration)):
            prefix = 'class'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def paramref(self, name):
        return super(PythonDomain, self).paramref(snake_case(name))
