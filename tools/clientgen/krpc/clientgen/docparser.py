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

    def indent(self, s, width):
        lines = s.split('\n')
        for i in range(len(lines)):
            if len(lines[i].strip()) > 0:
                lines[i] = (' '*width) + lines[i]
        return '\n'.join(lines).strip('\n')
