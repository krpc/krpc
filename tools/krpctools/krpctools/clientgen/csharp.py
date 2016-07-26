from .generator import Generator
from krpc.schema.KRPC import Type
from krpc.types import ValueType, ClassType, EnumerationType, MessageType
from krpc.types import TupleType, ListType, SetType, DictionaryType

class CsharpGenerator(Generator):

    def __init__(self, macro_template, service, definition_files):
        super(CsharpGenerator, self).__init__(macro_template, service, definition_files)

    _keywords = set([
        'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked', 'class',
        'const', 'continue', 'decimal', 'default', 'delegate', 'do', 'double', 'else', 'enum', 'event',
        'explicit', 'extern', 'false', 'finally', 'fixed', 'float', 'for', 'foreach', 'goto', 'if',
        'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace', 'new',
        'null', 'object', 'operator', 'out', 'override', 'params', 'private', 'protected', 'public',
        'readonly', 'ref', 'return', 'sbyte', 'sealed', 'short', 'sizeof', 'stackalloc', 'static',
        'string', 'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong',
        'unchecked', 'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while'
    ])

    _type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.INT32: 'int',
        Type.INT64: 'long',
        Type.UINT32: 'uint',
        Type.UINT64: 'ulong',
        Type.BOOL: 'bool',
        Type.STRING: 'string',
        Type.BYTES: 'byte[]'
    }

    def parse_name(self, name):
        if name in self._keywords:
            return '%s_' % name
        else:
            return name

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            return self._type_map[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'global::KRPC.Schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, ListType):
            return 'global::System.Collections.Generic.IList<%s>' % self.parse_type(typ.value_type)
        elif isinstance(typ, SetType):
            return 'global::System.Collections.Generic.ISet<%s>' % self.parse_type(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'global::System.Collections.Generic.IDictionary<%s,%s>' % \
                (self.parse_type(typ.key_type), self.parse_type(typ.value_type))
        elif isinstance(typ, TupleType):
            return 'global::System.Tuple<%s>' % ','.join(self.parse_type(t) for t in typ.value_types)
        elif isinstance(typ, ClassType) or isinstance(typ, EnumerationType):
            return 'global::KRPC.Client.Services.%s.%s' % (typ.protobuf_type.service, typ.protobuf_type.name)
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, typ):
        if typ is None:
            return 'void'
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    @staticmethod
    def parse_default_value(value, typ):
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.STRING:
            return '"%s"' % value
        if isinstance(typ, ValueType) and typ.protobuf_type.code == Type.BOOL:
            if value:
                return 'true'
            else:
                return 'false'
        elif isinstance(typ, ValueType) and typ.protobuf_type.code == Type.FLOAT:
            return str(value) + "f"
        elif isinstance(typ, ClassType) and value is None:
            return 'null'
        elif isinstance(typ, EnumerationType):
            return '(global::KRPC.Client.Services.%s.%s)%s' % \
                (typ.protobuf_type.service, typ.protobuf_type.name, value)
        else:
            return value

    @staticmethod
    def parse_documentation(documentation):
        documentation = documentation.replace('<doc>', '').replace('</doc>', '').strip()
        if documentation == '':
            return ''
        lines = ['/// '+line for line in documentation.split('\n')]
        content = '\n'.join(line.rstrip() for line in lines)
        content = content.replace('  <param', '<param')
        content = content.replace('  <returns', '<returns')
        content = content.replace('  <remarks', '<remarks')
        return content

    @staticmethod
    def parse_context(context):
        return context
