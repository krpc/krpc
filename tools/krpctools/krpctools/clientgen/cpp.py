import krpc.types
from krpc.utils import snake_case, split_type_string
from .generator import Generator
from .docparser import DocParser

def cpp_template_fix(typ):
    """ Ensure nested templates are separated by spaces for the C++ parser """
    if typ.endswith('>>'):
        return typ[:-2] + '> >'
    else:
        return typ

class CppGenerator(Generator):

    def __init__(self, macro_template, service, definition_files):
        super(CppGenerator, self).__init__(macro_template, service, definition_files)

    _keywords = set([
        'alignas', 'alignof', 'and', 'and_eq', 'asm', 'auto', 'bitand', 'bitor', 'bool', 'break',
        'case', 'catch', 'char', 'char16_t', 'char32_t', 'class', 'compl', 'concept', 'const', 'constexpr',
        'const_cast', 'continue', 'decltype', 'default', 'delete', 'do', 'double', 'dynamic_cast', 'else',
        'enum', 'explicit', 'export', 'extern', 'false', 'float', 'for', 'friend', 'goto', 'if', 'inline',
        'int', 'long', 'mutable', 'namespace', 'new', 'noexcept', 'not', 'not_eq', 'nullptr', 'operator',
        'or', 'or_eq', 'private', 'protected', 'public', 'register', 'reinterpret_cast', 'requires',
        'return', 'short', 'signed', 'sizeof', 'static', 'static_assert', 'static_cast', 'struct', 'switch',
        'template', 'this', 'thread_local', 'throw', 'true', 'try', 'typedef', 'typeid', 'typename', 'union',
        'unsigned', 'using', 'virtual', 'void', 'volatile', 'wchar_t', 'while', 'xor', 'xor_eq'
    ])

    def parse_name(self, name):
        name = snake_case(name)
        if name in self._keywords:
            return '%s_' % name
        else:
            return name

    def parse_type(self, typ):
        if isinstance(typ, krpc.types.ValueType):
            typ = typ.protobuf_type
            if typ == 'string' or typ == 'bytes':
                return 'std::string'
            elif 'int' in typ:
                return 'google::protobuf::%s' % typ
            else:
                return typ
        elif isinstance(typ, krpc.types.MessageType):
            typ = typ.protobuf_type
            if typ.startswith('KRPC.'):
                _, _, x = typ.rpartition('.')
                return 'krpc::schema::%s' % x
            elif typ.startswith('Test.'):
                _, _, x = typ.rpartition('.')
                return 'Test::%s' % x
        elif isinstance(typ, krpc.types.ListType):
            return cpp_template_fix('std::vector<%s>' %
                                    self.parse_type(self.types.as_type(typ.protobuf_type[5:-1])))
        elif isinstance(typ, krpc.types.SetType):
            return cpp_template_fix('std::set<%s>' % self.parse_type(self.types.as_type(typ.protobuf_type[4:-1])))
        elif isinstance(typ, krpc.types.DictionaryType):
            typs = split_type_string(typ.protobuf_type[11:-1])
            return cpp_template_fix('std::map<%s, %s>' % \
                (self.parse_type(self.types.as_type(typs[0])), self.parse_type(self.types.as_type(typs[1]))))
        elif isinstance(typ, krpc.types.TupleType):
            value_types = split_type_string(typ.protobuf_type[6:-1])
            return cpp_template_fix('std::tuple<%s>' % \
                ', '.join(self.parse_type(self.types.as_type(t)) for t in value_types))
        elif isinstance(typ, krpc.types.ClassType):
            return typ.protobuf_type[6:-1].replace('.', '::')
        elif isinstance(typ, krpc.types.EnumType):
            return typ.protobuf_type[5:-1].replace('.', '::')
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, typ):
        if typ is None:
            return 'void'
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_value(self, value, typ):
        if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'string':
            return '"%s"' % value
        if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'bool':
            if value:
                return 'true'
            else:
                return 'false'
        elif isinstance(typ, krpc.types.EnumType):
            return 'static_cast<%s>(%s)' % (self.parse_parameter_type(typ), value)
        elif value is None:
            return self.parse_parameter_type(typ) + '()'
        elif isinstance(typ, krpc.types.TupleType):
            values = (self.parse_default_value(x, typ.value_types[i]) for i, x in enumerate(value))
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, krpc.types.ListType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, krpc.types.SetType):
            values = (self.parse_default_value(x, typ.value_type) for x in value)
            return '%s{%s}' % (self.parse_type(typ), ', '.join(values))
        elif isinstance(typ, krpc.types.DictionaryType):
            entries = ('{%s, %s}' % (self.parse_default_value(k, typ.key_type),
                                     self.parse_default_value(v, typ.value_type))
                       for k, v in value.items())
            return '%s{%s}' % (self.parse_type(typ), ', '.join(entries))
        else:
            return str(value)

    def parse_set_client(self, procedure):
        return isinstance(self.get_return_type(procedure), krpc.types.ClassType)

    @staticmethod
    def parse_documentation(documentation):
        documentation = CppDocParser().parse(documentation)
        if documentation == '':
            return ''
        lines = ['/**'] + [' * ' + line for line in documentation.split('\n')] + [' */']
        return '\n'.join(line.rstrip() for line in lines)

    def parse_context(self, context):
        for info in context['procedures'].values():
            info['return_set_client'] = self.parse_set_client(info['procedure'])

        properties = {}
        for name, info in context['properties'].items():
            if info['getter']:
                properties[name] = {
                    'remote_name': info['getter']['remote_name'],
                    'parameters': [],
                    'return_type': info['type'],
                    'return_set_client': self.parse_set_client(info['getter']['procedure']),
                    'documentation': info['documentation']
                }
            if info['setter']:
                properties['set_'+name] = {
                    'remote_name': info['setter']['remote_name'],
                    'parameters': self.generate_context_parameters(info['setter']['procedure']),
                    'return_type': 'void',
                    'return_set_client': False,
                    'documentation': info['documentation']
                }

        for _, class_info in context['classes'].items():
            for info in class_info['methods'].values():
                info['return_set_client'] = self.parse_set_client(info['procedure'])

            for info in class_info['static_methods'].values():
                info['return_set_client'] = self.parse_set_client(info['procedure'])

            class_properties = {}
            for name, info in class_info['properties'].items():
                if info['getter']:
                    class_properties[name] = {
                        'remote_name': info['getter']['remote_name'],
                        'parameters': [],
                        'return_type': info['type'],
                        'return_set_client_fn': self.parse_set_client(info['getter']['procedure']),
                        'documentation': info['documentation']
                    }
                if info['setter']:
                    class_properties['set_'+name] = {
                        'remote_name': info['setter']['remote_name'],
                        'parameters': [self.generate_context_parameters(info['setter']['procedure'])[1]],
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
        return '\n@param %s %s' % (node.attrib['name'], self.parse_node(node).strip())

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
        content = ['- %s\n' % self.parse_node(item[0], indent=2)[2:].rstrip() for item in node]
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
