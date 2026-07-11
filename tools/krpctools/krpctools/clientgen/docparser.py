from xml.etree import ElementTree


def flatten_deprecation_reason(reason, parse_cref):
    """Flatten a deprecation reason fragment to plain text, mapping
    <see cref="..."/> references through parse_cref. Reasons containing
    no markup are returned unchanged."""
    if "<" not in reason:
        return reason
    parser = ElementTree.XMLParser(encoding="UTF-8")
    root = ElementTree.XML(("<doc>%s</doc>" % reason).encode("UTF-8"), parser=parser)
    return _flatten_node(root, parse_cref)


def _flatten_node(node, parse_cref):
    content = node.text or ""
    for child in node:
        if child.tag == "see":
            content += parse_cref(child.attrib["cref"])
        elif child.tag == "paramref":
            content += child.attrib["name"]
        else:
            content += _flatten_node(child, parse_cref)
        if child.tail:
            content += child.tail
    return content


class DocParser:

    def parse(self, xml):
        if xml.strip() == "":
            return ""
        parser = ElementTree.XMLParser(encoding="UTF-8")
        root = ElementTree.XML(xml.encode("UTF-8"), parser=parser)
        return self.parse_root(root)

    def parse_root(self, node):
        content = ""
        for child in node:
            content += self.inner_parse_node(child)
        return content

    def parse_node(self, node, indent=0):
        content = ""
        if node.text:
            content += node.text
        for child in node:
            content += self.inner_parse_node(child)
            if child.tail:
                content += child.tail
        return content if indent == 0 else self.indent(content, indent)

    def inner_parse_node(self, node):
        if not hasattr(self, "parse_" + node.tag):
            raise RuntimeError("Failed to parse node '%s'" % node.tag)
        return getattr(self, "parse_" + node.tag)(node)

    @staticmethod
    def indent(string, width):
        lines = string.split("\n")
        for i, line in enumerate(lines):
            if line.strip():
                lines[i] = (" " * width) + line
        return "\n".join(lines).strip("\n")
