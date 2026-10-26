"""Generate the gRPC .proto for a service from its kRPC service definitions.

    protogen.py KRPC.KSA.json KSA KSA.proto

The server registers its methods and encodes its messages by the same rules, in
grpc/src/ProtoCodec.cs:

 * Class C is `message C { uint64 id = 1; }`, with id 0 for null, in package
   krpc.<s>, where s is the service name S in lower case.
 * A procedure of class C is a method of service CService, and a procedure of the
   service itself a method of service SService. Its method name M is the procedure's
   name, prefixed with Get or Set for a property's getter or setter: C_get_Name is
   CService.GetName, C_static_Create is CService.Create and get_UT is SService.GetUT.
 * The method is `rpc M (C.MRequest) returns (C.MResponse)`, with the messages
   nested in message C, or in an empty message S for a procedure of the service.
   MRequest holds parameter i as field i + 1, named in snake case. MResponse holds
   the return value as field 1.
 * A tuple is a message holding item i as field i + 1.
 * A list or set is a repeated field, and a dictionary a map.
 * A nullable scalar, or a scalar parameter with a default value, is optional.

A procedure with a type that has no typed form is left out, as the server leaves it
out: nested containers, structures, nullable scalars inside a container, and types
of other services.
"""

import json
import re
import sys
from pathlib import Path

SCALARS = {
    "DOUBLE": "double",
    "FLOAT": "float",
    "SINT32": "sint32",
    "SINT64": "sint64",
    "UINT32": "uint32",
    "UINT64": "uint64",
    "BOOL": "bool",
    "STRING": "string",
    "BYTES": "bytes",
}
MAP_KEYS = {"STRING", "SINT32", "SINT64", "UINT32", "UINT64", "BOOL"}
CONTAINERS = {"LIST", "SET", "DICTIONARY"}


class Unsupported(Exception):
    """A type with no typed form."""


def grpc_name(service_name, procedure_name):
    """The message holding a procedure's request and response, and its method name."""
    parts = procedure_name.split("_")
    holder = service_name if len(parts) == 1 or parts[0] in ("get", "set") else parts[0]
    if holder != service_name:
        parts = parts[1:]
    if parts[0] == "get":
        return holder, "Get" + parts[1]
    if parts[0] == "set":
        return holder, "Set" + parts[1]
    if parts[0] == "static":
        return holder, parts[1]
    return holder, parts[0]


def snake_case(name):
    return re.sub(r"(?<=[a-z0-9])([A-Z])", r"_\1", name).lower()


def upper_snake_case(name):
    return re.sub(
        r"(?<=[a-z0-9])([A-Z])|(?<=[A-Z])([A-Z][a-z])", r"_\1\2", name
    ).upper()


def summary(documentation):
    """The text of the summary of a documentation comment."""
    match = re.search(r"<summary>(.*?)</summary>", documentation or "", re.S)
    if not match:
        return ""
    text = re.sub(
        r'<see cref="[A-Z]:(?:[^".]*\.)*([^".]*)"\s*/>', r"\1", match.group(1)
    )
    text = re.sub(r'<paramref name="([^"]*)"\s*/>', r"\1", text)
    text = re.sub(r"<[^>]+>", "", text)
    return " ".join(text.split())


def comment(text, indent=""):
    return "".join(f"{indent}// {line}\n" for line in wrap(text, 96 - len(indent)))


def wrap(text, width):
    lines, line = [], ""
    for word in text.split():
        if line and len(line) + 1 + len(word) > width:
            lines.append(line)
            line = word
        else:
            line = f"{line} {word}" if line else word
    if line:
        lines.append(line)
    return lines


