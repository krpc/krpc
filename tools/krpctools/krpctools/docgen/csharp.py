from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue


class CsharpDomain(Domain):
    name = 'csharp'
    prettyname = 'C#'
    sphinxname = 'csharp'
    highlight = 'csharp'
    codeext = 'cs'

    type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int',
        Type.SINT64: 'long',
        Type.UINT32: 'uint',
        Type.UINT64: 'ulong',
        Type.BOOL: 'bool',
        Type.STRING: 'string',
        Type.BYTES: 'byte[]'
    }

    def currentmodule(self, name):
        super(CsharpDomain, self).currentmodule(name)
        return '.. namespace:: KRPC.Client.Services.%s' % name

    def type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'KRPC.Schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                '%s.%s' % (typ.protobuf_type.service, typ.protobuf_type.name))
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
        elif isinstance(typ, (TupleType, ListType, SetType, DictionaryType)):
            return 'null'
        return str(value)

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
        return super(CsharpDomain, self).shorten_ref(name, obj)
