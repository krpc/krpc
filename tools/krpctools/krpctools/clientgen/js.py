import json
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

from ..lang.js import NATIVE_ERRORS, JsLanguage, accessor_name
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
    """Generates the TypeScript module of a service. The JavaScript client builds its services
    at run time, so the module declares the service: an interface with a member for each
    procedure, property, class, enumeration, structure and exception, and the types of the
    objects and values it passes. Its enumerations, structures and exceptions are values too,
    which a connection uses where the server's definitions match."""

    # The module the generated code imports the client's functions from
    client_module = "@krpc-org/krpc"

    def __init__(self, macro_template, service, definitions, includes=None):
        super().__init__(macro_template, service, definitions, includes)
        self.language = JsLanguage()
        # The names the client's own module exports that the declarations use
        self._client_imports = set()
        # The client's functions that declare the module's values
        self._value_imports = set()
        # The client's namespace of protobuf messages, when the declarations use one
        self._message_imports = set()
        # The other services whose types the declarations use
        self._service_imports = set()
        # Whether links in documentation name the server forms of properties
        self._server_links = False

    def parse_context(self, context):
        service = self._service
        members = []
        for name, info in context["procedures"].items():
            members.append(
                self._member(info["documentation"], self._procedure(name, info), info)
            )
        for info in context["properties"].values():
            members.extend(self._property(info))
        for name, info in context["classes"].items():
            static = []
            for method_name, method in info["static_methods"].items():
                static.append(
                    self._member(
                        method["documentation"],
                        self._procedure(method_name, method),
                        method,
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
            if info["alias"] is not None:
                self._client_imports.add(info["alias"])
                instance = info["alias"]
            elif info["native"] is None:
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
                        method["documentation"],
                        self._procedure(method_name, method),
                        method,
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
            base = "RemoteFlags" if info["flags"] else "RemoteEnum"
            self._client_imports.add(base)
            self._value_imports.add("declareEnumeration")
            values = [
                self._declared_member(name, x, [x["name"], x["value"]])
                for x in info["values"]
            ]
            enumerations.append(
                {
                    "name": name,
                    "base": base,
                    "documentation": doc_comment(self._documentation(info)),
                    "brand": "%s.%s" % (service, name),
                    "deprecated": deprecated_comment(info),
                    "arguments": "%s, %s, %s%s"
                    % (
                        json.dumps(service),
                        json.dumps(name),
                        js_array(values),
                        ", true" if info["flags"] else "",
                    ),
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
            self._value_imports.add("declareStruct")
            field_names = [
                self._declared_member(name, x, [x["name"]]) for x in info["fields"]
            ]
            structs.append(
                {
                    "name": name,
                    "documentation": doc_comment(self._documentation(info)),
                    "brand": "%s.%s" % (service, name),
                    "fields": render(fields),
                    "deprecated": deprecated_comment(info),
                    "arguments": "%s, %s, %s"
                    % (json.dumps(service), json.dumps(name), js_array(field_names)),
                }
            )

        exceptions = self._exceptions(context)

        server_members, server_classes, uses_client_service = self._server_forms(
            context
        )

        self._client_imports.add("RemoteService")
        local_names = self._defined_names() | {"Service", "Server", "ClientService"}
        for name in self._service_imports | self._client_imports | self._value_imports:
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
        # The native errors are globals, named to check that no type hides them
        declare = self._value_imports - set(NATIVE_ERRORS.values())
        if declare:
            imports.insert(
                0,
                'import { %s } from "%s";'
                % (
                    ", ".join(sorted(declare, key=str.lower)),
                    self.client_module,
                ),
            )
        if from_package:
            imports.insert(
                0,
                'import type { %s } from "@krpc-org/krpc";'
                % ", ".join(sorted(from_package, key=str.lower)),
            )

        return {
            "service_name": service,
            "service_documentation": doc_comment(
                self._documentation(
                    {
                        "documentation": context["service_documentation"],
                        "deprecated": context["service_deprecated"],
                        "deprecated_reason": context["service_deprecated_reason"],
                    }
                )
            ),
            "imports": imports,
            "members": render(members),
            "classes": classes,
            "enumerations": enumerations,
            "structs": structs,
            "exceptions": exceptions,
            "server_members": indent(render(server_members)),
            "server_classes": server_classes,
            "uses_client_service": uses_client_service,
        }

    def _server_forms(self, context):
        """The server forms of the service and its classes, as a compiled function reads them:
        the members of the service, the classes, and whether a member names a value of the
        client's form of the service"""
        service = self._service
        self._client_imports.add("ServerObject")
        server_members = self._server_members(
            context["procedures"], context["properties"]
        )
        uses_client_service = False
        for name, info in context["classes"].items():
            static = [
                self._member(
                    self._server_documentation(m["procedure"]["documentation"]),
                    self._procedure(n, m, True),
                    m,
                )
                for n, m in info["static_methods"].items()
            ]
            # A class is a value, which names a type and holds the static methods
            server_members.append(
                self._member(
                    self._server_definition_documentation("classes", name),
                    self._value_type(
                        name,
                        "[Symbol.hasInstance](value: unknown): value is %s;" % name,
                        static,
                    ),
                    info,
                )
            )
        for kind in ("enumerations", "structs", "exceptions"):
            for name, info in context[kind].items():
                uses_client_service = True
                server_members.append(
                    self._member(
                        self._server_definition_documentation(kind, name),
                        ['readonly %s: ClientService["%s"];' % (name, name)],
                        info,
                    )
                )
        server_classes = []
        for name, info in context["classes"].items():
            server_classes.append(
                {
                    "name": name,
                    "documentation": doc_comment(
                        self._documentation(
                            dict(
                                info,
                                documentation=self._server_definition_documentation(
                                    "classes", name
                                ),
                            )
                        )
                    ),
                    "brand": "%s.%s" % (service, name),
                    "members": indent(
                        render(
                            self._server_members(info["methods"], info["properties"])
                        )
                    ),
                }
            )
        return server_members, server_classes, uses_client_service

    def _defined_names(self):
        """The names of the classes, enumerations, structures and exceptions of the service,
        none of which can be a name the declarations of its server forms use"""
        defs = self._defs
        defined = (
            set(defs.get("classes", {}))
            | set(defs.get("enumerations", {}))
            | set(defs.get("structs", {}))
            | set(defs.get("exceptions", {}))
        )
        for name in ("Server", "ClientService"):
            if name in defined:
                raise RuntimeError(
                    "%s defines %s, the name of the declarations of its server forms"
                    % (self._service, name)
                )
        return defined

    def _exceptions(self, context):
        """The exceptions of the service, each declared as a type and a value"""
        exceptions = []
        service = self._service
        for name, info in context["exceptions"].items():
            self._value_imports.add("declareException")
            native = info["native"]
            arguments = [json.dumps(service), json.dumps(name)]
            if native is not None:
                self._value_imports.add(native)
                arguments.append(native)
            if info["alias"] is not None:
                instance = info["alias"]
            elif native is None:
                instance = "ServerError"
            else:
                instance = "%s & ServerErrorDetails" % native
            exceptions.append(
                {
                    "name": name,
                    "documentation": doc_comment(self._documentation(info)),
                    "type": instance,
                    "deprecated": deprecated_comment(info),
                    "arguments": ", ".join(arguments),
                }
            )
        return exceptions

    def _declared_member(self, type_name, info, item):
        """The array of an enumeration value or structure field the module declares, with
        the warning for a use of it when deprecated"""
        if info["deprecated"]:
            qualified = "%s.%s.%s" % (self._service, type_name, info["name"])
            item.append(deprecation_message(qualified, info))
        return item

    @staticmethod
    def parse_plain_cref_member(name):
        return lower_camel_case(name)

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

    def _procedure(self, name, info, server=False):
        """The signatures of a procedure or method: its arguments in order, then, when it has
        defaults, the arguments without one followed by the others by name. On the server, a
        call returns its value"""
        parameters = self._parameters(info, server)
        result = self._result_type(info["procedure"], server)
        returned = result if server else "Promise<%s>" % result
        positional = ", ".join(
            "%s%s: %s" % (p["name"], "?" if p["optional"] else "", p["type"])
            for p in parameters
        )
        lines = ["%s(%s): %s;" % (name, positional, returned)]
        optional = [p for p in parameters if p["optional"]]
        if optional:
            required = [
                "%s: %s" % (p["name"], p["type"])
                for p in parameters
                if not p["optional"]
            ]
            options = "; ".join("%s?: %s" % (p["key"], p["type"]) for p in optional)
            arguments = ", ".join(required + ["options: { %s }" % options])
            lines.append("%s(%s): %s;" % (name, arguments, returned))
        return lines

    def _parameters(self, info, server=False):
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
                    "type": self.ts_type(
                        self.as_type(definition["type"]), True, server
                    ),
                    "optional": "default_value" in definition,
                }
            )
        return result

    def _result_type(self, procedure, server=False):
        typ = self.get_return_type(procedure)
        return "void" if typ is None else self.ts_type(typ, False, server)

    def _server_property(self, info):
        """A property as a compiled function reads it, writable where it has a setter"""
        getter = info["getter"]
        setter = info["setter"]
        if getter is not None:
            remote = getter["remote_name"]
            documentation = getter["procedure"]["documentation"]
            value = self._result_type(getter["procedure"], True)
        else:
            remote = setter["remote_name"]
            documentation = setter["procedure"]["documentation"]
            parameter = setter["procedure"]["parameters"][-1]
            value = self.ts_type(self.as_type(parameter["type"]), True, True)
        name = lower_camel_case(self._property_name(remote))
        if getter is None:
            # A property with no getter cannot be read
            lines = ["set %s(value: %s);" % (name, value)]
        elif setter is None:
            lines = ["readonly %s: %s;" % (name, value)]
        else:
            argument = self.ts_type(
                self.as_type(setter["procedure"]["parameters"][-1]["type"]), True, True
            )
            if argument == value:
                lines = ["%s: %s;" % (name, value)]
            else:
                # A setter takes its argument form, such as a number for a 64-bit integer
                lines = [
                    "get %s(): %s;" % (name, value),
                    "set %s(value: %s);" % (name, argument),
                ]
        return self._member(self._server_documentation(documentation), lines, info)

    def _server_members(self, procedures, properties):
        """The members of a service or class as a compiled function reads them"""
        members = [
            self._member(
                self._server_documentation(x["procedure"]["documentation"]),
                self._procedure(name, x, True),
                x,
            )
            for name, x in procedures.items()
        ]
        members.extend(self._server_property(x) for x in properties.values())
        return members

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

    def ts_type(self, typ, argument, server=False):
        """The TypeScript type of a value. An argument takes a wider type than a result, such
        as a number for a 64-bit integer, or a read-only array. On the server, an object of a
        class has the class's server form"""
        name = self._ts_type(typ, argument, server)
        if typ.nullable:
            return "%s | null" % name
        return name

    def _ts_type(self, typ, argument, server):
        if isinstance(typ, ValueType):
            code = typ.protobuf_type.code
            if code in (Type.SINT64, Type.UINT64):
                return "number | bigint" if argument else "bigint"
            return self.language.parse_type(typ)
        if isinstance(typ, EventType):
            self._client_imports.add("Event")
            return "Event"
        if isinstance(typ, StreamType):
            return "never"
        if isinstance(typ, MessageType):
            self._message_imports.add("schema")
            return "schema.%s" % typ.python_type.__name__
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            service = typ.protobuf_type.service
            # Within the Server namespace, a class's name is its server form
            prefix = "Server." if server and isinstance(typ, ClassType) else ""
            if service == self._service:
                return typ.protobuf_type.name
            self._service_imports.add(service)
            return "%s.%s%s" % (service, prefix, typ.protobuf_type.name)
        if isinstance(typ, ListType):
            item = _parenthesize(self.ts_type(typ.value_type, argument, server))
            return ("readonly %s[]" if argument else "%s[]") % item
        if isinstance(typ, TupleType):
            items = ", ".join(
                self.ts_type(x, argument, server) for x in typ.value_types
            )
            return ("readonly [%s]" if argument else "[%s]") % items
        if isinstance(typ, SetType):
            item = self.ts_type(typ.value_type, argument, server)
            return ("ReadonlySet<%s>" if argument else "Set<%s>") % item
        if isinstance(typ, DictionaryType):
            key = self.ts_type(typ.key_type, argument, server)
            value = self.ts_type(typ.value_type, argument, server)
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
                if self._server_links:
                    member = lower_camel_case(member)
                else:
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

    def _server_documentation(self, documentation):
        """Documentation as a server form carries it, linking to the server forms of
        properties"""
        self._server_links = True
        try:
            return self.parse_documentation(documentation)
        finally:
            self._server_links = False

    def _server_definition_documentation(self, kind, name):
        """The documentation of a class, enumeration, structure or exception of the service,
        as its server form carries it"""
        return self._server_documentation(self._defs[kind][name]["documentation"])


def deprecation_message(qualified_name, info):
    """The warning for a use of a deprecated member, as the client words it"""
    reason = info.get("deprecated_reason", "")
    if reason:
        return "%s is deprecated: %s" % (qualified_name, reason)
    return "%s is deprecated" % qualified_name


def deprecated_comment(info):
    """The documentation comment marking a deprecated value, or none"""
    if not info.get("deprecated"):
        return []
    reason = info.get("deprecated_reason", "")
    return doc_comment("@deprecated " + reason if reason else "@deprecated")


def js_array(items):
    """A JavaScript array literal of arrays of strings and numbers"""
    return "[%s]" % ", ".join(
        "[%s]" % ", ".join(json.dumps(x) for x in item) for item in items
    )


def doc_comment(documentation):
    """The lines of a documentation comment, or none for no documentation"""
    if not documentation:
        return []
    lines = documentation.split("\n")
    if len(lines) == 1:
        return ["/** %s */" % lines[0]]
    return ["/**"] + [(" * " + line).rstrip() for line in lines] + [" */"]


def indent(text):
    """Text indented one more level, for the members of a namespace"""
    return "\n".join(("  " + line) if line else line for line in text.split("\n"))


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
