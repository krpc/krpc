import re

from krpc.attributes import Attributes
from krpc.schema.KRPC_pb2 import Type
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
from krpc.utils import lower_camel_case

from ..lang.js import JsLanguage, accessor_name
from . import crefs
from .docparser import DocParser
from .generator import Generator

# The words a parameter cannot be named
RESERVED_WORDS = {
    "arguments",
    "await",
    "break",
    "case",
    "catch",
    "class",
    "const",
    "continue",
    "debugger",
    "default",
    "delete",
    "do",
    "else",
    "enum",
    "eval",
    "export",
    "extends",
    "false",
    "finally",
    "for",
    "function",
    "if",
    "implements",
    "import",
    "in",
    "instanceof",
    "interface",
    "let",
    "new",
    "null",
    "package",
    "private",
    "protected",
    "public",
    "return",
    "static",
    "super",
    "switch",
    "this",
    "throw",
    "true",
    "try",
    "typeof",
    "var",
    "void",
    "while",
    "with",
    "yield",
}


class JsGenerator(Generator[JsLanguage]):
    """Generates the TypeScript declarations of a service. The JavaScript client builds its
    services at run time, so the module holds types only: an interface for the service, with
    a member for each procedure, property, class, enumeration, structure and exception, and
    the types of the objects and values it passes."""

    def __init__(self, macro_template, service, definitions, includes=None):
        super().__init__(macro_template, service, definitions, includes)
        self.language = JsLanguage()
        # The names the client's own module exports that the declarations use
        self._client_imports = set()
        # The client's namespace of protobuf messages, when the declarations use one
        self._message_imports = set()
        # The other services whose types the declarations use
        self._service_imports = set()

    def parse_context(self, context):
        defs = self._defs
        service = self._service
        members = []
        for name, info in context["procedures"].items():
            members.append(
                self._member(info["documentation"], self._procedure(name, info))
            )
        for info in context["properties"].values():
            members.extend(self._property(info))
        for name, info in context["classes"].items():
            static = []
            for method_name, method in info["static_methods"].items():
                static.append(
                    self._member(
                        method["documentation"], self._procedure(method_name, method)
                    )
                )
            members.append(
                self._member(
                    info["documentation"],
                    self._value_type(
                        name,
                        "[Symbol.hasInstance](value: unknown): value is %s;" % name,
                        static,
                    ),
                    info,
                )
            )
        for name, info in context["enumerations"].items():
            values = [
                self._member(
                    x["documentation"], ["readonly %s: %s;" % (x["name"], name)], x
                )
                for x in info["values"]
            ]
            members.append(
                self._member(
                    info["documentation"], self._value_type(name, None, values), info
                )
            )
        for name, info in context["structs"].items():
            fields = "; ".join(
                "readonly %s: %s" % (x["name"], self.ts_type(x["krpc_type"], True))
                for x in info["fields"]
            )
            constructor = (
                "new (fields: { %s }): %s;" % (fields, name)
                if fields
                else ("new (fields: Record<string, never>): %s;" % name)
            )
            members.append(
                self._member(
                    info["documentation"],
                    self._value_type(
                        name,
                        "[Symbol.hasInstance](value: unknown): value is %s;" % name,
                        [],
                        constructor,
                    ),
                    info,
                )
            )
        for name, info in context["exceptions"].items():
            if info["native"] is None:
                self._client_imports.add("ServerError")
                instance = "ServerError"
            else:
                # A mapped exception is the native error alone, carrying the server's details
                self._client_imports.add("ServerErrorDetails")
                instance = "%s & ServerErrorDetails" % info["native"]
            members.append(
                self._member(
                    info["documentation"],
                    self._value_type(
                        name,
                        "[Symbol.hasInstance](value: unknown): value is %s;" % instance,
                        [],
                    ),
                    info,
                )
            )

        classes = []
        for name, info in context["classes"].items():
            self._client_imports.add("RemoteObject")
            class_members = []
            for method_name, method in info["methods"].items():
                class_members.append(
                    self._member(
                        method["documentation"], self._procedure(method_name, method)
                    )
                )
            for prop in info["properties"].values():
                class_members.extend(self._property(prop))
            classes.append(
                {
                    "name": name,
                    "documentation": doc_comment(self._documentation(info)),
                    "brand": "%s.%s" % (service, name),
                    "members": render(class_members),
                }
            )

        enumerations = []
        for name, info in context["enumerations"].items():
            self._client_imports.add("RemoteEnum")
            enumerations.append(
                {
                    "name": name,
                    "documentation": doc_comment(self._documentation(info)),
                    "brand": "%s.%s" % (service, name),
                }
            )

        structs = []
        for name, info in context["structs"].items():
            self._client_imports.add("RemoteStruct")
            fields = [
                self._member(
                    x["documentation"],
                    [
                        "readonly %s: %s;"
                        % (x["name"], self.ts_type(x["krpc_type"], False))
                    ],
                    x,
                )
                for x in info["fields"]
            ]
            structs.append(
                {
                    "name": name,
                    "documentation": doc_comment(self._documentation(info)),
                    "brand": "%s.%s" % (service, name),
                    "fields": render(fields),
                }
            )

        self._client_imports.add("RemoteService")
        local_names = (
            set(defs.get("classes", {}))
            | set(defs.get("enumerations", {}))
            | set(defs.get("structs", {}))
            | {"Service"}
        )
        for name in self._service_imports | self._client_imports:
            if name in local_names:
                raise RuntimeError(
                    "%s defines %s, which the declarations of the service need for "
                    "another type" % (service, name)
                )

        # The client's types, and the services it ships, come from its package. A service
        # given an include path comes from there.
        imports = []
        from_package = set(self._client_imports) | self._message_imports
        for name in sorted(self._service_imports):
            if name in self._includes:
                imports.append(
                    'import type * as %s from "%s";' % (name, self._includes[name])
                )
            else:
                from_package.add(name)
        if from_package:
            imports.insert(
                0,
                'import type { %s } from "@krpc-org/krpc";'
                % ", ".join(sorted(from_package, key=str.lower)),
            )

        return {
            "service_name": service,
            "service_documentation": doc_comment(context["service_documentation"]),
            "imports": imports,
            "members": render(members),
            "classes": classes,
            "enumerations": enumerations,
            "structs": structs,
        }

    def _documentation(self, info):
        """The documentation of a definition, with its deprecation"""
        documentation = info.get("documentation", "")
        if info.get("deprecated"):
            reason = info.get("deprecated_reason", "")
            line = "@deprecated " + reason if reason else "@deprecated"
            documentation = documentation + "\n\n" + line if documentation else line
        return documentation

    def _member(self, documentation, lines, info=None):
        """The lines of a member's declaration, after its documentation comment"""
        info = dict(info or {}, documentation=documentation)
        return doc_comment(self._documentation(info)) + lines

    @staticmethod
    def _value_type(name, first, members, constructor=None):
        """The declaration of a value a service exposes, such as a class, as an object type
        holding its members"""
        lines = ["readonly %s: {" % name]
        if constructor is not None:
            lines.append("  " + constructor)
        if first is not None:
            lines.append("  " + first)
        for member in members:
            lines.extend("  " + line for line in member)
        lines.append("};")
        return lines

    def _procedure(self, name, info):
        """The signatures of a procedure or method: its arguments in order, then, when it has
        defaults, the arguments without one followed by the others by name"""
        parameters = self._parameters(info)
        result = self._result_type(info["procedure"])
        positional = ", ".join(
            "%s%s: %s" % (p["name"], "?" if p["optional"] else "", p["type"])
            for p in parameters
        )
        lines = ["%s(%s): Promise<%s>;" % (name, positional, result)]
        optional = [p for p in parameters if p["optional"]]
        if optional:
            required = [
                "%s: %s" % (p["name"], p["type"])
                for p in parameters
                if not p["optional"]
            ]
            options = "; ".join("%s?: %s" % (p["key"], p["type"]) for p in optional)
            arguments = ", ".join(required + ["options: { %s }" % options])
            lines.append("%s(%s): Promise<%s>;" % (name, arguments, result))
        return lines

    def _parameters(self, info):
        """The parameters of a procedure, leaving out the object of a method"""
        definitions = info["procedure"]["parameters"]
        definitions = definitions[len(definitions) - len(info["parameters"]) :]
        result = []
        for definition in definitions:
            key = self.language.parse_name(definition["name"])
            result.append(
                {
                    "key": key,
                    "name": key + "_" if key in RESERVED_WORDS else key,
                    "type": self.ts_type(self.as_type(definition["type"]), True),
                    "optional": "default_value" in definition,
                }
            )
        return result

    def _result_type(self, procedure):
        typ = self.get_return_type(procedure)
        return "void" if typ is None else self.ts_type(typ, False)

    def _property(self, info):
        """The getter and setter methods of a property"""
        result = []
        if info["getter"] is not None:
            remote = info["getter"]["remote_name"]
            name = accessor_name("get", self._property_name(remote))
            result.append(
                self._member(
                    info["documentation"],
                    [
                        "%s(): Promise<%s>;"
                        % (name, self._result_type(info["getter"]["procedure"]))
                    ],
                    info,
                )
            )
        if info["setter"] is not None:
            remote = info["setter"]["remote_name"]
            name = accessor_name("set", self._property_name(remote))
            parameter = info["setter"]["procedure"]["parameters"][-1]
            value = self.ts_type(self.as_type(parameter["type"]), True)
            result.append(
                self._member(
                    info["setter_documentation"],
                    ["%s(value: %s): Promise<void>;" % (name, value)],
                    info,
                )
            )
        return result

    @staticmethod
    def _property_name(remote):
        if not Attributes.is_a_class_member(remote):
            return Attributes.get_property_name(remote)
        return Attributes.get_class_member_name(remote)

    def ts_type(self, typ, argument):
        """The TypeScript type of a value. An argument takes a wider type than a result, such
        as a number for a 64-bit integer, or a read-only array"""
        name = self._ts_type(typ, argument)
        if typ.nullable:
            return "%s | null" % name
        return name

    def _ts_type(self, typ, argument):
        if isinstance(typ, ValueType):
            code = typ.protobuf_type.code
            if code in (Type.SINT64, Type.UINT64):
                return "number | bigint" if argument else "bigint"
            return self.language.parse_type(typ)
        if isinstance(typ, (StreamType, EventType)):
            return "never"
        if isinstance(typ, MessageType):
            self._message_imports.add("schema")
            return "schema.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            service = typ.protobuf_type.service
            if service == self._service:
                return typ.protobuf_type.name
            self._service_imports.add(service)
            return "%s.%s" % (service, typ.protobuf_type.name)
        if isinstance(typ, ListType):
            item = _parenthesize(self.ts_type(typ.value_type, argument))
            return ("readonly %s[]" if argument else "%s[]") % item
        if isinstance(typ, TupleType):
            items = ", ".join(self.ts_type(x, argument) for x in typ.value_types)
            return ("readonly [%s]" if argument else "[%s]") % items
        if isinstance(typ, SetType):
            item = self.ts_type(typ.value_type, argument)
            return ("ReadonlySet<%s>" if argument else "Set<%s>") % item
        if isinstance(typ, DictionaryType):
            key = self.ts_type(typ.key_type, argument)
            value = self.ts_type(typ.value_type, argument)
            return ("ReadonlyMap<%s, %s>" if argument else "Map<%s, %s>") % (key, value)
        raise RuntimeError("Unknown type " + str(typ))

    def format_cref(self, target):
        """A link to the target of a cref within the service's module, or the name of the
        target in another service's"""
        if target.kind == crefs.SERVICE:
            name = "Service"
        elif target.member is None:
            name = target.type
        else:
            member = target.member
            if target.kind in (crefs.PROPERTY, crefs.CLASS_PROPERTY):
                member = accessor_name("get" if target.getter else "set", member)
            else:
                member = self.language.parse_name(member)
            if target.kind in (crefs.CLASS_STATIC_METHOD, crefs.ENUMERATION_VALUE):
                name = "Service.%s.%s" % (target.type, member)
            else:
                name = "%s.%s" % (target.type or "Service", member)
        if target.service != self._service:
            return "`%s.%s`" % (target.service, name)
        return "{@link %s}" % name

    def parse_documentation(self, documentation):
        documentation = JsDocParser(self.parse_cref).parse(documentation).strip()
        return re.sub(r"\n{3,}", "\n\n", documentation)


