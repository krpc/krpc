import collections
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser
from ..lang.cnano import CnanoLanguage


class CnanoGenerator(Generator):

    language = CnanoLanguage()

    def __init__(self, macro_template, service, definitions):
        super(CnanoGenerator, self).__init__(
            macro_template, service, definitions)
        self._collection_types = set()

    def parse_name(self, name):
        if name in self.language.keywords:
            return '%s_' % name
        return name

    def parse_type(self, typ):
        return self._parse_type(typ)

    def _parse_type(self, typ, in_collection=False):
        ptr = True
        self._collection_types.add(typ)
        if isinstance(typ, ValueType):
            ctype = self.language.type_map[typ.protobuf_type.code]
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
            if in_collection:
                if isinstance(typ, ClassType):
                    ctype = 'krpc_object_t'
                else:
                    ctype = 'krpc_enum_t'
            else:
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

    def parse_collection_type(self, typ):
        result = self.parse_type(typ)
        if isinstance(typ, TupleType):
            result.update({
                'value_types': [
                    self._parse_type(t, in_collection=True)
                    for t in typ.value_types]
            })
        elif isinstance(typ, (ListType, SetType)):
            result.update({
                'value_type': self._parse_type(
                    typ.value_type, in_collection=True)
            })
        elif isinstance(typ, DictionaryType):
            result.update({
                'key_type': self._parse_type(
                    typ.key_type, in_collection=True),
                'value_type': self._parse_type(
                    typ.value_type, in_collection=True)
            })
        else:
            raise RuntimeError('Unknown type ' + str(typ))
        return result

    def parse_type_name(self, typ):
        if isinstance(typ, ValueType):
            return self.language.type_name_map[typ.protobuf_type.code]
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

    def parse_return_type(self, typ):
        if typ is None:
            return {'ctype': 'void', 'cvtype': None, 'name': None}
        typ = self.parse_type(typ)
        typ['cvtype'] = typ['ctype'] + ' *'
        typ['ccvtype'] = typ['cvtype']
        return typ

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    @staticmethod
    def parse_default_value(value, typ):  # pylint: disable=unused-argument
        # No default arguments in C
        return None

    @staticmethod
    def parse_documentation(documentation):
        documentation = CNanoDocParser().parse(documentation)
        if documentation == '':
            return ''
        lines = ['/**'] + [' * ' + line
                           for line in documentation.split('\n')] + [' */']
        return '\n'.join(line.rstrip() for line in lines)

    def parse_context(self, context):
        def return_type(typ):
            typ['cvtype'] = typ['ctype'] + ' *'
            typ['ccvtype'] = typ['cvtype']
            return typ

        properties = collections.OrderedDict()
        for name, info in context['properties'].items():
            if info['getter']:
                properties[name] = {
                    'remote_id': info['getter']['remote_id'],
                    'parameters': [],
                    'return_type': return_type(info['type']),
                    'documentation': info['documentation']
                }
            if info['setter']:
                properties['set_'+name] = {
                    'remote_id': info['setter']['remote_id'],
                    'parameters': self.generate_context_parameters(
                        info['setter']['procedure']),
                    'return_type': {
                        'ctype': 'void', 'cvtype': None, 'name': None},
                    'documentation': info['documentation']
                }

        for _, class_info in context['classes'].items():
            class_properties = collections.OrderedDict()
            for name, info in class_info['properties'].items():
                if info['getter']:
                    class_properties[name] = {
                        'remote_id': info['getter']['remote_id'],
                        'parameters': [],
                        'return_type': return_type(info['type']),
                        'documentation': info['documentation']
                    }
                if info['setter']:
                    class_properties['set_'+name] = {
                        'remote_id': info['setter']['remote_id'],
                        'parameters': [self.generate_context_parameters(
                            info['setter']['procedure'])[1]],
                        'return_type': {
                            'ctype': 'void', 'cvtype': None, 'name': None},
                        'return_set_client': False,
                        'documentation': info['documentation']
                    }
            class_info['properties'] = class_properties

        context['properties'] = properties

        context['collection_types'] = []

        def build_collection_types(typ):
            if isinstance(typ, TupleType):
                for value_type in typ.value_types:
                    build_collection_types(value_type)
            elif isinstance(typ, (ListType, SetType)):
                build_collection_types(typ.value_type)
            elif isinstance(typ, DictionaryType):
                build_collection_types(typ.key_type)
                build_collection_types(typ.value_type)
            else:
                return
            typ = self.parse_collection_type(typ)
            if typ not in context['collection_types']:
                context['collection_types'].append(typ)

        for typ in sorted(self._collection_types, key=self.parse_type_name):
            build_collection_types(typ)

        return context


class CNanoDocParser(DocParser):

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
