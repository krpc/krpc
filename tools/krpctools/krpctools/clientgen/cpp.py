import collections

from krpc.types import ClassType
from krpc.utils import snake_case

from ..lang.cpp import CppLanguage
from . import crefs
from .docparser import DocParser
from .generator import Generator


def _cpp_template_fix(typ):
    """Ensure nested templates are separated by spaces for the C++ parser"""
    return typ[:-2] + "> >" if typ.endswith(">>") else typ


class CppGenerator(Generator[CppLanguage]):
    language = CppLanguage()
    includes_services = True

    plain_cref_separator = "::"

    @staticmethod
    def parse_plain_cref_member(name):
        return snake_case(name)

    def parse_set_client(self, procedure):
        return isinstance(self.get_return_type(procedure), ClassType)

    def parse_type(self, typ):
        return _cpp_template_fix(self.language.parse_type(typ))

    def parse_documentation(self, documentation):
        documentation = CppDocParser(self.parse_cref).parse(documentation)
        if documentation == "":
            return ""
        lines = ["/**"] + [" * " + line for line in documentation.split("\n")] + [" */"]
        return "\n".join(line.rstrip() for line in lines)

    def format_cref(self, target):
        # Fully qualified, so that Doxygen links it from the header of any service
        name = "krpc::services::" + target.service
        if target.type is not None:
            name += "::" + target.type
        if target.member is None:
            return name
        member = self.language.parse_name(target.member)
        if not target.getter:
            member = "set_" + member
        if target.kind in (crefs.ENUMERATION_VALUE, crefs.STRUCT_FIELD):
            return name + "::" + member
        return name + "::" + member + "()"

    def parse_context(self, context):
        for info in context["procedures"].values():
            info["return_set_client"] = self.parse_set_client(info["procedure"])

        properties = collections.OrderedDict()
        for name, info in context["properties"].items():
            if info["getter"]:
                properties[name] = {
                    "remote_name": info["getter"]["remote_name"],
                    "parameters": [],
                    "return_type": info["type"],
                    "return_set_client": self.parse_set_client(
                        info["getter"]["procedure"]
                    ),
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
            if info["setter"]:
                properties["set_" + name] = {
                    "remote_name": info["setter"]["remote_name"],
                    "parameters": self.generate_context_parameters(
                        info["setter"]["remote_name"], info["setter"]["procedure"]
                    ),
                    "return_type": "void",
                    "return_set_client": False,
                    "documentation": info["setter_documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }

        for class_info in context["classes"].values():
            for info in class_info["methods"].values():
                info["return_set_client"] = self.parse_set_client(info["procedure"])

            for info in class_info["static_methods"].values():
                info["return_set_client"] = self.parse_set_client(info["procedure"])
                self.add_client_param_doxygen(info)

            class_properties = collections.OrderedDict()
            for name, info in class_info["properties"].items():
                if info["getter"]:
                    class_properties[name] = {
                        "remote_name": info["getter"]["remote_name"],
                        "parameters": [],
                        "return_type": info["type"],
                        "return_set_client_fn": self.parse_set_client(
                            info["getter"]["procedure"]
                        ),
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
                if info["setter"]:
                    class_properties["set_" + name] = {
                        "remote_name": info["setter"]["remote_name"],
                        "parameters": [
                            self.generate_context_parameters(
                                info["setter"]["remote_name"],
                                info["setter"]["procedure"],
                            )[1]
                        ],
                        "return_type": "void",
                        "return_set_client": False,
                        "documentation": info["setter_documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
            class_info["properties"] = class_properties

        context["properties"] = properties

        # Append a \deprecated doxygen tag for every deprecated member/type
        deprecatable = (
            list(context["procedures"].values())
            + list(context["properties"].values())
            + list(context["enumerations"].values())
            + list(context["structs"].values())
            + list(context["exceptions"].values())
            + list(context["classes"].values())
        )
        for enm in context["enumerations"].values():
            deprecatable += list(enm["values"])
        for struct_info in context["structs"].values():
            deprecatable += list(struct_info["fields"])
        for class_info in context["classes"].values():
            deprecatable += (
                list(class_info["methods"].values())
                + list(class_info["static_methods"].values())
                + list(class_info["properties"].values())
            )
        for info in deprecatable:
            self.add_deprecated_doxygen(info)

        return context

    @staticmethod
    def add_client_param_doxygen(info):
        """Document the client a static method takes ahead of its parameters, where its
        documentation documents any parameters"""
        lines = info.get("documentation", "").split("\n")
        params = [i for i, line in enumerate(lines) if line.startswith(" * @param ")]
        if not params:
            return
        lines.insert(params[0], " * @param client A client object.")
        info["documentation"] = "\n".join(lines)

    @staticmethod
    def add_deprecated_doxygen(info):
        if not info.get("deprecated") or not info.get("deprecated_reason"):
            return
        tag = " * \\deprecated " + info["deprecated_reason"]
        documentation = info.get("documentation", "")
        if documentation:
            lines = documentation.split("\n")
            # lines[-1] is the closing ' */'
            info["documentation"] = "\n".join(lines[:-1] + [" *", tag, " */"])
        else:
            info["documentation"] = "\n".join(["/**", tag, " */"])


class CppDocParser(DocParser):
    language = CppLanguage()

    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return "\n\n" + self.parse_node(node).strip()

    def parse_param(self, node):
        name = self.language.parse_name(node.attrib["name"])
        return "\n@param %s %s" % (name, self.parse_node(node).strip())

    def parse_returns(self, node):
        return "\n@return %s" % self.parse_node(node).strip()

    def parse_paramref(self, node):
        return self.language.parse_name(node.attrib["name"])

    @staticmethod
    def parse_a(node):
        return '<a href="%s">%s</a>' % (node.attrib["href"], node.text)

    @staticmethod
    def parse_c(node):
        # A Markdown code span, in which Doxygen reads no commands
        return "`%s`" % node.text

    def parse_list(self, node):
        content = [
            "- %s\n" % self.parse_node(item[0], indent=2)[2:].rstrip() for item in node
        ]
        return "\n" + "".join(content)
