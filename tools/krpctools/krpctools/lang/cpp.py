from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .language import Language


class CppLanguage(Language):

    keywords = set([
        'alignas', 'alignof', 'and', 'and_eq', 'asm', 'auto', 'bitand',
        'bitor', 'bool', 'break', 'case', 'catch', 'char', 'char16_t',
        'char32_t', 'class', 'compl', 'concept', 'const', 'constexpr',
        'const_cast', 'continue', 'decltype', 'default', 'delete', 'do',
        'double', 'dynamic_cast', 'else', 'enum', 'explicit', 'export',
        'extern', 'false', 'float', 'for', 'friend', 'goto', 'if', 'inline',
        'int', 'long', 'mutable', 'namespace', 'new', 'noexcept', 'not',
        'not_eq', 'nullptr', 'operator', 'or', 'or_eq', 'private', 'protected',
        'public', 'register', 'reinterpret_cast', 'requires', 'return',
        'short', 'signed', 'sizeof', 'static', 'static_assert', 'static_cast',
        'struct', 'switch', 'template', 'this', 'thread_local', 'throw',
        'true', 'try', 'typedef', 'typeid', 'typename', 'union', 'unsigned',
        'using', 'virtual', 'void', 'volatile', 'wchar_t', 'while', 'xor',
        'xor_eq'
    ])

    type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int32_t',
        Type.SINT64: 'int64_t',
        Type.UINT32: 'uint32_t',
        Type.UINT64: 'uint64_t',
        Type.BOOL: 'bool',
        Type.STRING: 'std::string',
        Type.BYTES: 'std::string'
    }

    value_map = {
        'null': 'NULL'
    }

    def parse_name(self, name):
        return super(CppLanguage, self).parse_name(snake_case(name))

    def parse_type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif (isinstance(typ, MessageType) and
              typ.protobuf_type.code == Type.EVENT):
            return '::krpc::Event'
        elif isinstance(typ, MessageType):
            return 'krpc::schema::%s' % typ.python_type.__name__
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.parse_type(typ.value_type)
        elif isinstance(typ, SetType):
            return 'std::set<%s>' % self.parse_type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s, %s>' % (self.parse_type(typ.key_type),
                                         self.parse_type(typ.value_type))
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % ', '.join(self.parse_type(t)
                                                for t in typ.value_types)
        elif isinstance(typ, (ClassType, EnumerationType)):
            name = '%s.%s' % (typ.protobuf_type.service,
                              typ.protobuf_type.name)
            return self.shorten_ref(name).replace('.', '::')
        raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def parse_default_value(self, value, typ):
        if (isinstance(typ, ValueType) and
                typ.protobuf_type.code == Type.STRING):
            return '"%s"' % value
        elif (isinstance(typ, ValueType) and
              typ.protobuf_type.code == Type.BOOL):
            return 'true' if value else 'false'
        elif isinstance(typ, ClassType) and value is None:
            return self.parse_type(typ) + '()'
        elif isinstance(typ, EnumerationType):
            return 'static_cast<%s>(%s)' % \
                (self.parse_type(typ), value)
        elif value is None:
            return self.parse_type(typ) + '()'
        elif isinstance(typ, TupleType):
            values = (self.parse_default_value(x, typ.value_types[i])
                      for i, x in enumerate(value))
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, ListType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, SetType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, DictionaryType):
            entries = ('{%s, %s}' %
                       (self.parse_default_value(k, typ.key_type),
                        self.parse_default_value(v, typ.value_type))
                       for k, v in value.items())
            return '%s{%s}' % (self.parse_type(typ), ', '.join(entries))
        return str(value)

    def shorten_ref(self, name):
        name = name.split('.')
        if name[0] == self.module:
            del name[0]
        return '.'.join(name)
