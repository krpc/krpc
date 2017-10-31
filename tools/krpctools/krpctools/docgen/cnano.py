from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue


class CnanoDomain(Domain):
    name = 'cnano'
    prettyname = 'Cnano'
    sphinxname = 'c'
    highlight = 'c'
    codeext = 'c'

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

    _type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int32_t',
        Type.SINT64: 'int64_t',
        Type.UINT32: 'uint32_t',
        Type.UINT64: 'uint64_t',
        Type.BOOL: 'bool',
        Type.STRING: 'char *',
        Type.BYTES: 'krpc_bytes_t'
    }

    _type_name_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int32',
        Type.SINT64: 'int64',
        Type.UINT32: 'uint32',
        Type.UINT64: 'uint64',
        Type.BOOL: 'bool',
        Type.STRING: 'string',
        Type.BYTES: 'bytes'
    }

    value_map = {
        'null': 'nullptr'
    }

    def currentmodule(self, name):
        super(CnanoDomain, self).currentmodule(name)
        return ''

    def method_name(self, name):
        if name in self._keywords:
            return '%s_' % name
        return name

    def parse_type_name(self, typ):
        if isinstance(typ, ValueType):
            return self._type_name_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'message_%s' % typ.python_type.__name__
        elif isinstance(typ, ListType):
            return 'list_%s' % self.parse_type_name(typ.value_type)
        elif isinstance(typ, SetType):
            return 'set_%s' % self.parse_type_name(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'dictionary_%s_%s' % (self.parse_type_name(typ.key_type),
                                         self.parse_type_name(typ.value_type))
        elif isinstance(typ, TupleType):
            return 'tuple_%s' % \
                '_'.join(self.parse_type_name(t) for t in typ.value_types)
        elif isinstance(typ, ClassType):
            return 'object'
        elif isinstance(typ, EnumerationType):
            return 'enum'
        raise RuntimeError('Unknown type ' + str(typ))

    def type(self, typ):
        ptr = True
        if typ is None:
            return {'ctype': 'void', 'cvtype': None, 'name': None}
        elif isinstance(typ, ValueType):
            ctype = self._type_map[typ.protobuf_type.code]
            ptr = False
        elif isinstance(typ, MessageType):
            ctype = 'krpc_schema_%s' % typ.python_type.__name__
        elif isinstance(typ, ListType):
            ctype = 'krpc_list_%s_t' % self.parse_type_name(typ.value_type)
        elif isinstance(typ, SetType):
            ctype = 'krpc_set_%s_t' % self.parse_type_name(typ.value_type)
        elif isinstance(typ, DictionaryType):
            ctype = 'krpc_dictionary_%s_%s_t' % \
                    (self.parse_type_name(typ.key_type),
                     self.parse_type_name(typ.value_type))
        elif isinstance(typ, TupleType):
            ctype = 'krpc_tuple_%s_t' % \
                    '_'.join(self.parse_type_name(t) for t in typ.value_types)
        elif isinstance(typ, (ClassType, EnumerationType)):
            ctype = 'krpc_%s_%s_t' % \
                    (typ.protobuf_type.service, typ.protobuf_type.name)
            ptr = False
        else:
            raise RuntimeError('Unknown type ' + str(typ))
        # Note:
        #  name - name of the type used in encode/decode function names
        #  ctype - C type for the kRPC type
        #  cvtype - C 'value' type that is, for example,
        #           passed as an argument to functions
        #  ccvtype - const version of cvtype
        #  getval - gets the value from a pointer
        #  getptr - gets a pointer for a value
        #  structgetval - gets a value from a struct
        #  structgetptr - gets a pointer for a value in a struct
        #                 (equivalent to structgetval then getptr)
        #  removeconst - removes constness from a pointer for a value
        return {
            'name': self.parse_type_name(typ),
            'ctype': ctype,
            'cvtype': '%s *' % ctype if ptr else ctype,
            'ccvtype': 'const %s *' % ctype if ptr else
                       ('const ' + ctype if ctype.endswith('*') else ctype),
            'getval': '' if ptr else '*',
            'getptr': '' if ptr else '&',
            'structgetval': '&' if ptr else '',
            'structgetptr': '&',
            'removeconst': '(%s*)' % ctype if ptr else ''
        }

    def type_description(self, typ):
        return self.type(type)

    def return_type(self, typ):
        if self.type(typ)['ctype'] == 'void':
            return 'void'
        return self.type(typ)['ctype']+' *'

    def parameter_type(self, typ):
        return self.type(typ)['ccvtype']

    def ref(self, obj):
        name = obj.fullname.split('.')
        ref = 'krpc_'+'_'.join(name)
        if isinstance(obj, EnumerationValue):
            ref = ref.upper()
        elif isinstance(obj, (Class, Enumeration)):
            ref += '_t'
        return ref

    def see(self, obj):
        if isinstance(obj, (Property, ClassProperty)):
            prefix = 'func'
        elif isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            prefix = 'func'
        elif isinstance(obj, Class):
            prefix = 'type'
        elif isinstance(obj, Enumeration):
            prefix = 'type'
        elif isinstance(obj, EnumerationValue):
            prefix = 'macro'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))
