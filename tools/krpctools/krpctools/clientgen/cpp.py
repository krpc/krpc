import collections
from krpc.types import ClassType
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser
from ..lang.cpp import CppLanguage


def _cpp_template_fix(typ):
    """ Ensure nested templates are separated by spaces for the C++ parser """
    return typ[:-2] + '> >' if typ.endswith('>>') else typ


class CppGenerator(Generator):

    language = CppLanguage()

    def parse_set_client(self, procedure):
        return isinstance(self.get_return_type(procedure), ClassType)

    def parse_type(self, typ):
        return _cpp_template_fix(self.language.parse_type(typ))

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

        properties = collections.OrderedDict()
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

        for class_info in context['classes'].values():
            for info in class_info['methods'].values():
                info['return_set_client'] = self.parse_set_client(
                    info['procedure'])

            for info in class_info['static_methods'].values():
                info['return_set_client'] = self.parse_set_client(
                    info['procedure'])

            class_properties = collections.OrderedDict()
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
