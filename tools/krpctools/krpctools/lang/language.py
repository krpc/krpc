import math
import struct

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import ValueType

# The extremes come from the python client, which krpctools already builds on. They describe
# the protobuf types rather than anything about python, so every language's generator reads
# them from here, and the python generator matches a default value against the very constant
# it goes on to name.
from krpc.limits import (
    DOUBLE_MAX,
    DOUBLE_LOWEST,
    FLOAT_MAX,
    FLOAT_LOWEST,
    SINT32_MAX,
    SINT32_MIN,
    SINT64_MAX,
    SINT64_MIN,
    UINT32_MAX,
    UINT64_MAX,
)

# The finite extremes of the floating point types, by protobuf type code. They are named
# "lowest" rather than "min" because C++ numeric_limits<T>::min() is the smallest positive
# normal for a float, so "min" would name two different things across the two families.
_FLOAT_EXTREMES = {
    Type.DOUBLE: {"max": DOUBLE_MAX, "lowest": DOUBLE_LOWEST},
    Type.FLOAT: {"max": FLOAT_MAX, "lowest": FLOAT_LOWEST},
}

# The extremes of the integer types, by protobuf type code. The unsigned minimum is zero and
# needs no name. BOOL is deliberately absent, as Python's True and False are integers.
_INTEGER_EXTREMES = {
    Type.SINT32: {"max": SINT32_MAX, "min": SINT32_MIN},
    Type.SINT64: {"max": SINT64_MAX, "min": SINT64_MIN},
    Type.UINT32: {"max": UINT32_MAX},
    Type.UINT64: {"max": UINT64_MAX},
}


def special_value_name(value, typ):
    """The symbolic name of value, if it is a special value of the value type typ,
    otherwise None."""
    if value is None or not isinstance(typ, ValueType):
        return None
    code = typ.protobuf_type.code
    if code in _FLOAT_EXTREMES:
        if math.isnan(value):
            return "nan"
        if math.isinf(value):
            return "inf" if value > 0 else "-inf"
        extremes = _FLOAT_EXTREMES[code]
    elif code in _INTEGER_EXTREMES:
        extremes = _INTEGER_EXTREMES[code]
    else:
        return None
    for name, extreme in extremes.items():
        if value == extreme:
            return name
    return None


def float32_literal(value):
    """The shortest decimal text that round-trips to the same 32-bit float."""
    text = repr(value)
    for precision in range(1, 10):
        candidate = "%.*g" % (precision, value)
        try:
            narrowed = struct.unpack("<f", struct.pack("<f", float(candidate)))[0]
        except OverflowError:
            # Rounding took the text past the largest float, as it does for the four
            # digits of 3.403e+38, so it is not this value however it is read
            continue
        if narrowed == value:
            text = candidate
            break
    # Text that names no fractional part and carries no exponent would read as an integer
    if "." not in text and "e" not in text:
        text += ".0"
    return text


class Language:

    # Language source for each special value, keyed by (protobuf type code, symbolic name).
    # A value with no entry is written as an ordinary literal.
    special_values = {}

    def __init__(self):
        self.module = None

    def parse_name(self, name):
        if hasattr(self, "keywords") and name in self.keywords:
            return "%s_" % name
        return name

    def parse_enum_value_name(self, name):
        """The name of a value of an enumeration, as the enumeration declares it"""
        return self.parse_name(name)

    def parse_type(self, typ):  # pylint: disable=unused-argument
        raise NotImplementedError

    def parse_default_value(self, value, typ):  # pylint: disable=unused-argument
        return None

    def parse_special_value(self, value, typ):
        """The language's source for value, if it is a special value the language names,
        otherwise None."""
        name = special_value_name(value, typ)
        if name is None:
            return None
        return self.special_values.get((typ.protobuf_type.code, name))
