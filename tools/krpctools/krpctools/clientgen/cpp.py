from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser


def cpp_template_fix(typ):
    """ Ensure nested templates are separated by spaces for the C++ parser """
    return typ[:-2] + '> >' if typ.endswith('>>') else typ


class CppGenerator(Generator):

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
        Type.SINT32: 'google::protobuf::int32',
        Type.SINT64: 'google::protobuf::int64',
        Type.UINT32: 'google::protobuf::uint32',
        Type.UINT64: 'google::protobuf::uint64',
        Type.BOOL: 'bool',
        Type.STRING: 'std::string',
        Type.BYTES: 'std::string'
    }

    def parse_name(self, name):
        name = snake_case(name)
        if name in self._keywords:
            return '%s_' % name
        return name

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            return self._type_map[typ.protobuf_type.code]
        elif (isinstance(typ, MessageType) and
              typ.protobuf_type.code == Type.EVENT):
            return '::krpc::Event'
        elif isinstance(typ, MessageType):
            return 'krpc::schema::%s' % typ.python_type.__name__
        elif isinstance(typ, ListType):
            return cpp_template_fix(
                'std::vector<%s>' % self.parse_type(typ.value_type))
        elif isinstance(typ, SetType):
            return cpp_template_fix(
                'std::set<%s>' % self.parse_type(typ.value_type))
        elif isinstance(typ, DictionaryType):
            return cpp_template_fix(
                'std::map<%s, %s>' % (self.parse_type(typ.key_type),
                                      self.parse_type(typ.value_type)))
        elif isinstance(typ, TupleType):
            return cpp_template_fix(
                'std::tuple<%s>' % ', '.join(self.parse_type(t)
                                             for t in typ.value_types))
        elif isinstance(typ, (ClassType, EnumerationType)):
            return '%s::%s' % (typ.protobuf_type.service,
                               typ.protobuf_type.name)
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, typ):
        if typ is None:
            return 'void'
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_value(self, value, typ):
        if (isinstance(typ, ValueType) and
                typ.protobuf_type.code == Type.STRING):
            return '"%s"' % value
        elif (isinstance(typ, ValueType) and
              typ.protobuf_type.code == Type.BOOL):
            return 'true' if value else 'false'
        elif isinstance(typ, ClassType) and value is None:
            return self.parse_parameter_type(typ) + '()'
        elif isinstance(typ, EnumerationType):
            return 'static_cast<%s>(%s)' % \
                (self.parse_parameter_type(typ), value)
        elif value is None:
            return self.parse_parameter_type(typ) + '()'
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

    def parse_set_client(self, procedure):
        return isinstance(self.get_return_type(procedure), ClassType)

    @staticmethod
    def parse_documentation(documentation):
        documentation = CppDocParser().parse(documentation)
        if documentation == '':
            return ''
        lines = ['/**'] + [' * ' + line
                           for line in documentation.split('\n')] + [' */']
        return '\n'.join(line.rstrip() for line in lines)

    def parse_context(self, context):
        for info in context['procedures'].values():
            info['return_set_client'] = self.parse_set_client(
                info['procedure'])

        properties = {}
        for name, info in context['properties'].items():
            if info['getter']:
                properties[name] = {
                    'remote_name': info['getter']['remote_name'],
                    'parameters': [],
                    'return_type': info['type'],
                    'return_set_client': self.parse_set_client(
                        info['getter']['procedure']),
                    'documentation': info['documentation']
                }
            if info['setter']:
                properties['set_'+name] = {
                    'remote_name': info['setter']['remote_name'],
                    'parameters': self.generate_context_parameters(
                        info['setter']['procedure']),
                    'return_type': 'void',
                    'return_set_client': False,
                    'documentation': info['documentation']
                }

        for _, class_info in context['classes'].items():
            for info in class_info['methods'].values():
                info['return_set_client'] = self.parse_set_client(
                    info['procedure'])

            for info in class_info['static_methods'].values():
                info['return_set_client'] = self.parse_set_client(
                    info['procedure'])

            class_properties = {}
            for name, info in class_info['properties'].items():
                if info['getter']:
                    class_properties[name] = {
                        'remote_name': info['getter']['remote_name'],
                        'parameters': [],
                        'return_type': info['type'],
                        'return_set_client_fn': self.parse_set_client(
                            info['getter']['procedure']),
                        'documentation': info['documentation']
                    }
                if info['setter']:
                    class_properties['set_'+name] = {
                        'remote_name': info['setter']['remote_name'],
                        'parameters': [self.generate_context_parameters(
                            info['setter']['procedure'])[1]],
                        'return_type': 'void',
                        'return_set_client': False,
                        'documentation': info['documentation']
                    }
            class_info['properties'] = class_properties

        context['properties'] = properties
        return context


class CppDocParser(DocParser):

    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return '\n\n'+self.parse_node(node).strip()

    def parse_param(self, node):
        return '\n@param %s %s' % \
            (node.attrib['name'], self.parse_node(node).strip())

    def parse_returns(self, node):
        return '\n@return %s' % self.parse_node(node).strip()

    def parse_see(self, node):
        return self.parse_cref(node.attrib['cref'])

    @staticmethod
    def parse_paramref(node):
        return node.attrib['name']

    @staticmethod
    def parse_a(node):
        return '<a href="%s">%s</a>' % (node.attrib['href'], node.text)

    @staticmethod
    def parse_c(node):
        return node.text

    @staticmethod
    def parse_math(node):
        return node.text

    def parse_list(self, node):
        content = ['- %s\n' % self.parse_node(item[0], indent=2)[2:].rstrip()
                   for item in node]
        return '\n'+''.join(content)

    @staticmethod
    def parse_cref(cref):
        if cref[0] == 'M':
            cref = cref[2:].split('.')
            member = snake_case(cref[-1])
            del cref[-1]
            return '::'.join(cref)+'::'+member
        elif cref[0] == 'T':
            return cref[2:].replace('.', '::')
        else:
            raise RuntimeError('Unknown cref \'%s\'' % cref)