class Generator:
    def __init__(self, service_name, service):
        self.service_name = service_name
        self.service = service
        self.tuples = {}

    def element_type(self, typ):
        """The proto type of a value that is not a container."""
        code = typ["code"]
        if code in SCALARS:
            return SCALARS[code]
        if code in ("CLASS", "ENUMERATION"):
            if typ["service"] != self.service_name:
                raise Unsupported(
                    f"{typ['service']}.{typ['name']} is in another service"
                )
            return typ["name"]
        if code == "TUPLE":
            name = "Tuple_" + "_".join(self.type_name(x) for x in typ["types"])
            if name not in self.tuples:
                self.tuples[name] = None
                self.tuples[name] = [
                    self.field(x, f"item{i + 1}", i + 1, False, False)
                    for i, x in enumerate(typ["types"])
                ]
            return name
        raise Unsupported(f"{code} has no typed form")

    def type_name(self, typ):
        code = typ["code"]
        if code in SCALARS:
            return SCALARS[code]
        if code in CONTAINERS:
            return (
                code.capitalize()
                + "_"
                + "_".join(self.type_name(x) for x in typ["types"])
            )
        return self.element_type(typ)

    def element(self, typ):
        if typ["code"] in CONTAINERS:
            raise Unsupported("nested containers have no typed form")
        if typ.get("nullable") and (
            typ["code"] in SCALARS or typ["code"] == "ENUMERATION"
        ):
            raise Unsupported("a nullable scalar in a container has no typed form")
        return self.element_type(typ)

    def field(self, typ, name, number, top_level, has_default):
        code = typ["code"]
        if typ.get("nullable") and (code in CONTAINERS or code == "TUPLE"):
            raise Unsupported(f"a nullable {code} has no typed form")
        if code in ("LIST", "SET"):
            return f"repeated {self.element(typ['types'][0])} {name} = {number};"
        if code == "DICTIONARY":
            key, value = typ["types"]
            if key["code"] not in MAP_KEYS or key.get("nullable"):
                raise Unsupported(f"{key['code']} is not a map key")
            return (
                f"map<{SCALARS[key['code']]}, {self.element(value)}> {name} = {number};"
            )
        proto_type = self.element_type(typ)
        if code in SCALARS or code == "ENUMERATION":
            if typ.get("nullable") and not top_level:
                raise Unsupported("a nullable scalar in a tuple has no typed form")
            if typ.get("nullable") or has_default:
                return f"optional {proto_type} {name} = {number};"
        return f"{proto_type} {name} = {number};"

    def procedure(self, procedure):
        """The request and response messages of a procedure."""
        request = []
        for i, parameter in enumerate(procedure.get("parameters", [])):
            request.append(
                self.field(
                    parameter["type"],
                    snake_case(parameter["name"]),
                    i + 1,
                    True,
                    "default_value" in parameter,
                )
            )
        response = []
        if "return_type" in procedure:
            response.append(
                self.field(procedure["return_type"], "value", 1, True, False)
            )
        return request, response

    def generate(self):
        out = ['syntax = "proto3";\n', "\n"]
        out.append(comment(summary(self.service.get("documentation"))))
        out.append(f"package krpc.{self.service_name.lower()};\n\n")

        # The methods and nested messages of each holder: a class, or the service
        rpcs, messages, skipped = {}, {}, []
        for name, procedure in self.service["procedures"].items():
            try:
                request, response = self.procedure(procedure)
            except Unsupported as e:
                skipped.append(f"{name}: {e}")
                continue
            holder, method = grpc_name(self.service_name, name)
            holder_rpcs = rpcs.setdefault(holder, {})
            if method in holder_rpcs:
                raise ValueError(
                    f"{name} and {holder_rpcs[method][0]} are both {holder}Service.{method}"
                )
            holder_rpcs[method] = (
                name,
                comment(summary(procedure.get("documentation")), "  ")
                + f"  rpc {method} ({holder}.{method}Request) returns ({holder}.{method}Response);\n",
            )
            messages.setdefault(holder, []).extend(
                [(f"{method}Request", request), (f"{method}Response", response)]
            )

        types = {
            self.service_name,
            *self.service["classes"],
            *self.service["enumerations"],
            *self.service.get("structs", {}),
        }
        for holder in rpcs:
            if f"{holder}Service" in types:
                raise ValueError(f"{holder}Service is both a service and a type")

        def holder_documentation(holder):
            if holder == self.service_name:
                return self.service.get("documentation")
            return self.service["classes"][holder].get("documentation")

        for holder, holder_rpcs in rpcs.items():
            out.append("\n" + comment(summary(holder_documentation(holder))))
            out.append(f"service {holder}Service {{\n")
            out.append("\n".join(text for _, text in holder_rpcs.values()))
            out.append("}\n")

        def nested(holder):
            return "".join(
                f"\n  message {name} {{\n"
                + "".join(f"    {field}\n" for field in fields)
                + "  }\n"
                for name, fields in messages.get(holder, [])
            )

        out.append(
            f"\n// The requests and responses of the methods of {self.service_name}Service.\n"
        )
        out.append(f"message {self.service_name} {{{nested(self.service_name)}}}\n")

        for name, cls in self.service["classes"].items():
            out.append("\n" + comment(summary(cls.get("documentation"))))
            out.append(f"message {name} {{\n  uint64 id = 1;\n{nested(name)}}}\n")

        for name, enum in self.service["enumerations"].items():
            out.append("\n" + comment(summary(enum.get("documentation"))))
            out.append(f"enum {name} {{\n")
            prefix = upper_snake_case(name)
            values = enum["values"]
            if all(v["value"] != 0 for v in values):
                out.append(f"  {prefix}_UNSPECIFIED = 0;\n")
            for value in values:
                out.append(
                    f"  {prefix}_{upper_snake_case(value['name'])} = {value['value']};\n"
                )
            out.append("}\n")

        for name, fields in self.tuples.items():
            out.append(f"\nmessage {name} {{\n")
            out.extend(f"  {field}\n" for field in fields)
            out.append("}\n")
        return "".join(out), skipped


def main():
    definitions, service_name, output = sys.argv[1:]
    service = json.loads(Path(definitions).read_text(encoding="utf-8"))[service_name]
    text, skipped = Generator(service_name, service).generate()
    for line in skipped:
        print(f"protogen: skipping {service_name}.{line}", file=sys.stderr)
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    Path(output).write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
