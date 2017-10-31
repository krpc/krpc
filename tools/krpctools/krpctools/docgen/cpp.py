from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue


class CppDomain(Domain):
    name = 'cpp'
    prettyname = 'C++'
    sphinxname = 'cpp'
    highlight = 'cpp'
    codeext = 'cpp'

    _keywords = set([
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

    def currentmodule(self, name):
        super(CppDomain, self).currentmodule(name)
        return '.. namespace:: krpc::services::%s' % name

    def method_name(self, name):
        if snake_case(name) in self._keywords:
            return '%s_' % name
        return name

    def type(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'krpc::schema::%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            name = '%s.%s' % \
                   (typ.protobuf_type.service, typ.protobuf_type.name)
            return self.shorten_ref(name).replace('.', '::')
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % \
                (self.type(typ.key_type), self.type(typ.value_type))
        elif isinstance(typ, SetType):
            return 'std::set<%s>' % self.type(typ.value_type)
        elif isinstance(typ, TupleType):
            return 'std::tuple<%s>' % \
                ', '.join(self.type(typ) for typ in typ.value_types)
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def type_description(self, typ):
        if typ is None:
            return 'void'
        elif isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return ':class:`krpc::schema::%s`' % typ.python_type.__name__
        elif isinstance(typ, ClassType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return 'std::vector<%s>' % self.type_description(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'std::map<%s,%s>' % \
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

    def default_value(self, typ, value):
        if (isinstance(typ, ValueType) and
                typ.protobuf_type.code == Type.STRING):
            return '"%s"' % value
        elif (isinstance(typ, ValueType) and
              typ.protobuf_type.code == Type.BOOL):
            return 'true' if value else 'false'
        elif isinstance(typ, EnumerationType):
            return 'static_cast<%s>(%s)' % (self.type(typ), value)
        elif value is None:
            return self.type(typ) + '()'
        elif isinstance(typ, TupleType):
            values = (self.default_value(typ.value_types[i], x)
                      for i, x in enumerate(value))
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, ListType):
            values = (self.default_value(typ.value_type, x) for x in value)
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, SetType):
            values = (self.default_value(typ.value_type, x) for x in value)
            return '(%s)' % ', '.join(values)
        elif isinstance(typ, DictionaryType):
            entries = ('(%s, %s)' % (self.default_value(typ.key_type, k),
                                     self.default_value(typ.value_type, v))
                       for k, v in value.items())
            return '(%s)' % ', '.join(entries)
        return str(value)
