from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .language import Language


class LuaLanguage(Language):

    value_map = {
        'null': 'nil',
        'true': 'True',
        'false': 'False'
    }

    type_map = {
        Type.DOUBLE: 'number',
        Type.FLOAT: 'number',
        Type.SINT32: 'number',
        Type.SINT64: 'number',
        Type.UINT32: 'number',
        Type.UINT64: 'number',
        Type.BOOL: 'boolean',
        Type.STRING: 'string',
        Type.BYTES: 'string'
    }

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return '%s.%s' % (typ.protobuf_type.service,
                              typ.protobuf_type.name)
        elif isinstance(typ, ListType):
            return 'List'
        elif isinstance(typ, DictionaryType):
            return 'Map'
        elif isinstance(typ, SetType):
            return 'Set'
        elif isinstance(typ, TupleType):
            return 'Tuple'
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def parse_default_value(self, value, typ):
        if (isinstance(typ, ValueType) and
                typ.protobuf_type.code == Type.STRING):
            return '\'%s\'' % value
        # python2 fix: convert set to string manually
        if isinstance(typ, SetType):
            return '{'+', '.join(self.parse_default_value(x, typ.value_type)
                                 for x in value)+'}'
        return str(value)
