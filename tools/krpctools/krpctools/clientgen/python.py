import collections
import itertools

from krpc.types import (
    ClassType,
    DictionaryType,
    EnumerationType,
    EventType,
    ListType,
    MessageType,
    SetType,
    StreamType,
    StructType,
    TupleType,
    ValueType,
)
from krpc.utils import snake_case

from ..lang.python import PythonLanguage
from ..utils import as_type
from . import crefs
from .docparser import DocParser
from .generator import Generator

# The module of each service whose module ships with the client, for a service that uses its
# types and is generated without an --include for it
SHIPPED_MODULES = {
    "KRPC": "krpc.services.krpc",
    "StdLib": "krpc.services.stdlib",
    "TestService": "krpc.services.testservice",
    "SpaceCenter": "krpc.ksp.spacecenter",
    "Drawing": "krpc.ksp.drawing",
    "UI": "krpc.ksp.ui",
    "InfernalRobotics": "krpc.ksp.infernalrobotics",
    "KerbalAlarmClock": "krpc.ksp.kerbalalarmclock",
    "RemoteTech": "krpc.ksp.remotetech",
    "LIDAR": "krpc.ksp.lidar",
    "DockingCamera": "krpc.ksp.dockingcamera",
    "Debug": "krpc.ksp.debug",
}


class StubLanguage(PythonLanguage):
    """How a generated stub module names the types it uses: another service's through the module
    its stubs are imported from, and its own service's directly.

    The module another service's stubs are imported from is aliased, so that a service whose name
    is also that of a module the stubs use, such as KRPC, does not hide it."""

    def __init__(self, service):
        super().__init__()
        self.module = service

    def parse_type(self, typ):
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            name = typ.protobuf_type.name
            if typ.protobuf_type.service != self.module:
                name = "_service_" + typ.protobuf_type.service.lower() + "." + name
            return name
        return super().parse_type(typ)


