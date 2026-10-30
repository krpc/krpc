"""Generate a gRPC API reference page for a service, from a krpc-docgen template.

    docgen.py ksa.tmpl order.txt ksa.rst KRPC.KSA.json
    docgen.py --messages KSA messages.rst KRPC.KSA.json StdLib.json

Run it with the Python of the krpctools virtualenv. It renders the templates in doc/api
as krpc-docgen does for a client library, with the macros in grpc.tmpl. The methods,
messages and fields it documents are the ones protogen.py generates.
"""

import builtins
import json
import os
import re
import sys

from krpc.types import (
    ClassType,
    DictionaryType,
    EnumerationType,
    ListType,
    SetType,
    StructType,
    TupleType,
    ValueType,
)
from krpc.utils import snake_case
from krpctools.definitions import Definitions
from krpctools.docgen import process_file
from krpctools.docgen.domain import Domain
from krpctools.docgen.nodes import (
    Class,
    ClassMethod,
    ClassProperty,
    ClassStaticMethod,
    Enumeration,
    EnumerationValue,
    Procedure,
    Property,
    Service,
    Struct,
    StructField,
)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from protogen import Generator, Unsupported, grpc_name, summary, upper_snake_case  # noqa: E402


class GrpcDomain(Domain):
    name = "grpc"
    prettyname = "gRPC"
    sphinxname = "grpc"
    highlight = "protobuf"
    codeext = "proto"
    language = None

    def __init__(self, macros, services_info):
        super().__init__(macros)
        self.services_info = services_info

    def package(self, service_name=None):
        return "krpc.%s" % (service_name or self.module).lower()

    def currentmodule(self, name):
        self.module = name
        return ".. grpc:package:: %s" % self.package(name)

    def rpcs(self, member):
        """The methods of a service or class member, one for each of its procedures, with
        the name and type of each request field and of the response field."""
        if isinstance(member, (Property, ClassProperty)):
            procedures = [
                (x.name, x.parameters)
                for x in (member.getter, member.setter)
                if x is not None
            ]
        elif isinstance(member, ClassStaticMethod):
            procedures = [
                ("%s_static_%s" % (member.class_name, member.name), member.parameters)
            ]
        elif isinstance(member, ClassMethod):
            procedures = [
                ("%s_%s" % (member.class_name, member.name), member.parameters)
            ]
        else:
            procedures = [(member.name, member.parameters)]
        service_info = self.services_info[member.service_name]
        generator = Generator(member.service_name, service_info)
        rpcs = []
        for name, nodes in procedures:
            info = service_info["procedures"][name]
            try:
                generator.procedure(info)
            except Unsupported:
                continue
            holder, method = grpc_name(member.service_name, name)
            parameters = info.get("parameters", [])
            rpcs.append(
                {
                    "holder": holder,
                    "method": method,
                    "request": [
                        (
                            snake_case(p["name"]),
                            self.field_type(p["type"], "default_value" in p),
                            node,
                        )
                        for p, node in zip(parameters, nodes)
                    ],
                    "response": self.field_type(info["return_type"], False)
                    if "return_type" in info
                    else None,
                }
            )
        return rpcs

    def field_type(self, typ, has_default):
        """The type of a request or response field, as protogen.py declares it."""
        code = typ["code"]
        if code in ("LIST", "SET"):
            return self.wrap("repeated ", self.element_type(typ["types"][0]), "")
        if code == "DICTIONARY":
            key, value = typ["types"]
            return self.wrap(
                "map<%s, " % self.scalar(key), self.element_type(value), ">"
            )
        description = self.element_type(typ)
        if code not in ("CLASS", "STRUCT", "TUPLE") and (
            typ.get("nullable") or has_default
        ):
            description = self.wrap("optional ", description, "")
        return description

    @staticmethod
    def wrap(before, element, after):
        """A type around an element type, in code style as the element is."""
        if element.startswith("``"):
            return "``%s%s%s``" % (before, element.strip("`"), after)
        text = "``%s`` %s" % (before.strip(), element)
        return text + ("\\ ``%s``" % after if after else "")

    def element_type(self, typ):
        code = typ["code"]
        if code == "CLASS":
            return ":grpc:service:`%s <%s.%sService>`" % (
                typ["name"],
                self.package(typ["service"]),
                typ["name"],
            )
        if code == "STRUCT" and typ["service"] != self.module:
            name = "%s.%s" % (self.package(typ["service"]), typ["name"])
            return ":grpc:message:`%s <%s>`" % (name, name)
        if code in ("ENUMERATION", "STRUCT"):
            return ":grpc:%s:`%s <%s.%s>`" % (
                "enum" if code == "ENUMERATION" else "message",
                typ["name"],
                self.package(typ["service"]),
                typ["name"],
            )
        if code == "TUPLE":
            name = Generator(self.module, {}).element_type(typ)
            return ":grpc:message:`%s <%s.%s>`" % (name, self.package(), name)
        return "``%s``" % self.scalar(typ)

    @staticmethod
    def scalar(typ):
        return typ["code"].lower()

    def default_value(self, value, typ):
        if value is None:
            return None
        if isinstance(typ, ValueType):
            if isinstance(value, bool):
                return "``%s``" % str(value).lower()
            if isinstance(value, str):
                return '``"%s"``' % value
            return "``%s``" % value
        if isinstance(typ, EnumerationType):
            return "``%s_%s``" % (
                upper_snake_case(typ.protobuf_type.name),
                upper_snake_case(value.name),
            )
        return None

    def type(self, typ):
        if isinstance(typ, ValueType):
            return typ.protobuf_type.code
        if isinstance(typ, (ClassType, EnumerationType, StructType)):
            return typ.protobuf_type.name
        if isinstance(typ, (ListType, SetType, DictionaryType, TupleType)):
            return str(typ)
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def target(self, obj):
        """The name of the object a kRPC member is documented as."""
        package = self.package(
            obj.service_name if hasattr(obj, "service_name") else obj.name
        )
        if isinstance(obj, Service):
            return "%s.%sService" % (package, obj.name)
        if isinstance(obj, Class):
            return "%s.%sService" % (package, obj.name)
        if isinstance(obj, (Enumeration, Struct)):
            return "%s.%s" % (package, obj.name)
        if isinstance(obj, (EnumerationValue, StructField)):
            parent, name = obj.fullname.split(".")[1:]
            if isinstance(obj, EnumerationValue):
                name = "%s_%s" % (upper_snake_case(parent), upper_snake_case(name))
            else:
                name = snake_case(name)
            return "%s.%s.%s" % (package, parent, name)
        if isinstance(
            obj, (Procedure, Property, ClassMethod, ClassStaticMethod, ClassProperty)
        ):
            rpcs = self.rpcs(obj)
            if not rpcs:
                return None
            return "%s.%sService.%s" % (package, rpcs[0]["holder"], rpcs[0]["method"])
        raise RuntimeError(str(obj))

    def see(self, obj):
        target = self.target(obj)
        if target is None:
            return "``%s``" % obj.name
        title = ".".join(target.split(".")[-2:])
        if isinstance(obj, Class):
            title = obj.name
        elif isinstance(obj, (Service, Enumeration, Struct)):
            title = target.split(".")[-1]
        elif isinstance(obj, (EnumerationValue, StructField)):
            title = target.split(".")[-1]
        if isinstance(obj, (Service, Class)):
            role = "service"
        elif isinstance(obj, Enumeration):
            role = "enum"
        elif isinstance(obj, Struct):
            role = "message"
        elif isinstance(obj, EnumerationValue):
            role = "value"
        elif isinstance(obj, StructField):
            role = "field"
        else:
            role = "rpc"
        return ":grpc:%s:`%s <%s>`" % (role, title, target)

    @staticmethod
    def paramref(name):
        return "``%s``" % snake_case(name)


