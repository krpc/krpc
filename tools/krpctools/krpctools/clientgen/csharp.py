import krpc.types
from krpc.utils import split_type_string
from .generator import Generator


class CsharpGenerator(Generator):

    def __init__(self, macro_template, service, definition_files):
        super(CsharpGenerator, self).__init__(
            macro_template, service, definition_files)

    _keywords = set([
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

    def parse_name(self, name):
        if name in self._keywords:
            return '%s_' % name
        else:
            return name

    def parse_type(self, typ, interface=True):
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
                    'int16': 'Int16',
                    'uint16': 'UInt16',
                    'int32': 'Int32',
                    'uint32': 'UInt32',
                    'int64': 'Int64',
                    'uint64': 'UInt64'
                }
                return int_type_map[typ]
        elif isinstance(typ, krpc.types.MessageType):
            typ = typ.protobuf_type
            if typ.startswith('KRPC.'):
                _, _, x = typ.rpartition('.')
                return 'global::KRPC.Schema.KRPC.%s' % x
            elif typ.startswith('Test.'):
                _, _, x = typ.rpartition('.')
                return 'global::Test.%s' % x
        elif isinstance(typ, krpc.types.TupleType):
            value_types = split_type_string(typ.protobuf_type[6:-1])
            return 'global::System.Tuple<%s>' % \
                ','.join(self.parse_type(self.types.as_type(t))
                         for t in value_types)
        elif isinstance(typ, krpc.types.ListType):
            if interface:
                name = 'IList'
            else:
                name = 'List'
            return 'global::System.Collections.Generic.%s<%s>' % \
                (name,
                 self.parse_type(self.types.as_type(typ.protobuf_type[5:-1])))
        elif isinstance(typ, krpc.types.SetType):
            if interface:
                name = 'ISet'
            else:
                name = 'HashSet'
            return 'global::System.Collections.Generic.%s<%s>' % \
                (name,
                 self.parse_type(self.types.as_type(typ.protobuf_type[4:-1])))
        elif isinstance(typ, krpc.types.DictionaryType):
            if interface:
                name = 'IDictionary'
            else:
                name = 'Dictionary'
            typs = split_type_string(typ.protobuf_type[11:-1])
            return 'global::System.Collections.Generic.%s<%s,%s>' % \
                (name,
                 self.parse_type(self.types.as_type(typs[0])),
                 self.parse_type(self.types.as_type(typs[1])))
        elif isinstance(typ, krpc.types.ClassType):
            return 'global::KRPC.Client.Services.%s' % typ.protobuf_type[6:-1]
        elif isinstance(typ, krpc.types.EnumType):
            return 'global::KRPC.Client.Services.%s' % typ.protobuf_type[5:-1]
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, typ):
        if typ is None:
            return 'void'
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_value(self, value, typ):
        if isinstance(typ, krpc.types.ValueType) and \
           typ.protobuf_type == 'string':
            return '"%s"' % value
        if isinstance(typ, krpc.types.ValueType) and \
           typ.protobuf_type == 'bool':
            if value:
                return 'true'
            else:
                return 'false'
        elif (isinstance(typ, krpc.types.ValueType) and
              typ.protobuf_type == 'float'):
            return str(value) + "f"
        elif isinstance(typ, krpc.types.EnumType):
            return '(global::KRPC.Client.Services.%s)%s' % \
                (typ.protobuf_type[5:-1], value)
        elif value is None:
            return 'null'
        elif isinstance(typ, krpc.types.TupleType):
            values = (self.parse_default_value(x, typ.value_types[i])
                      for i, x in enumerate(value))
            return 'new %s (%s)' % \
                (self.parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, krpc.types.ListType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return 'new %s { %s }' % \
                (self.parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, krpc.types.SetType):
            values = (self.parse_default_value(x, typ.value_type)
                      for x in value)
            return 'new %s { %s }' % \
                (self.parse_type(typ, False), ', '.join(values))
        elif isinstance(typ, krpc.types.DictionaryType):
            entries = ('{ %s, %s }' %
                       (self.parse_default_value(k, typ.key_type),
                        self.parse_default_value(v, typ.value_type))
                       for k, v in value.items())
            return 'new %s {%s}' % \
                (self.parse_type(typ, False), ', '.join(entries))
        else:
            return str(value)

    @staticmethod
    def parse_documentation(documentation):
        documentation = documentation \
            .replace('<doc>', '').replace('</doc>', '').strip()
        if documentation == '':
            return ''
        lines = ['/// '+line for line in documentation.split('\n')]
        content = '\n'.join(line.rstrip() for line in lines)
        content = content.replace('  <param', '<param')
        content = content.replace('  <returns', '<returns')
        content = content.replace('  <remarks', '<remarks')
        return content

    def generate_context_parameters(self, procedure):
        parameters = super(CsharpGenerator, self) \
            .generate_context_parameters(procedure)
        for parameter in parameters:
            if 'default_value' not in parameter:
                parameter['name_value'] = parameter['name']
                continue
            typ = parameter['type']
            default_value = parameter['default_value']
            if typ.startswith('global::System.Tuple') or \
               typ.startswith('global::System.Collections.Generic.IList') or \
               typ.startswith('global::System.Collections.Generic.ISet') or \
               typ.startswith('global::System.Collections' +
                              '.Generic.IDictionary'):
                parameter['name_value'] = '%s ?? %s' % \
                                          (parameter['name'], default_value)
                parameter['default_value'] = 'null'
            else:
                parameter['name_value'] = parameter['name']
        return parameters

    @staticmethod
    def parse_context(context):
        return context
