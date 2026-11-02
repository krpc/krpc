import jinja2.ext
import jinja2.nodes


class AppendExtension(jinja2.ext.Extension):
    tags = set(["append"])

    def __init__(self, environment):
        super().__init__(environment)
        # The nodes a render appended to, for the caller to clear
        self.appended_nodes = []

    def parse(self, parser):
        lineno = next(parser.stream).lineno
        args = [parser.parse_expression()]
        body = parser.parse_statements(("name:endappend",), drop_needle=True)
        return jinja2.nodes.CallBlock(
            self.call_method("_append_support", args), [], [], body
        ).set_lineno(lineno)

    def _append_support(self, obj, caller):
        obj.append(caller())
        self.appended_nodes.append(obj)
        return ""
