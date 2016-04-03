from .generator import Generator
from .docparser import DocParser
from ..utils import lower_camel_case, upper_camel_case
from krpc.utils import snake_case
import krpc.types
import hashlib

Types = krpc.types.Types()

class JavaGenerator(Generator):

    def __init__(self, macro_template, service, definition_files):
        super(JavaGenerator, self).__init__(macro_template, service, definition_files)

    def keywords(self):
        return [
            'abstract', 'continue', 'for', 'new', 'switch', 'assert', 'default', 'goto', 'package', 'synchronized',
            'boolean', 'do', 'if', 'private', 'this', 'break', 'double', 'implements', 'protected', 'throw',
            'byte', 'else', 'import', 'public', 'throws', 'case', 'enum', 'instanceof', 'return', 'transient',
            'catch', 'extends', 'int', 'short', 'try', 'char', 'final', 'interface', 'static', 'void',
            'class', 'finally', 'long', 'strictfp', 'volatile', 'const', 'float', 'native', 'super', 'while',
            'wait'
        ]

    def get_tuple_class_name(self, value_types):
        names = ['Unit', 'Pair', 'Triplet', 'Quartet', 'Quintet', 'Sextet', 'Septet', 'Octet', 'Ennead', 'Decade']
        return names[len(value_types)-1]


    def parse_name(self, name):
        name = lower_camel_case(name)
        if name in self.keywords():
            return '%s_' % name
        else:
            return name

    def parse_const_name(self, name):
        return snake_case(name).upper()

    def parse_type(self, typ, in_collection=False):
        if not in_collection and isinstance(typ, krpc.types.ValueType):
            typ = typ.protobuf_type
            if typ == 'string':
                return 'String'
            elif typ == 'bytes':
                return 'byte[]'
            elif typ == 'float':
                return 'float'
            elif typ == 'double':
                return 'double'
            elif typ == 'bool':
                return 'boolean'
            elif 'int' in typ:
                int_type_map = {
                    'int32' : 'int',
                    'uint32': 'int',
                    'int64' : 'long',
                    'uint64': 'long'
                }
                return int_type_map[typ]
        elif isinstance(typ, krpc.types.ValueType):
            typ = typ.protobuf_type
            if typ == 'string':
                return 'String'
            elif typ == 'bytes':
                return 'byte[]'
            elif typ == 'float':
                return 'Float'
            elif typ == 'double':
                return 'Double'
            elif typ == 'bool':
                return 'Boolean'
            elif 'int' in typ:
                int_type_map = {
                    'int32' : 'Integer',
                    'uint32': 'Integer',
                    'int64' : 'Long',
                    'uint64': 'Long'
                }
                return int_type_map[typ]
        elif isinstance(typ, krpc.types.MessageType):
            typ = typ.protobuf_type
            if typ.startswith('KRPC.'):
                _,_,x = typ.rpartition('.')
                return 'krpc.schema.KRPC.%s' % x
            elif typ.startswith('Test.'):
                _,_,x = typ.rpartition('.')
                return 'test.Test.%s' % x
        elif isinstance(typ, krpc.types.ListType):
            return 'java.util.List<%s>' % \
                self.parse_type(Types.as_type(typ.protobuf_type[5:-1]), True)
        elif isinstance(typ, krpc.types.SetType):
            return 'java.util.Set<%s>' % \
                self.parse_type(Types.as_type(typ.protobuf_type[4:-1]), True)
        elif isinstance(typ, krpc.types.DictionaryType):
            key_type,value_type = tuple(typ.protobuf_type[11:-1].split(','))
            return 'java.util.Map<%s,%s>' % \
                (self.parse_type(Types.as_type(key_type)), self.parse_type(Types.as_type(value_type), True))
        elif isinstance(typ, krpc.types.TupleType):
            value_types = typ.protobuf_type[6:-1].split(',')
            name = self.get_tuple_class_name(value_types)
            return 'org.javatuples.'+name+'<%s>' % (','.join(self.parse_type(Types.as_type(t), True) for t in value_types))
        elif isinstance(typ, krpc.types.ClassType):
            return 'krpc.client.services.%s' % typ.protobuf_type[6:-1]
        elif isinstance(typ, krpc.types.EnumType):
            return 'krpc.client.services.%s' % typ.protobuf_type[5:-1]
        raise RuntimeError('Unknown type ' + typ)

    def parse_type_specification(self, typ):
        if typ is None:
            return None
        if isinstance(typ, krpc.types.ListType):
            return 'new TypeSpecification(java.util.List.class, %s)' % \
                self.parse_type_specification(Types.as_type(typ.protobuf_type[5:-1]))
        elif isinstance(typ, krpc.types.SetType):
            return 'new TypeSpecification(java.util.Set.class, %s)' % \
                self.parse_type_specification(Types.as_type(typ.protobuf_type[4:-1]))
        elif isinstance(typ, krpc.types.DictionaryType):
            key_type,value_type = tuple(typ.protobuf_type[11:-1].split(','))
            return 'new TypeSpecification(java.util.Map.class, %s, %s)' % \
                (self.parse_type_specification(Types.as_type(key_type)),
                 self.parse_type_specification(Types.as_type(value_type)))
        elif isinstance(typ, krpc.types.TupleType):
            value_types = typ.protobuf_type[6:-1].split(',')
            return 'new TypeSpecification(org.javatuples.%s.class, %s)' % \
                (self.get_tuple_class_name(value_types),
                 ','.join(self.parse_type_specification(Types.as_type(t)) for t in value_types))
        else:
            return 'new TypeSpecification(%s.class)' % self.parse_type(typ, True)

    def get_return_type(self, procedure):
        if 'return_type' in procedure is not None:
            return Types.get_return_type(procedure['return_type'], procedure['attributes'])
        return None

    def get_parameter_type(self, procedure, pos):
        return Types.get_parameter_type(pos, procedure['parameters'][pos]['type'], procedure['attributes'])

    def parse_return_type(self, procedure):
        if 'return_type' in procedure is not None:
            typ = Types.get_return_type(procedure['return_type'], procedure['attributes'])
            return self.parse_type(typ)
        else:
            return 'void'

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_argument(self, value, typ):
        # No default arguments in Java
        return None

    def parse_documentation(self, documentation):
        documentation = JavaDocParser().parse(documentation)
        if documentation == '':
            return ''
        lines = ['/**'] + [' * ' + line for line in documentation.split('\n')] + [' */']
        return '\n'.join(line.rstrip() for line in lines)

    def parse_context(self, context):
        # Expand service properties into get and set methods
        properties = {}
        for name,info in context['properties'].items():
            if info['getter']:
                properties['get'+upper_camel_case(name)] = {
                    'procedure': info['getter']['procedure'],
                    'remote_name': info['getter']['remote_name'],
                    'parameters': [],
                    'return_type': info['type'],
                    'documentation': info['documentation']
                }
            if info['setter']:
                properties['set'+upper_camel_case(name)] = {
                    'procedure': info['setter']['procedure'],
                    'remote_name': info['setter']['remote_name'],
                    'parameters': self.generate_context_parameters(info['setter']['procedure']),
                    'return_type': 'void',
                    'documentation': info['documentation']
                }
        context['properties'] = properties

        # Expand class properties into get and set methods
        for class_name,class_info in context['classes'].items():
            class_properties = {}
            for name,info in class_info['properties'].items():
                if info['getter']:
                    class_properties['get'+upper_camel_case(name)] = {
                        'procedure': info['getter']['procedure'],
                        'remote_name': info['getter']['remote_name'],
                        'parameters': [],
                        'return_type': info['type'],
                        'documentation': info['documentation']
                    }
                if info['setter']:
                    class_properties['set'+upper_camel_case(name)] = {
                        'procedure': info['setter']['procedure'],
                        'remote_name': info['setter']['remote_name'],
                        'parameters': [self.generate_context_parameters(info['setter']['procedure'])[1]],
                        'return_type': 'void',
                        'documentation': info['documentation']
                    }
            class_info['properties'] = class_properties

        # Add type specifications to types
        for info in context['procedures'].values() + context['properties'].values():
            info['return_type'] = {
                'name': info['return_type'],
                'spec': self.parse_type_specification(self.get_return_type(info['procedure']))
            }
            pos = 0
            for pinfo in info['parameters']:
                param_type = self.get_parameter_type(info['procedure'], pos)
                pinfo['type'] = {
                    'name': pinfo['type'],
                    'spec': self.parse_type_specification(param_type)
                }
                pos += 1

        for class_name,class_info in context['classes'].items():
            for name,info in class_info['methods'].items() + class_info['static_methods'].items() + class_info['properties'].items():
                info['return_type'] = {
                    'name': info['return_type'],
                    'spec': self.parse_type_specification(self.get_return_type(info['procedure']))
                }
                pos = 0
                for pinfo in info['parameters']:
                    param_type = self.get_parameter_type(info['procedure'], pos)
                    pinfo['type'] = {
                        'name': pinfo['type'],
                        'spec': self.parse_type_specification(param_type)
                    }
                    pos += 1

        # Make enumeration members UPPER_SNAKE_CASE
        for enm in context['enumerations'].values():
            for value in enm['values']:
                value['name'] = self.parse_const_name(value['name'])

        # Add serial version UIDs to classes (generated using seeded hash of class' name)
        for class_name,cls in context['classes'].items():
            cls['serial_version_uid'] = int(hashlib.sha1('bada55'+class_name).hexdigest(), 16) % (10 ** 18)

        return context

class JavaDocParser(DocParser):
    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return '\n\n'+self.parse_node(node).strip()

    def parse_param(self, node):
        return '\n@param %s %s' % (node.attrib['name'], self.parse_node(node).strip())

    def parse_returns(self, node):
        return '\n@return %s' % self.parse_node(node).strip()

    def parse_see(self, node):
        return '{@link %s}' % self.parse_cref(node.attrib['cref'])

    def parse_paramref(self, node):
        return node.attrib['name']

    def parse_a(self, node):
        return '<a href="%s">%s</a>' % (node.attrib['href'], node.text)

    def parse_c(self, node):
        return '{@code %s}' % node.text

    def parse_math(self, node):
        return node.text

    def parse_list(self, node):
        content = ['<li>%s\n' % self.parse_node(item[0], indent=2)[2:].rstrip() for item in node]
        return '<p><ul>'+'\n'+''.join(content)+'</ul></p>'

    def parse_cref(self, cref):
        if cref[0] == 'M':
            cref = cref[2:].split('.')
            member = lower_camel_case(cref[-1])
            del cref[-1]
            return '.'.join(cref)+'#'+member
        elif cref[0] == 'T':
            return cref[2:]
        else:
            raise RuntimeError('Unknown cref \'%s\'' % cref)
