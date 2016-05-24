from xml.etree import ElementTree

class DocParser(object):

    def parse(self, xml):
        if xml.strip() == '':
            return ''
        parser = ElementTree.XMLParser(encoding='UTF-8')
        root = ElementTree.XML(xml.encode('UTF-8'), parser=parser)
        return self.parse_root(root)

    def parse_root(self, node):
        content = ''
        for child in node:
            content += self.inner_parse_node(child)
        return content

    def parse_node(self, node, indent=0):
        content = ''
        if node.text:
            content += node.text
        for child in node:
            content += self.inner_parse_node(child)
            if child.tail:
                content += child.tail
        if indent == 0:
            return content
        else:
            return self.indent(content, indent)

    def inner_parse_node(self, node):
        if not hasattr(self, 'parse_'+node.tag):
            raise RuntimeError('Failed to parse node \'%s\'' % node.tag)
        return getattr(self, 'parse_'+node.tag)(node)

    @staticmethod
    def indent(string, width):
        lines = string.split('\n')
        for i, line in enumerate(lines):
            if len(line.strip()) > 0:
                lines[i] = (' '*width) + line
        return '\n'.join(lines).strip('\n')