def doc_comment(documentation):
    """The lines of a documentation comment, or none for no documentation"""
    if not documentation:
        return []
    lines = documentation.split("\n")
    if len(lines) == 1:
        return ["/** %s */" % lines[0]]
    return ["/**"] + [(" * " + line).rstrip() for line in lines] + [" */"]


def render(members):
    """The declarations of the members of a type, indented within it"""
    blocks = ["\n".join("  " + line for line in member) for member in members]
    return "\n".join(blocks)


def _parenthesize(name):
    """A type as the item of an array type, which a union or a read-only array is only in
    parentheses"""
    if name.startswith("readonly "):
        return "(%s)" % name
    depth = 0
    for char in name:
        if char in "([<{":
            depth += 1
        elif char in ")]>}":
            depth -= 1
        elif char == "|" and depth == 0:
            return "(%s)" % name
    return name


class JsDocParser(DocParser):
    """Converts documentation to the body of a TSDoc comment"""

    language = JsLanguage()

    def parse_node(self, node, indent=0):
        content = super().parse_node(node, indent)
        # The text of an element runs over lines indented for the XML
        return "\n".join(line.strip() for line in content.split("\n"))

    def parse_summary(self, node):
        return self.parse_node(node).strip("\n")

    def parse_remarks(self, node):
        return "\n\n" + self.parse_node(node).strip("\n")

    def parse_param(self, node):
        description = self.parse_node(node).strip()
        if not description:
            return ""
        name = self.language.parse_name(node.attrib["name"])
        return "\n\n@param %s - %s" % (name, description)

    def parse_returns(self, node):
        description = self.parse_node(node).strip()
        if not description:
            return ""
        return "\n\n@returns %s" % description

    def parse_paramref(self, node):
        return "`%s`" % self.language.parse_name(node.attrib["name"])

    @staticmethod
    def parse_a(node):
        return "[%s](%s)" % (node.text.replace("\n", " "), node.attrib["href"])

    def parse_c(self, node):
        code = node.text
        return "`%s`" % self.language.value_map.get(code, code)

    def parse_list(self, node):
        lines = ["- " + self.parse_node(item[0]).strip() for item in node]
        return "\n\n" + "\n".join(lines) + "\n\n"