class PythonGenerator(Generator[StubLanguage]):
    @staticmethod
    def parse_plain_cref_member(name):
        return snake_case(name)

    def __init__(self, macro_template, service, definitions, includes=None):
        super().__init__(macro_template, service, definitions, includes)
        self.language = StubLanguage(service)

    def parse_type_specification(self, typ):
        if typ is None:
            return "None"
        if isinstance(typ, ValueType):
            spec = self.language.parse_type(typ)
        elif isinstance(typ, StreamType):
            spec = "int"
        elif isinstance(typ, EventType):
            spec = "Event"
        elif isinstance(typ, MessageType):
            spec = "KRPC_pb2.%s" % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType, StructType)):
            spec = self.language.parse_type(typ)
        elif isinstance(typ, TupleType):
            spec = "Tuple[%s]" % ",".join(
                self.parse_type_specification(t) for t in typ.value_types
            )
        elif isinstance(typ, ListType):
            spec = "List[%s]" % self.parse_type_specification(typ.value_type)
        elif isinstance(typ, SetType):
            spec = "Set[%s]" % self.parse_type_specification(typ.value_type)
        elif isinstance(typ, DictionaryType):
            spec = "Dict[%s, %s]" % (
                self.parse_type_specification(typ.key_type),
                self.parse_type_specification(typ.value_type),
            )
        else:
            raise RuntimeError("Unknown type " + typ)
        if typ.nullable:
            return "Optional[%s]" % spec
        return spec

    def parse_context(self, context):
        # Expand service properties into get and set methods
        properties = collections.OrderedDict()
        for name, info in context["properties"].items():
            if name not in properties:
                properties[name] = {}

            if info["getter"]:
                properties[name]["getter"] = {
                    "procedure": info["getter"]["procedure"],
                    "remote_name": info["getter"]["remote_name"],
                    "parameters": [],
                    "return_type": info["type"],
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
            if info["setter"]:
                properties[name]["setter"] = {
                    "procedure": info["setter"]["procedure"],
                    "remote_name": info["setter"]["remote_name"],
                    "parameters": self.generate_context_parameters(
                        info["setter"]["remote_name"], info["setter"]["procedure"]
                    ),
                    "return_type": "None",
                    "documentation": info["setter_documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
        context["properties"] = properties

        # Expand class properties into get and set methods
        for class_info in context["classes"].values():
            class_properties = collections.OrderedDict()
            for name, info in class_info["properties"].items():
                if name not in class_properties:
                    class_properties[name] = {}
                if info["getter"]:
                    class_properties[name]["getter"] = {
                        "procedure": info["getter"]["procedure"],
                        "remote_name": info["getter"]["remote_name"],
                        "parameters": [],
                        "return_type": info["type"],
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
                if info["setter"]:
                    class_properties[name]["setter"] = {
                        "procedure": info["setter"]["procedure"],
                        "remote_name": info["setter"]["remote_name"],
                        "parameters": [
                            self.generate_context_parameters(
                                info["setter"]["remote_name"],
                                info["setter"]["procedure"],
                            )[1]
                        ],
                        "return_type": "None",
                        "documentation": info["setter_documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
            class_info["properties"] = class_properties

        # Find all service dependencies
        dependencies = set()
        procedures = (
            list(context["procedures"].values())
            + [
                procedure
                for property in context["properties"].values()
                for procedure in property.values()
            ]
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
            if "return_type" in info["procedure"]:
                rtype = info["procedure"]["return_type"]
                if "service" in rtype and context["service_name"] != rtype["service"]:
                    dependencies.add(rtype["service"])
            for i in range(len(info["parameters"])):
                ptype = info["procedure"]["parameters"][i]["type"]
                if "service" in ptype and context["service_name"] != ptype["service"]:
                    dependencies.add(ptype["service"])

        for class_info in context["classes"].values():
            items = list(class_info["methods"].values()) + [
                procedure
                for property in class_info["properties"].values()
                for procedure in property.values()
            ]
            for info in items:
                if "return_type" in info["procedure"]:
                    rtype = info["procedure"]["return_type"]
                    if (
                        "service" in rtype
                        and context["service_name"] != rtype["service"]
                    ):
                        dependencies.add(rtype["service"])
                for i in range(len(info["parameters"])):
                    ptype = info["procedure"]["parameters"][i]["type"]
                    if (
                        "service" in ptype
                        and context["service_name"] != ptype["service"]
                    ):
                        dependencies.add(ptype["service"])
        dependencies |= self.struct_field_dependencies(context)
        context["dependencies"] = [
            {"alias": "_service_" + name.lower(), "module": self.module(name)}
            for name in sorted(dependencies)
        ]

        self.add_struct_field_specifications(context)

        # Add type specifications to types
        procedures = (
            list(context["procedures"].values())
            + [
                procedure
                for property in context["properties"].values()
                for _, procedure in property.items()
            ]
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
                ptype = as_type(self.types, info["procedure"]["parameters"][i]["type"])
                pinfo["type"] = {
                    "name": pinfo["type"],
                    "spec": self.parse_type_specification(ptype),
                }
                pos += 1

        for class_info in context["classes"].values():
            items = list(class_info["methods"].values()) + [
                procedure
                for property in class_info["properties"].values()
                for _, procedure in property.items()
            ]
            for info in items:
                info["return_type"] = {
                    "name": info["return_type"],
                    "spec": self.parse_type_specification(
                        self.get_return_type(info["procedure"])
                    ),
                }
                pos = 0
                for i, pinfo in enumerate(info["parameters"]):
                    ptype = as_type(
                        self.types, info["procedure"]["parameters"][i + 1]["type"]
                    )
                    pinfo["type"] = {
                        "name": pinfo["type"],
                        "spec": self.parse_type_specification(ptype),
                    }
                    pos += 1

        self.add_deprecated_docstrings(context)
        return context

    @staticmethod
    def struct_field_dependencies(context):
        """The other services whose types the fields of this service's structures name, which
        the generated module has to import to name them"""
        dependencies = set()
        for struct_info in context["structs"].values():
            for field in struct_info["fields"]:
                service = field["krpc_type"].protobuf_type.service
                if service and service != context["service_name"]:
                    dependencies.add(service)
        return dependencies

    def add_struct_field_specifications(self, context):
        """Add the type annotation each field of a structure is declared with"""
        for struct_info in context["structs"].values():
            for field in struct_info["fields"]:
                field["spec"] = self.parse_type_specification(field["krpc_type"])

    @classmethod
    def add_deprecated_docstrings(cls, context):
        # Prepend a "Deprecated:" line to the docstring of every deprecated member/type
        deprecatable = (
            list(context["procedures"].values())
            + [
                accessor
                for property in context["properties"].values()
                for accessor in property.values()
            ]
            + list(context["enumerations"].values())
            + list(context["structs"].values())
            + list(context["exceptions"].values())
            + list(context["classes"].values())
        )
        for struct_info in context["structs"].values():
            deprecatable += list(struct_info["fields"])
        for class_info in context["classes"].values():
            deprecatable += list(class_info["methods"].values())
            deprecatable += list(class_info["static_methods"].values())
            deprecatable += [
                accessor
                for property in class_info["properties"].values()
                for accessor in property.values()
            ]
        for info in deprecatable:
            cls.add_deprecated_docstring(info)

    @staticmethod
    def add_deprecated_docstring(info):
        if not info.get("deprecated"):
            return
        reason = info.get("deprecated_reason", "")
        line = "Deprecated. " + reason if reason else "Deprecated."
        documentation = info.get("documentation", "")
        if documentation:
            # documentation is '"""\n<content>\n"""'
            body = documentation[len('"""\n') : -len('\n"""')]
            info["documentation"] = '"""\n' + line + "\n\n" + body + '\n"""'
        else:
            info["documentation"] = '"""\n' + line + '\n"""'

    def module(self, service: str) -> str:
        """The module a service's code is generated in. An include gives it as a module or as
        the path of its file."""
        include = self._includes.get(service)
        if include is None:
            return SHIPPED_MODULES.get(service, "krpc.services." + service.lower())
        if include.endswith(".py"):
            return include[: -len(".py")].replace("/", ".")
        return include

    def format_cref(self, target):
        # The module the service is generated in, and in it the class of the service when the
        # target is a member of the service itself
        name = self.module(target.service) + "."
        name += target.type or target.service
        if target.member is not None:
            name += "." + self.language.parse_name(target.member)
        if target.kind in (
            crefs.PROCEDURE,
            crefs.CLASS_METHOD,
            crefs.CLASS_STATIC_METHOD,
        ):
            role = "meth"
        elif target.member is not None:
            role = "attr"
        else:
            role = "class"
        return ":%s:`%s`" % (role, name)

    def parse_documentation(self, documentation):
        documentation = PythonDocParser(self.parse_cref).parse(documentation)
        if len(documentation) == 0:
            return ""
        return '"""\n' + documentation + '\n"""'


class PythonDocParser(DocParser):
    language = PythonLanguage()

    def parse_summary(self, node):
        return self.parse_node(node).strip("\n")

    def parse_remarks(self, node):
        return "\n\n" + self.parse_node(node).strip("\n")

    def parse_param(self, node):
        name = ":param %s: " % self.language.parse_name(node.attrib["name"])
        desc = self.parse_node(node, indent=len(name))[len(name) :]
        if len(desc) == 0:
            return ""
        return "\n\n" + name + desc

    def parse_returns(self, node):
        name = ":returns: "
        desc = self.parse_node(node, indent=len(name))[len(name) :]
        if len(desc) == 0:
            return ""
        return "\n\n" + name + desc

    def parse_paramref(self, node):
        # Emphasized, as reStructuredText reads a name ending in _ as a link
        return "*%s*" % self.language.parse_name(node.attrib["name"])

    @staticmethod
    def parse_a(node):
        return "`%s <%s>`_" % (node.text.replace("\n", " "), node.attrib["href"])

    def parse_c(self, node):
        code = node.text
        code = self.language.value_map.get(code, code)
        return "``%s``" % code

    def parse_list(self, node):
        # A reStructuredText list starts at the margin, set apart by blank lines
        lines = [
            "* " + self.parse_node(item[0], indent=2)[2:].rstrip() for item in node
        ]
        return "\n" + "\n".join(lines) + "\n"