def messages_page(service_name, services_info):
    """The page of the tuple messages of a service, and of the structures of other
    services it uses, with the definition of each."""
    service_info = services_info[service_name]
    generator = Generator(service_name, service_info)
    generator.generate()
    content = [
        "Messages\n========\n\n",
        "Tuples\n------\n\n",
        ".. grpc:package:: krpc.%s\n\n" % service_name.lower(),
        "A tuple is a message holding item *i* in the field ``item<i>``.\n",
    ]
    for name, fields in sorted(generator.tuples.items()):
        content.append(message(name, "", fields))

    structs = set()

    def walk(typ):
        if typ["code"] == "STRUCT" and typ["service"] != service_name:
            structs.add((typ["service"], typ["name"]))
        for x in typ.get("types", []):
            walk(x)

    for procedure in service_info["procedures"].values():
        for parameter in procedure.get("parameters", []):
            walk(parameter["type"])
        if "return_type" in procedure:
            walk(procedure["return_type"])
    if structs:
        content.append(
            "\nStructures\n----------\n\n"
            "The structures of other services that the ``%s`` service uses.\n"
            % service_name
        )
    for other, name in sorted(structs):
        info = services_info[other]["structs"][name]
        other_generator = Generator(other, services_info[other])
        fields = [
            other_generator.field(
                field["type"], snake_case(field["name"]), i + 1, True, False
            )
            for i, field in enumerate(info["fields"])
        ]
        content.append("\n.. grpc:package:: krpc.%s\n" % other.lower())
        content.append(message(name, summary(info.get("documentation")), fields))
    return "".join(content)


