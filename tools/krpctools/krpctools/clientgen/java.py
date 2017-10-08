import hashlib
import itertools
from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser
from ..utils import lower_camel_case, upper_camel_case, as_type


class JavaGenerator(Generator):

    _keywords = set([
        'abstract', 'continue', 'for', 'new', 'switch', 'assert', 'default',
        'goto', 'package', 'synchronized', 'boolean', 'do', 'if', 'private',
        'this', 'break', 'double', 'implements', 'protected', 'throw', 'byte',
        'else', 'import', 'public', 'throws', 'case', 'enum', 'instanceof',
        'return', 'transient', 'catch', 'extends', 'int', 'short', 'try',
        'char', 'final', 'interface', 'static', 'void', 'class', 'finally',
        'long', 'strictfp', 'volatile', 'const', 'float', 'native', 'super',
        'while', 'wait'
    ])

    _tuple_class_names = [
        'Unit', 'Pair', 'Triplet', 'Quartet', 'Quintet', 'Sextet', 'Septet',
        'Octet', 'Ennead', 'Decade'
    ]

    _type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int',
        Type.SINT64: 'long',
        Type.UINT32: 'int',
        Type.UINT64: 'long',
        Type.BOOL: 'boolean',
        Type.STRING: 'String',
        Type.BYTES: 'byte[]'
    }

    _type_map_classes = {
        Type.DOUBLE: 'Double',
        Type.FLOAT: 'Float',
        Type.SINT32: 'Integer',
        Type.SINT64: 'Long',
        Type.UINT32: 'Integer',
        Type.UINT64: 'Long',
        Type.BOOL: 'Boolean',
        Type.STRING: 'String',
        Type.BYTES: 'byte[]'
    }

    @classmethod
    def get_tuple_class_name(cls, value_types):
        return cls._tuple_class_names[len(value_types)-1]

    @classmethod
    def parse_name(cls, name):
        name = lower_camel_case(name)
        if name in cls._keywords:
            return '%s_' % name
        return name

    @staticmethod
    def parse_const_name(name):
        return snake_case(name).upper()

    def parse_type(self, typ, in_collection=False):
        if not in_collection and isinstance(typ, ValueType):
            return self._type_map[typ.protobuf_type.code]
        elif isinstance(typ, ValueType):
            return self._type_map_classes[typ.protobuf_type.code]
        elif (isinstance(typ, MessageType) and
              typ.protobuf_type.code == Type.EVENT):
            return 'krpc.client.Event'
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return 'krpc.client.services.%s.%s' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        elif isinstance(typ, TupleType):
            name = self.get_tuple_class_name(typ.value_types)
            return 'org.javatuples.'+name+'<%s>' % \
                (','.join(self.parse_type(t, True) for t in typ.value_types))
        elif isinstance(typ, ListType):
            return 'java.util.List<%s>' % self.parse_type(typ.value_type, True)
        elif isinstance(typ, SetType):
            return 'java.util.Set<%s>' % self.parse_type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return 'java.util.Map<%s,%s>' % \
                (self.parse_type(typ.key_type, True),
                 self.parse_type(typ.value_type, True))
        raise RuntimeError('Unknown type ' + typ)

    def parse_type_specification(self, typ):
        if typ is None:
            return None
        if isinstance(typ, ValueType):
            return 'krpc.client.Types.createValue(' + \
                'krpc.schema.KRPC.Type.TypeCode.%s)' % \
                Type.TypeCode.Name(typ.protobuf_type.code)
        elif isinstance(typ, MessageType):
            return 'krpc.client.Types.createMessage(' + \
                'krpc.schema.KRPC.Type.TypeCode.%s)' % \
                Type.TypeCode.Name(typ.protobuf_type.code)
        elif isinstance(typ, ClassType):
            return 'krpc.client.Types.createClass("%s", "%s")' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        elif isinstance(typ, EnumerationType):
            return 'krpc.client.Types.createEnumeration("%s", "%s")' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        elif isinstance(typ, TupleType):
            return 'krpc.client.Types.createTuple(%s)' % \
                ','.join(self.parse_type_specification(t)
                         for t in typ.value_types)
        elif isinstance(typ, ListType):
            return 'krpc.client.Types.createList(%s)' % \
                self.parse_type_specification(typ.value_type)
        elif isinstance(typ, SetType):
            return 'krpc.client.Types.createSet(%s)' % \
                self.parse_type_specification(typ.value_type)
        elif isinstance(typ, DictionaryType):
            return 'krpc.client.Types.createDictionary(%s, %s)' % \
                (self.parse_type_specification(typ.key_type),
                 self.parse_type_specification(typ.value_type))
        raise RuntimeError('Unknown type ' + typ)

    def parse_return_type(self, typ):
        if typ is None:
            return 'void'
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    @staticmethod
    def parse_default_value(value, typ):  # pylint: disable=unused-argument
        # No default arguments in Java
        return None

    @staticmethod
    def parse_documentation(documentation):
        documentation = JavaDocParser().parse(documentation)
        if documentation == '':
            return ''
        lines = ['/**'] + [' * ' + line
                           for line in documentation.split('\n')] + [' */']
        return '\n'.join(line.rstrip() for line in lines)

    def parse_context(self, context):
        # Expand service properties into get and set methods
        properties = {}
        for name, info in context['properties'].items():
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
                    'parameters': self.generate_context_parameters(
                        info['setter']['procedure']),
                    'return_type': 'void',
                    'documentation': info['documentation']
                }
        context['properties'] = properties

        # Expand class properties into get and set methods
        for class_name, class_info in context['classes'].items():
            class_properties = {}
            for name, info in class_info['properties'].items():
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
                        'parameters': [
                            self.generate_context_parameters(
                                info['setter']['procedure'])[1]],
                        'return_type': 'void',
                        'documentation': info['documentation']
                    }
            class_info['properties'] = class_properties

        # Add type specifications to types
        procedures = \
            context['procedures'].values() + \
            context['properties'].values() + \
            list(itertools.chain(
                *[class_info['static_methods'].values()
                  for class_info in context['classes'].values()]))
        for info in procedures:
            info['return_type'] = {
                'name': info['return_type'],
                'spec': self.parse_type_specification(
                    self.get_return_type(info['procedure']))
            }
            pos = 0
            for i, pinfo in enumerate(info['parameters']):
                param_type = as_type(
                    self.types, info['procedure']['parameters'][i]['type'])
                pinfo['type'] = {
                    'name': pinfo['type'],
                    'spec': self.parse_type_specification(param_type)
                }
                pos += 1

        for class_info in context['classes'].values():
            items = class_info['methods'].values() + \
                    class_info['properties'].values()
            for info in items:
                info['return_type'] = {
                    'name': info['return_type'],
                    'spec': self.parse_type_specification(
                        self.get_return_type(info['procedure']))
                }
                pos = 0
                for i, pinfo in enumerate(info['parameters']):
                    param_type = as_type(
                        self.types,
                        info['procedure']['parameters'][i+1]['type'])
                    pinfo['type'] = {
                        'name': pinfo['type'],
                        'spec': self.parse_type_specification(param_type)
                    }
                    pos += 1

        # Make enumeration members UPPER_SNAKE_CASE
        for enm in context['enumerations'].values():
            for value in enm['values']:
                value['name'] = self.parse_const_name(value['name'])

        # Add serial version UIDs to classes
        items = context['classes'].items() + context['exceptions'].items()
        for class_name, cls in items:
            tohash = self.service_name+'.'+class_name
            hsh = hashlib.sha1(tohash.encode('utf-8')).hexdigest()
            cls['serial_version_uid'] = int(hsh, 16) % (10 ** 18)

        return context


