import array
import base64
import re
from krpc import definitions

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type

_CAMEL_CASE_REGEX = re.compile(r"([^A-Z]+|[A-Z][^A-Z]*)")


def lower_camel_case(string):
    """Convert from CamelCase to lowerCamelCase"""
    parts = re.findall(_CAMEL_CASE_REGEX, string)
    parts[0] = parts[0].lower()
    return "".join(parts)


def upper_camel_case(string):
    """Convert from lowerCamelCase to CamelCase"""
    return string[0].upper() + string[1:]


def indent(string, width=3):
    """Indent the lines in the given string with width spaces"""
    lines = string.split("\n")
    for i, line in enumerate(lines):
        if line.strip():
            lines[i] = (" " * width) + line
    return "\n".join(lines).strip("\n")


def single_line(string):
    """Convert the given string into a single line"""
    return " ".join(line.strip() for line in string.split("\n"))


def as_type(types, type_info):
    """Convert a type parsed from a JSON service definitions file
    into a type object"""
    return types.as_type(as_protobuf_type(type_info))


def is_nullable(type_info):
    """Whether the position described by the given type specification can hold null.
    A procedure that returns nothing gives no specification"""
    return type_info is not None and type_info.get("nullable", False)


def as_protobuf_type(type_info):
    """Convert a type parsed from a JSON service definitions file
    into a protocol buffer type"""
    protobuf_type = Type()
    protobuf_type.code = getattr(Type, type_info["code"])
    if "service" in type_info:
        protobuf_type.service = type_info["service"]
    if "name" in type_info:
        protobuf_type.name = type_info["name"]
    if "types" in type_info:
        protobuf_type.types.extend([as_protobuf_type(t) for t in type_info["types"]])
    return protobuf_type


def decode_default_value(value, typ, location):
    """Decode a default value parsed from a JSON service definitions file. location names the
    parameter it belongs to, and is reported when the value cannot be decoded."""
    if value is None:
        # A JSON null default value means the default is null
        return None
    value = base64.b64decode(value)
    value = array.array("B", value).tobytes()
    return definitions.decode_default_value(None, value, typ, location)
