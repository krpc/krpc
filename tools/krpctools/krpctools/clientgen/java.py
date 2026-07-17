import collections
import hashlib
import itertools

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
from .generator import Generator
from .docparser import DocParser
from ..utils import lower_camel_case, upper_camel_case, as_type
from ..lang.java import JavaLanguage


class JavaGenerator(Generator):

    language = JavaLanguage()

    parse_plain_cref_member = staticmethod(lower_camel_case)

    def parse_type_specification(self, typ):
        if typ is None:
            return None
        if isinstance(typ, ValueType):
            return (
                "krpc.client.Types.createValue("
                + "krpc.schema.KRPC.Type.TypeCode.%s)"
                % Type.TypeCode.Name(typ.protobuf_type.code)
            )
        if isinstance(typ, MessageType):
            return (
                "krpc.client.Types.createMessage("
                + "krpc.schema.KRPC.Type.TypeCode.%s)"
                % Type.TypeCode.Name(typ.protobuf_type.code)
            )
        if isinstance(typ, ClassType):
            return 'krpc.client.Types.createClass("%s", "%s")' % (
                typ.protobuf_type.service,
                typ.protobuf_type.name,
            )
        if isinstance(typ, EnumerationType):
            return 'krpc.client.Types.createEnumeration("%s", "%s")' % (
                typ.protobuf_type.service,
                typ.protobuf_type.name,
            )
        if isinstance(typ, TupleType):
            return "krpc.client.Types.createTuple(%s)" % ",".join(
                self.parse_type_specification(t) for t in typ.value_types
            )
        if isinstance(typ, ListType):
            return "krpc.client.Types.createList(%s)" % self.parse_type_specification(
                typ.value_type
            )
        if isinstance(typ, SetType):
            return "krpc.client.Types.createSet(%s)" % self.parse_type_specification(
                typ.value_type
            )
        if isinstance(typ, DictionaryType):
            return "krpc.client.Types.createDictionary(%s, %s)" % (
                self.parse_type_specification(typ.key_type),
                self.parse_type_specification(typ.value_type),
            )
        raise RuntimeError("Unknown type " + typ)

    @staticmethod
    def parse_documentation(documentation):
        documentation = JavaDocParser().parse(documentation)
        if documentation == "":
            return ""
        lines = ["/**"] + [" * " + line for line in documentation.split("\n")] + [" */"]
        return "\n".join(line.rstrip() for line in lines)

    def parse_context(self, context):
        # Expand service properties into get and set methods
        properties = collections.OrderedDict()
        for name, info in context["properties"].items():
            if info["getter"]:
                properties["get" + upper_camel_case(name)] = {
                    "procedure": info["getter"]["procedure"],
                    "remote_name": info["getter"]["remote_name"],
                    "parameters": [],
                    "return_type": info["type"],
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
            if info["setter"]:
                properties["set" + upper_camel_case(name)] = {
                    "procedure": info["setter"]["procedure"],
                    "remote_name": info["setter"]["remote_name"],
                    "parameters": self.generate_context_parameters(
                        info["setter"]["procedure"]
                    ),
                    "return_type": "void",
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
        context["properties"] = properties

        # Expand class properties into get and set methods
        for class_name, class_info in context["classes"].items():
            class_properties = collections.OrderedDict()
            for name, info in class_info["properties"].items():
                if info["getter"]:
                    class_properties["get" + upper_camel_case(name)] = {
                        "procedure": info["getter"]["procedure"],
                        "remote_name": info["getter"]["remote_name"],
                        "parameters": [],
                        "return_type": info["type"],
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
                if info["setter"]:
                    class_properties["set" + upper_camel_case(name)] = {
                        "procedure": info["setter"]["procedure"],
                        "remote_name": info["setter"]["remote_name"],
                        "parameters": [
                            self.generate_context_parameters(
                                info["setter"]["procedure"]
                            )[1]
                        ],
                        "return_type": "void",
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
            class_info["properties"] = class_properties

        # Add type specifications to types
        procedures = (
            list(context["procedures"].values())
            + list(context["properties"].values())
            + list(
                itertools.chain(
                    *[
                        class_info["static_methods"].values()
                        for class_info in context["classes"].values()
                    ]
                )
            )
        )
        for info in procedures:
            info["return_type"] = {
                "name": info["return_type"],
                "spec": self.parse_type_specification(
                    self.get_return_type(info["procedure"])
                ),
            }
            pos = 0
            for i, pinfo in enumerate(info["parameters"]):
                param_type = as_type(
                    self.types, info["procedure"]["parameters"][i]["type"]
                )
                pinfo["type"] = {
                    "name": pinfo["type"],
                    "spec": self.parse_type_specification(param_type),
                }
                pos += 1

        for class_info in context["classes"].values():
            items = list(class_info["methods"].values()) + list(
                class_info["properties"].values()
            )
            for info in items:
                info["return_type"] = {
                    "name": info["return_type"],
                    "spec": self.parse_type_specification(
                        self.get_return_type(info["procedure"])
                    ),
                }
                pos = 0
                for i, pinfo in enumerate(info["parameters"]):
                    param_type = as_type(
                        self.types, info["procedure"]["parameters"][i + 1]["type"]
                    )
                    pinfo["type"] = {
                        "name": pinfo["type"],
                        "spec": self.parse_type_specification(param_type),
                    }
                    pos += 1

        # Make enumeration members UPPER_SNAKE_CASE
        for enm in context["enumerations"].values():
            for value in enm["values"]:
                value["name"] = self.language.parse_const_name(value["name"])

        # Add serial version UIDs to classes
        items = list(context["classes"].items()) + list(context["exceptions"].items())
        for class_name, cls in items:
            tohash = self.service_name + "." + class_name
            hsh = hashlib.sha1(tohash.encode("utf-8")).hexdigest()
            cls["serial_version_uid"] = int(hsh, 16) % (10**18)

        # Append an @deprecated javadoc tag for every deprecated member/type
        deprecatable = (
            list(context["procedures"].values())
            + list(context["properties"].values())
            + list(context["enumerations"].values())
            + list(context["exceptions"].values())
            + list(context["classes"].values())
        )
        for enm in context["enumerations"].values():
            deprecatable += list(enm["values"])
        for cls in context["classes"].values():
            deprecatable += (
                list(cls["methods"].values())
                + list(cls["static_methods"].values())
                + list(cls["properties"].values())
            )
        for info in deprecatable:
            self.add_deprecated_javadoc(info)

        context["types_table"] = self.build_types_table(context)

        return context

    def build_types_table(self, context):
        # Flat table of every procedure's return and parameter type specs, used
        # to populate the _Types lookup maps. Instance members take the owning
        # class as their leading parameter; static members do not.
        table = [
            self.make_types_entry(info)
            for info in list(context["procedures"].values())
            + list(context["properties"].values())
        ]
        for class_name, class_info in context["classes"].items():
            for info in list(class_info["methods"].values()) + list(
                class_info["properties"].values()
            ):
                table.append(self.make_types_entry(info, class_name))
            for info in class_info["static_methods"].values():
                table.append(self.make_types_entry(info))
        return table

    def make_types_entry(self, info, class_name=None):
        parameter_specs = []
        if class_name is not None:
            parameter_specs.append(
                'krpc.client.Types.createClass("%s", "%s")'
                % (self.service_name, class_name)
            )
        parameter_specs += [p["type"]["spec"] for p in info["parameters"]]
        return {
            "remote_name": info["remote_name"],
            "return_spec": (
                info["return_type"]["spec"]
                if info["return_type"]["name"] != "void"
                else None
            ),
            "parameter_specs": parameter_specs,
        }

    @staticmethod
    def add_deprecated_javadoc(info):
        if not info.get("deprecated") or not info.get("deprecated_reason"):
            return
        tag = " * @deprecated " + info["deprecated_reason"]
        documentation = info.get("documentation", "")
        if documentation:
            lines = documentation.split("\n")
            # lines[-1] is the closing ' */'
            info["documentation"] = "\n".join(lines[:-1] + [" *", tag, " */"])
        else:
            info["documentation"] = "\n".join(["/**", tag, " */"])


class JavaDocParser(DocParser):

    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return "\n\n" + self.parse_node(node).strip()

    def parse_param(self, node):
        return "\n@param %s %s" % (node.attrib["name"], self.parse_node(node).strip())

    def parse_returns(self, node):
        return "\n@return %s" % self.parse_node(node).strip()

    def parse_see(self, node):
        return "{@link %s}" % self.parse_cref(node.attrib["cref"])

    @staticmethod
    def parse_paramref(node):
        return node.attrib["name"]

    @staticmethod
    def parse_a(node):
        return '<a href="%s">%s</a>' % (node.attrib["href"], node.text)

    @staticmethod
    def parse_c(node):
        return "{@code %s}" % node.text

    @staticmethod
    def parse_math(node):
        return node.text

    def parse_list(self, node):
        content = [
            "<li>%s\n" % self.parse_node(item[0], indent=2)[2:].rstrip()
            for item in node
        ]
        return "<p><ul>" + "\n" + "".join(content) + "</ul></p>"

    @staticmethod
    def parse_cref(cref):
        if cref[0] == "M":
            cref = cref[2:].split(".")
            member = lower_camel_case(cref[-1])
            del cref[-1]
            return ".".join(cref) + "#" + member
        if cref[0] == "T":
            return cref[2:]
        raise RuntimeError("Unknown cref '%s'" % cref)
