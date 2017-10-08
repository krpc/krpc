import jinja2.ext
import jinja2.nodes


class AppendExtension(jinja2.ext.Extension):
    tags = set(['append'])

    def parse(self, parser):
        lineno = next(parser.stream).lineno
        args = [parser.parse_expression()]
        body = parser.parse_statements(['name:endappend'], drop_needle=True)
        return jinja2.nodes.CallBlock(
            self.call_method('_append_support', args),
            [], [], body).set_lineno(lineno)

    @staticmethod
    def _append_support(obj, caller):
        obj.append(caller())
        return ''