class JavaDocParser(DocParser):

    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return '\n\n'+self.parse_node(node).strip()

    def parse_param(self, node):
        return '\n@param %s %s' % (node.attrib['name'],
                                   self.parse_node(node).strip())

    def parse_returns(self, node):
        return '\n@return %s' % self.parse_node(node).strip()

    def parse_see(self, node):
        return '{@link %s}' % self.parse_cref(node.attrib['cref'])

    @staticmethod
    def parse_paramref(node):
        return node.attrib['name']

    @staticmethod
    def parse_a(node):
        return '<a href="%s">%s</a>' % (node.attrib['href'], node.text)

    @staticmethod
    def parse_c(node):
        return '{@code %s}' % node.text

    @staticmethod
    def parse_math(node):
        return node.text

    def parse_list(self, node):
        content = ['<li>%s\n' % self.parse_node(item[0], indent=2)[2:].rstrip()
                   for item in node]
        return '<p><ul>'+'\n'+''.join(content)+'</ul></p>'

    @staticmethod
    def parse_cref(cref):
        if cref[0] == 'M':
            cref = cref[2:].split('.')
            member = lower_camel_case(cref[-1])
            del cref[-1]
            return '.'.join(cref)+'#'+member
        elif cref[0] == 'T':
            return cref[2:]
        else:
            raise RuntimeError('Unknown cref \'%s\'' % cref)