def message(name, documentation, fields):
    """A message, with its documentation and definition."""
    content = ["\n.. grpc:message:: %s\n\n" % name]
    if documentation:
        content.append("   %s\n\n" % documentation)
    content.append("   .. code-block:: protobuf\n\n      message %s {\n" % name)
    content.extend("        %s\n" % field for field in fields)
    content.append("      }\n")
    return "".join(content)


def main():
    if sys.argv[1] == "--messages":
        service_name, output, *definitions = sys.argv[2:]
        services_info = {}
        for path in definitions:
            with builtins.open(path, encoding="utf-8") as fp:
                services_info.update(json.load(fp))
        os.makedirs(os.path.dirname(os.path.abspath(output)), exist_ok=True)
        with builtins.open(output, "w", encoding="utf-8") as fp:
            fp.write(messages_page(service_name, services_info))
        return

    source, order_file, output, *definitions = sys.argv[1:]
    with builtins.open(order_file, encoding="utf-8") as fp:
        ordering = {value.strip(): i for i, value in enumerate(fp.readlines())}

    services_info = {}
    for path in definitions:
        with builtins.open(path, encoding="utf-8") as fp:
            services_info.update(json.load(fp))

    sort_failed = []

    def sort(member):
        key = ordering.get(member.fullname)
        if key is None:
            sort_failed.append(member.fullname)
            return 0
        return key

    definitions = Definitions(services_info)
    services = {
        name: Service(
            name,
            sort=sort,
            types=definitions.types,
            **{k: v for k, v in info.items() if k != "id"},
        )
        for name, info in services_info.items()
    }
    if sort_failed:
        raise RuntimeError("Don't know how to order:\n" + "\n".join(sort_failed))

    macros = os.path.join(os.path.dirname(os.path.abspath(__file__)), "grpc.tmpl")
    domain = GrpcDomain(macros, services_info)
    content, _ = process_file(domain, services, source)
    content = re.sub(r"\n{3,}", "\n\n", content)
    os.makedirs(os.path.dirname(os.path.abspath(output)), exist_ok=True)
    with builtins.open(output, "w", encoding="utf-8") as fp:
        fp.write(content)


if __name__ == "__main__":
    main()
