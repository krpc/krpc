import warnings
from xml.etree import ElementTree

from krpc.docmath import problem, to_text


class DocumentationMarkupWarning(UserWarning):
    """Documentation markup that docgen rejects, which clientgen renders as text."""


def warn_markup(message, xml):
    """Warn about documentation markup that docgen rejects, in the given documentation"""
    context = " ".join(xml.split())
    if len(context) > 120:
        context = context[:117] + "..."
    warnings.warn(
        "Invalid documentation markup: %s, in %s" % (message, context),
        DocumentationMarkupWarning,
        stacklevel=2,
    )


def render_markup(tag, source, xml):
    """The content of a <math> or <unit> element as text, warning if it is invalid."""
    unit = tag == "unit"
    message = problem(source, unit)
    if message is not None:
        warn_markup(message, xml)
    return to_text(source, unit)


def flatten_deprecation_reason(reason, parse_cref):
    """Flatten a deprecation reason fragment to plain text, mapping
    <see cref="..."/> references through parse_cref. Reasons containing
    no markup are returned unchanged."""
    if "<" not in reason:
        return reason
    parser = ElementTree.XMLParser(encoding="UTF-8")
    root = ElementTree.XML(("<doc>%s</doc>" % reason).encode("UTF-8"), parser=parser)
    return _flatten_node(root, parse_cref, reason)


def _flatten_node(node, parse_cref, reason):
    content = node.text or ""
    for child in node:
        if child.tag == "see":
            content += parse_cref(child.attrib["cref"])
        elif child.tag == "paramref":
            content += child.attrib["name"]
        elif child.tag in ("math", "unit"):
            content += render_markup(child.tag, child.text or "", reason)
        else:
            content += _flatten_node(child, parse_cref, reason)
        if child.tail:
            content += child.tail
    return content


class DocParser:
    _xml = ""

    def __init__(self, parse_cref):
        # Maps a cref to a reference, or to None if it names nothing the definitions define
        self._parse_cref = parse_cref

    def parse(self, xml):
        if xml.strip() == "":
            return ""
        parser = ElementTree.XMLParser(encoding="UTF-8")
        root = ElementTree.XML(xml.encode("UTF-8"), parser=parser)
        self._xml = xml
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
        # An unknown tag is rendered as its content
        if not hasattr(self, "parse_" + node.tag):
            warn_markup("unknown tag '%s'" % node.tag, self._xml)
            return self.parse_node(node)
        return getattr(self, "parse_" + node.tag)(node)

    def parse_see(self, node):
        cref = node.attrib["cref"]
        reference = self._parse_cref(cref)
        if reference is None:
            warn_markup("unknown cref '%s'" % cref, self._xml)
            return cref[2:]
        return reference

    def parse_math(self, node):
        return render_markup("math", node.text or "", self._xml)

    def parse_unit(self, node):
        return render_markup("unit", node.text or "", self._xml)

    @staticmethod
    def indent(string, width):
        lines = string.split("\n")
        for i, line in enumerate(lines):
            if line.strip():
                lines[i] = (" " * width) + line
        return "\n".join(lines).strip("\n")
