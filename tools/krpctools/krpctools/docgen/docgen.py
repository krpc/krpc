import xml.etree.ElementTree as ElementTree
from ..utils import indent
from .utils import lookup_cref


class DocumentationGenerator(object):
    def __init__(self, domain, services, xml):
        self.domain = domain
        self.services = services
        if xml.strip() == '':
            self.root = None
        else:
            parser = ElementTree.XMLParser(encoding='UTF-8')
            self.root = ElementTree.XML(xml.encode('UTF-8'), parser=parser)

    def generate(self, path='./summary'):
        if self.root is None:
            return ''
        node = self.root.find(path)
        if node is None:
            return ''
        return self._generate(node)

    def has(self, path='./summary'):
        if self.root is None:
            return False
        node = self.root.find(path)
        return node is not None \
            and node.text is not None \
            and node.text.strip() != ''

    def _generate(self, node):
        content = node.text or ''
        for child in node:
            content += self._generate_node(child)
            if child.tail:
                content += child.tail
        return content.strip()

    def _generate_node(self, node):
        if node.tag == 'see':
            return self.domain.see(
                lookup_cref(node.attrib['cref'], self.services))
        elif node.tag == 'paramref':
            return self.domain.paramref(node.attrib['name'])
        elif node.tag == 'a':
            return '`%s <%s>`_' % \
                (node.text.replace('\n', ' ').strip(), node.attrib['href'])
        elif node.tag == 'c':
            return self.domain.code(node.text)
        elif node.tag == 'math':
            return self.domain.math(node.text)
        elif node.tag == 'list':
            content = ['* %s\n' %
                       indent(self._generate(item[0]), width=2)[2:].rstrip()
                       for item in node]
            return '\n'+''.join(content)
        else:
            raise RuntimeError('Unknown node \'%s\'' % node.tag)
