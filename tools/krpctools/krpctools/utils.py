import array
import base64
import re
from krpc.decoder import Decoder
from krpc.schema.KRPC_pb2 import Type
from krpc.types import Types, EnumerationType

_CAMEL_CASE_REGEX = re.compile(r'([^A-Z]+|[A-Z][^A-Z]*)')


def lower_camel_case(string):
    """ Convert from CamelCase to lowerCamelCase """
    parts = re.findall(_CAMEL_CASE_REGEX, string)
    parts[0] = parts[0].lower()
    return ''.join(parts)


def upper_camel_case(string):
    """ Convert from lowerCamelCase to CamelCase """
    return string[0].upper() + string[1:]


def indent(string, width=3):
    """ Indent the lines in the given string with width spaces """
    lines = string.split('\n')
    for i, line in enumerate(lines):
        if line.strip():
            lines[i] = (' '*width) + line
    return '\n'.join(lines).strip('\n')


def single_line(string):
    """ Convert the given string into a single line """
    return ' '.join(line.strip() for line in string.split('\n'))


def as_type(types, type_info):
    """ Convert a type parsed from a JSON service definitions file
        into a type object """
    return types.as_type(_as_protobuf_type(types, type_info))


def _as_protobuf_type(types, type_info):
    protobuf_type = Type()
    protobuf_type.code = getattr(Type, type_info['code'])
    if 'service' in type_info:
        protobuf_type.service = type_info['service']
    if 'name' in type_info:
        protobuf_type.name = type_info['name']
    if 'types' in type_info:
        protobuf_type.types.extend(
            [_as_protobuf_type(types, t) for t in type_info['types']])
    return protobuf_type


def decode_default_value(value, typ):
    value = base64.b64decode(value)
    value = array.array('B', value).tostring()
    # Note: following is a workaround for decoding EnumerationType,
    # as set_values has not been called
    if not isinstance(typ, EnumerationType):
        return Decoder.decode(value, typ)
    return Decoder.decode(value, Types().sint32_type)
