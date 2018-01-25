from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue
from ..lang.cpp import CppLanguage


class CppDomain(Domain):
    name = 'cpp'
    prettyname = 'C++'
    sphinxname = 'cpp'
    highlight = 'cpp'
    codeext = 'cpp'
    language = CppLanguage()

    def currentmodule(self, name):
        super(CppDomain, self).currentmodule(name)
        return '.. namespace:: krpc::services::%s' % name

    def method_name(self, name):
        if snake_case(name) in self.language.keywords:
            return '%s_' % name
        return name

    def type_description(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return ':class:`krpc::schema::%s`' % typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s, %s>' % \
                (self.type_description(typ.key_type),
                 self.type_description(typ.value_type))
        elif isinstance(typ, SetType):
            return 'std::set<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' \
                % ', '.join(self.type_description(typ)
                            for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname.split('.')
        if any(isinstance(obj, cls) for cls in
               (Procedure, Property, ClassMethod, ClassStaticMethod,
                ClassProperty, EnumerationValue)):
            name[-1] = snake_case(name[-1])
        return self.shorten_ref('.'.join(name)).replace('.', '::')

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty)):
            prefix = 'func'
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
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
        return super(CppDomain, self).paramref(snake_case(name))

    def default_value(self, value, typ):
        if isinstance(typ, TupleType):
            values = (self.default_value(x, typ.value_types[i])
                      for i, x in enumerate(value))
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, ListType):
            values = (self.default_value(x, typ.value_type) for x in value)
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, SetType):
            values = (self.default_value(x, typ.value_type) for x in value)
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, DictionaryType):
            entries = ('(%s, %s)' % (self.default_value(k, typ.key_type),
                                     self.default_value(v, typ.value_type))
                       for k, v in value.items())
            return '(%s)' % ', '.join(entries)
        return self.language.parse_default_value(value, typ)
