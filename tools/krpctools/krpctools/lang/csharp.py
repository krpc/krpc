from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .language import Language


class CsharpLanguage(Language):

    keywords = set([
        'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch',
        'char', 'checked', 'class', 'const', 'continue', 'decimal', 'default',
        'delegate', 'do', 'double', 'else', 'enum', 'event', 'explicit',
        'extern', 'false', 'finally', 'fixed', 'float', 'for', 'foreach',
        'goto', 'if', 'implicit', 'in', 'int', 'interface', 'internal', 'is',
        'lock', 'long', 'namespace', 'new', 'null', 'object', 'operator',
        'out', 'override', 'params', 'private', 'protected', 'public',
        'readonly', 'ref', 'return', 'sbyte', 'sealed', 'short', 'sizeof',
        'stackalloc', 'static', 'string', 'struct', 'switch', 'this', 'throw',
        'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked', 'unsafe',
        'ushort', 'using', 'virtual', 'void', 'volatile', 'while'
    ])

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

    def parse_type(self, typ):
        return self._parse_type(typ)

    def _parse_type(self, typ, interface=True):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif (isinstance(typ, MessageType) and
              typ.protobuf_type.code == Type.EVENT):
            return 'global::KRPC.Client.Event'
        elif isinstance(typ, MessageType):
            return 'global::KRPC.Schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, TupleType):
            return 'systemAlias::Tuple<%s>' % \
                ','.join(self._parse_type(t) for t in typ.value_types)
        elif isinstance(typ, ListType):
            if interface:
                name = 'IList'
            else:
                name = 'List'
            return 'global::System.Collections.Generic.%s<%s>' % \
                (name, self._parse_type(typ.value_type))
        elif isinstance(typ, SetType):
            if interface:
                name = 'genericCollectionsAlias::ISet'
            else:
                name = 'global::System.Collections.Generic.HashSet'
            return '%s<%s>' % \
                (name, self._parse_type(typ.value_type))
        elif isinstance(typ, DictionaryType):
            if interface:
                name = 'IDictionary'
            else:
                name = 'Dictionary'
            return 'global::System.Collections.Generic.%s<%s,%s>' % \
                (name, self._parse_type(typ.key_type),
                 self._parse_type(typ.value_type))
        elif isinstance(typ, (ClassType, EnumerationType)):
            return 'global::KRPC.Client.Services.%s.%s' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def parse_default_value(self, value, typ):
        if (isinstance(typ, ValueType) and
                typ.protobuf_type.code == Type.STRING):
            return '"%s"' % value
        elif (isinstance(typ, ValueType) and
              typ.protobuf_type.code == Type.BOOL):
            return 'true' if value else 'false'
        elif (isinstance(typ, ValueType) and
              typ.protobuf_type.code == Type.FLOAT):
            return str(value) + "f"
        elif isinstance(typ, ClassType) and value is None:
            return 'null'
        elif isinstance(typ, EnumerationType):
            return '(global::KRPC.Client.Services.%s.%s)%s' % \
                (typ.protobuf_type.service, typ.protobuf_type.name, value)
        elif value is None:
            return 'null'
        elif isinstance(typ, TupleType):
            values = (self.parse_default_value(x, typ.value_types[i])
                      for i, x in enumerate(value))
            return 'new %s (%s)' % \
                (self._parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return 'new %s { %s }' % \
                (self._parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return 'new %s { %s }' % \
                (self._parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, DictionaryType):
            entries = ('{ %s, %s }' %
                       (self.parse_default_value(k, typ.key_type),
                        self.parse_default_value(v, typ.value_type))
                       for k, v in value.items())
            return 'new %s {%s}' % \
                (self._parse_type(typ, False), ', '.join(entries))
        return str(value)
