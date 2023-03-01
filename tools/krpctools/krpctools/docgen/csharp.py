from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue
from ..lang.csharp import CsharpLanguage


class CsharpDomain(Domain):
    name = 'csharp'
    prettyname = 'C#'
    sphinxname = 'csharp'
    highlight = 'csharp'
    codeext = 'cs'
    language = CsharpLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return '.. namespace:: KRPC.Client.Services.%s' % name

    def type(self, typ):
        if typ is None:
            return 'void'
        if isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return 'KRPC.Schema.KRPC.%s' % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                '%s.%s' % (typ.protobuf_type.service, typ.protobuf_type.name))
        if isinstance(typ, ListType):
            return 'System.Collections.Generic.IList<%s>' % \
                self.type(typ.value_type)
        if isinstance(typ, DictionaryType):
            return 'System.Collections.Generic.IDictionary<%s,%s>' % \
                (self.type(typ.key_type), self.type(typ.value_type))
        if isinstance(typ, SetType):
            return 'System.Collections.Generic.ISet<%s>' \
                % self.type(typ.value_type)
        if isinstance(typ, TupleType):
            return 'System.Tuple<%s>' % \
                ','.join(self.type(typ) for typ in typ.value_types)
        raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def default_value(self, value, typ):
        if isinstance(typ, EnumerationType):
            return '%s' % value
        if isinstance(typ, TupleType):
            if value is None:
                value = tuple()
            values = (self.default_value(x, typ.value_types[i])
                      for i, x in enumerate(value))
            return '{ %s }' % ', '.join(values)
        if isinstance(typ, ListType):
            if value is None:
                value = []
            values = (self.default_value(x, typ.value_type)
                      for x in value)
            return '{ %s }' % ', '.join(values)
        if isinstance(typ, SetType):
            if value is None:
                value = set()
            values = (self.default_value(x, typ.value_type)
                      for x in value)
            return '{ %s }' % ', '.join(values)
        if isinstance(typ, DictionaryType):
            if value is None:
                value = {}
            entries = ('%s: %s' %
                       (self.default_value(k, typ.key_type),
                        self.default_value(v, typ.value_type))
                       for k, v in value.items())
            return '{ %s }' % ', '.join(entries)
        return self.language.parse_default_value(value, typ)

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty)):
            prefix = 'prop'
        elif isinstance(obj, EnumerationValue):
            prefix = 'enum'
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = 'meth'
        elif isinstance(obj, (Class, Enumeration)):
            prefix = 'type'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def shorten_ref(self, name, obj=None):
        # Only drop service name for non-service members
        if obj and isinstance(obj, (Procedure, Property)):
            return name
        return super().shorten_ref(name, obj)
