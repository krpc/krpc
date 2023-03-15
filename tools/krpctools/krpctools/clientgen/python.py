import collections
import itertools
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
import krpc.schema.KRPC_pb2 as KRPC
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser
from ..lang.python import PythonLanguage
from ..utils import lower_camel_case, as_type


class PythonGenerator(Generator):

    language = PythonLanguage()

    def parse_python_type(self, typ):
        if typ is None:
            return 'None'
        if isinstance(typ, ValueType):
            mapping = {
                KRPC.Type.DOUBLE: 'double',
                KRPC.Type.FLOAT: 'float',
                KRPC.Type.SINT32: 'sint32',
                KRPC.Type.SINT64: 'sint64',
                KRPC.Type.UINT32: 'uint32',
                KRPC.Type.UINT64: 'uint64',
                KRPC.Type.BOOL: 'bool',
                KRPC.Type.STRING: 'string',
                KRPC.Type.BYTES: 'bytes'
            }
            return 'self._client._types.%s_type' % \
                mapping[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return 'self._client._types.%s_type' % \
                snake_case(typ.python_type.__name__)
        if isinstance(typ, ClassType):
            return 'self._client._types.class_type("%s", "%s")' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        if isinstance(typ, EnumerationType):
            return 'self._client._types.enumeration_type("%s", "%s")' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        if isinstance(typ, TupleType):
            return 'self._client._types.tuple_type(%s)' % \
                ', '.join(self.parse_python_type(x) for x in typ.value_types)
        if isinstance(typ, ListType):
            return 'self._client._types.list_type(%s)' % \
                self.parse_python_type(typ.value_type)
        if isinstance(typ, SetType):
            return 'self._client._types.set_type(%s)' % \
                self.parse_python_type(typ.value_type)
        if isinstance(typ, DictionaryType):
            return 'self._client._types.dictionary_type(%s, %s)' % \
                (self.parse_python_type(typ.key_type),
                 self.parse_python_type(typ.value_type))
        raise RuntimeError('Unknown type ' + typ)

    def parse_type_specification(self, typ, is_nullable=False):
        if typ is None:
            spec = 'None'
        elif isinstance(typ, ValueType):
            spec = self.language.parse_type(typ)
        elif isinstance(typ, MessageType):
            if typ.python_type == KRPC.Event:
                spec = 'Event'
            else:
                return 'KRPC_pb2.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            spec = typ.protobuf_type.name
            if typ.protobuf_type.service != self.service_name:
                spec = typ.protobuf_type.service.lower() + "." + spec
        elif isinstance(typ, TupleType):
            spec = 'Tuple[%s]' % \
                ','.join(self.parse_type_specification(t)
                         for t in typ.value_types)
        elif isinstance(typ, ListType):
            spec = 'List[%s]' % \
                self.parse_type_specification(typ.value_type)
        elif isinstance(typ, SetType):
            spec = 'Set[%s]' % \
                self.parse_type_specification(typ.value_type)
        elif isinstance(typ, DictionaryType):
            spec = 'Dict[%s, %s]' % \
                (self.parse_type_specification(typ.key_type),
                 self.parse_type_specification(typ.value_type))
        else:
            raise RuntimeError('Unknown type ' + typ)
        if is_nullable:
            return 'Optional[%s]' % spec
        return spec

    def parse_context(self, context):
        # Expand service properties into get and set methods
        properties = collections.OrderedDict()
        for name, info in context['properties'].items():
            if name not in properties:
                properties[name] = {}

            if info['getter']:
                properties[name]['getter'] = {
                    'procedure': info['getter']['procedure'],
                    'remote_name': info['getter']['remote_name'],
                    'parameters': [],
                    'return_type': info['type'],
                    'documentation': info['documentation']
                }
            if info['setter']:
                properties[name]['setter'] = {
                    'procedure': info['setter']['procedure'],
                    'remote_name': info['setter']['remote_name'],
                    'parameters': self.generate_context_parameters(
                        info['setter']['procedure']),
                    'return_type': 'None',
                    'documentation': info['documentation']
                }
        context['properties'] = properties

        # Expand class properties into get and set methods
        for class_info in context['classes'].values():
            class_properties = collections.OrderedDict()
            for name, info in class_info['properties'].items():
                if name not in class_properties:
                    class_properties[name] = {}
                if info['getter']:
                    class_properties[name]['getter'] = {
                        'procedure': info['getter']['procedure'],
                        'remote_name': info['getter']['remote_name'],
                        'parameters': [],
                        'return_type': info['type'],
                        'documentation': info['documentation']
                    }
                if info['setter']:
                    class_properties[name]['setter'] = {
                        'procedure': info['setter']['procedure'],
                        'remote_name': info['setter']['remote_name'],
                        'parameters': [
                            self.generate_context_parameters(
                                info['setter']['procedure'])[1]],
                        'return_type': 'None',
                        'documentation': info['documentation']
                    }
            class_info['properties'] = class_properties

        # Find all service dependencies
        dependencies = set()
        procedures = \
            list(context['procedures'].values()) + \
            [procedure
             for property in context['properties'].values()
             for procedure in property.values()] + \
            list(itertools.chain(
                *[class_info['static_methods'].values()
                  for class_info in context['classes'].values()]))
        for info in procedures:
            if 'return_type' in info['procedure']:
                rtype = info['procedure']['return_type']
                if 'service' in rtype \
                        and context['service_name'] != rtype['service']:
                    dependencies.add(rtype['service'])
            for i, pinfo in enumerate(info['parameters']):
                ptype = info['procedure']['parameters'][i]['type']
                if 'service' in ptype \
                        and context['service_name'] != ptype['service']:
                    dependencies.add(ptype['service'])

        for class_info in context['classes'].values():
            items = list(class_info['methods'].values()) + \
                    [procedure
                     for property in class_info['properties'].values()
                     for procedure in property.values()]
            for info in items:
                if 'return_type' in info['procedure']:
                    rtype = info['procedure']['return_type']
                    if 'service' in rtype \
                            and context['service_name'] != rtype['service']:
                        dependencies.add(rtype['service'])
                for i, pinfo in enumerate(info['parameters']):
                    ptype = info['procedure']['parameters'][i]['type']
                    if 'service' in ptype \
                            and context['service_name'] != ptype['service']:
                        dependencies.add(ptype['service'])
        context['dependencies'] = dependencies

        # Add type specifications to types
        procedures = \
            list(context['procedures'].values()) + \
            [procedure
             for property in context['properties'].values()
             for _, procedure in property.items()] + \
            list(itertools.chain(
                *[class_info['static_methods'].values()
                  for class_info in context['classes'].values()]))
        for info in procedures:
            info['return_type'] = {
                'name': info['return_type'],
                'python_type': self.parse_python_type(
                    self.get_return_type(info['procedure'])
                ),
                'spec': self.parse_type_specification(
                    self.get_return_type(info['procedure']),
                    info['procedure'].get('return_is_nullable', False))
            }
            pos = 0
            for i, pinfo in enumerate(info['parameters']):
                ptype = as_type(
                    self.types, info['procedure']['parameters'][i]['type'])
                nullable = info['procedure']['parameters'][i].get('nullable', False)
                pinfo['type'] = {
                    'name': pinfo['type'],
                    'python_type': self.parse_python_type(ptype),
                    'spec': self.parse_type_specification(ptype, nullable)
                }
                pos += 1

        for class_info in context['classes'].values():
            items = list(class_info['methods'].values()) + \
                    [procedure
                     for property in class_info['properties'].values()
                     for _, procedure in property.items()]
            for info in items:
                info['return_type'] = {
                    'name': info['return_type'],
                    'python_type': self.parse_python_type(
                        self.get_return_type(info['procedure'])
                    ),
                    'spec': self.parse_type_specification(
                        self.get_return_type(info['procedure']),
                        info['procedure'].get('return_is_nullable', False))
                }
                pos = 0
                for i, pinfo in enumerate(info['parameters']):
                    ptype = as_type(
                        self.types,
                        info['procedure']['parameters'][i+1]['type'])
                    nullable = info['procedure']['parameters'][i+1].get('nullable', False)
                    pinfo['type'] = {
                        'name': pinfo['type'],
                        'python_type': self.parse_python_type(ptype),
                        'spec': self.parse_type_specification(ptype, nullable)
                    }
                    pos += 1

        return context

    def parse_default_value(self, value, typ):
        result = super().parse_default_value(value, typ)
        # Fix references to types within the same service
        prefix = self.service_name + "."
        if result.startswith(prefix):
            result = result[len(prefix):]
        return result

    @staticmethod
    def parse_documentation(documentation):
        documentation = PythonDocParser().parse(documentation)
        if len(documentation) == 0:
            return ''
        return '"""\n' + documentation + '\n"""'


class PythonDocParser(DocParser):
    language = PythonLanguage()

    def parse_summary(self, node):
        return self.parse_node(node).strip('\n')

    def parse_remarks(self, node):
        return '\n\n'+self.parse_node(node).strip('\n')

    def parse_param(self, node):
        name = ':param %s: ' % self.language.parse_name(node.attrib['name'])
        desc = self.parse_node(node, indent=len(name))[len(name):]
        if len(desc) == 0:
            return ''
        return '\n\n' + name + desc

    def parse_returns(self, node):
        name = ':returns: '
        desc = self.parse_node(node, indent=len(name))[len(name):]
        if len(desc) == 0:
            return ''
        return '\n\n' + name + desc

    def parse_see(self, node):
        return self.parse_cref(node.attrib['cref'])

    def parse_paramref(self, node):
        return self.language.parse_name(node.attrib['name'])

    @staticmethod
    def parse_a(node):
        return "`%s <%s>`_" % \
            (node.text.replace('\n', ' '), node.attrib['href'])

    def parse_c(self, node):
        code = node.text
        code = self.language.value_map.get(code, code)
        return '``%s``' % code

    @staticmethod
    def parse_math(node):
        return '``%s``' % node.text

    def parse_list(self, node):
        lines = [
            ' * ' + self.parse_node(item[0], indent=3)[3:].rstrip()
            for item in node
        ]
        content = '\n'.join(lines)
        return content

    @staticmethod
    def parse_cref(cref):
        # FIXME: is this correct?
        if cref[0] == 'M':
            cref = cref[2:].split('.')
            member = lower_camel_case(cref[-1])
            del cref[-1]
            return '.'.join(cref)+'#'+member
        if cref[0] == 'T':
            return cref[2:]
        raise RuntimeError('Unknown cref \'%s\'' % cref)
