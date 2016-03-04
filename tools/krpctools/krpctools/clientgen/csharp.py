from .generator import Generator
import krpc.types

Types = krpc.types.Types()

class CsharpGenerator(Generator):

    def __init__(self, macro_template, service, definition_files):
        super(CsharpGenerator, self).__init__(macro_template, service, definition_files)

    def keywords(self):
        return [
            'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked', 'class',
            'const', 'continue', 'decimal', 'default', 'delegate', 'do', 'double', 'else', 'enum', 'event',
            'explicit', 'extern', 'false', 'finally', 'fixed', 'float', 'for', 'foreach', 'goto', 'if',
            'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace', 'new',
            'null', 'object', 'operator', 'out', 'override', 'params', 'private', 'protected', 'public',
            'readonly', 'ref', 'return', 'sbyte', 'sealed', 'short', 'sizeof', 'stackalloc', 'static',
            'string', 'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong',
            'unchecked', 'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while'
        ]

    def parse_name(self, name):
        if name in self.keywords():
            return '%s_' % name
        else:
            return name

    def parse_type(self, typ):
        if isinstance(typ, krpc.types.ValueType):
            typ = typ.protobuf_type
            if typ == 'string':
                return 'String'
            elif typ == 'bytes':
                return 'byte[]'
            elif typ == 'float':
                return 'Single'
            elif typ == 'double':
                return 'Double'
            elif typ == 'bool':
                return 'Boolean'
            elif 'int' in typ:
                int_type_map = {
                    'int16' : 'Int16',
                    'uint16': 'UInt16',
                    'int32' : 'Int32',
                    'uint32': 'UInt32',
                    'int64' : 'Int64',
                    'uint64': 'UInt64'
                }
                return int_type_map[typ]
        elif isinstance(typ, krpc.types.MessageType):
            typ = typ.protobuf_type
            if typ.startswith('KRPC.'):
                _,_,x = typ.rpartition('.')
                return 'global::KRPC.Schema.KRPC.%s' % x
            elif typ.startswith('Test.'):
                _,_,x = typ.rpartition('.')
                return 'global::Test.%s' % x
        elif isinstance(typ, krpc.types.ListType):
            return 'global::System.Collections.Generic.IList<%s>' % \
                self.parse_type(Types.as_type(typ.protobuf_type[5:-1]))
        elif isinstance(typ, krpc.types.SetType):
            return 'global::System.Collections.Generic.ISet<%s>' % \
                self.parse_type(Types.as_type(typ.protobuf_type[4:-1]))
        elif isinstance(typ, krpc.types.DictionaryType):
            key_type,value_type = tuple(typ.protobuf_type[11:-1].split(','))
            return 'global::System.Collections.Generic.IDictionary<%s,%s>' % \
                (self.parse_type(Types.as_type(key_type)), self.parse_type(Types.as_type(value_type)))
        elif isinstance(typ, krpc.types.TupleType):
            value_types = typ.protobuf_type[6:-1].split(',')
            return 'global::System.Tuple<%s>' % ','.join(self.parse_type(Types.as_type(t)) for t in value_types)
        elif isinstance(typ, krpc.types.ClassType):
            return 'global::KRPC.Client.Services.%s' % typ.protobuf_type[6:-1]
        elif isinstance(typ, krpc.types.EnumType):
            return 'global::KRPC.Client.Services.%s' % typ.protobuf_type[5:-1]
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, procedure):
        if 'return_type' in procedure is not None:
            typ = Types.get_return_type(procedure['return_type'], procedure['attributes'])
            return self.parse_type(typ)
        else:
            return 'void'

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_argument(self, value, typ):
        #TODO: following is a workaround for decoding EnumType, as set_values has not been called
        if krpc.platform.PY2:
            value = str(bytearray(value))
        else:
            value = bytearray(value)
        if not isinstance(typ, krpc.types.EnumType):
            value = krpc.decoder.Decoder.decode(value, typ)
        else:
            value = krpc.decoder.Decoder.decode(value, Types.as_type('int32'))
        if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'string':
            return '"%s"' % value
        if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'bool':
            if value:
                return 'true'
            else:
                return 'false'
        elif isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'float':
            return str(value) + "f"
        elif isinstance(typ, krpc.types.ClassType) and value is None:
            return 'null'
        elif isinstance(typ, krpc.types.EnumType):
            return '(global::KRPC.Client.Services.%s)%s' % (typ.protobuf_type[5:-1], value)
        else:
            return value

    def parse_documentation(self, documentation):
        documentation = documentation.replace('<doc>', '').replace('</doc>','').strip()
        if documentation == '':
            return ''
        lines = ['/// '+line for line in documentation.split('\n')]
        content = '\n'.join(line.rstrip() for line in lines)
        content = content.replace('  <param', '<param')
        content = content.replace('  <returns', '<returns')
        content = content.replace('  <remarks', '<remarks')
        return content

    def parse_context(self, context):
        return context
