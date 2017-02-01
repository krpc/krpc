from krpc.types import \
    ValueType, MessageType, ClassType, EnumType, ListType, DictionaryType, \
    SetType, TupleType
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue


class CsharpDomain(Domain):
    name = 'csharp'
    prettyname = 'C#'
    sphinxname = 'csharp'
    codeext = 'cs'

    type_map = {
        'int32': 'int',
        'int64': 'long',
        'uint32': 'uint',
        'uint64': 'ulong',
        'bytes': 'byte[]',
        'string': 'string',
        'float': 'float',
        'double': 'double',
        'bool': 'bool'
    }

    def __init__(self, macros):
        super(CsharpDomain, self).__init__(macros)

    def currentmodule(self, name):
        super(CsharpDomain, self).currentmodule(name)
        return '.. namespace:: KRPC.Client.Services.%s' % name

    def type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type]
        elif isinstance(typ, MessageType):
            return 'KRPC.Schema.%s' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1])
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1])
        elif isinstance(typ, ListType):
            return 'System.Collections.Generic.IList<%s>' % \
                self.type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'System.Collections.Generic.IDictionary<%s,%s>' % \
                (self.type(typ.key_type), self.type(typ.value_type))
        elif isinstance(typ, SetType):
            return 'System.Collections.Generic.ISet<%s>' \
                % self.type(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'System.Tuple<%s>' % \
                ','.join(self.type(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    @staticmethod
    def default_value(typ, value):
        if value is None:
            return 'null'
        elif (isinstance(typ, TupleType) or isinstance(typ, ListType) or
              isinstance(typ, SetType) or isinstance(typ, DictionaryType)):
            return 'null'
        else:
            return str(value)

    def see(self, obj):
        if isinstance(obj, Property) or isinstance(obj, ClassProperty):
            prefix = 'prop'
        elif isinstance(obj, EnumerationValue):
            prefix = 'enum'
        elif (isinstance(obj, Procedure) or
              isinstance(obj, ClassMethod) or
              isinstance(obj, ClassStaticMethod)):
            prefix = 'meth'
        elif isinstance(obj, Class) or isinstance(obj, Enumeration):
            prefix = 'type'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))

    def shorten_ref(self, name, obj=None):
        # Only drop service name for non-service members
        if obj and (isinstance(obj, Procedure) or isinstance(obj, Property)):
            return name
        return super(CsharpDomain, self).shorten_ref(name, obj)
