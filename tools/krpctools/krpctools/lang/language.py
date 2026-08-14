import math
import struct
import sys

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import ValueType

# The largest finite 32-bit float, as the double a decoded FLOAT default widens to.
_FLOAT32_MAX = (2 - 2**-23) * 2**127

# The finite extremes of the floating point types, by protobuf type code. They are named
# "lowest" rather than "min" because C++ numeric_limits<T>::min() is the smallest positive
# normal for a float, so "min" would name two different things across the two families.
_FLOAT_EXTREMES = {
    Type.DOUBLE: {"max": sys.float_info.max, "lowest": -sys.float_info.max},
    Type.FLOAT: {"max": _FLOAT32_MAX, "lowest": -_FLOAT32_MAX},
}

# The extremes of the integer types, by protobuf type code. The unsigned minimum is zero and
# needs no name. BOOL is deliberately absent, as Python's True and False are integers.
_INTEGER_EXTREMES = {
    Type.SINT32: {"max": 2**31 - 1, "min": -(2**31)},
    Type.SINT64: {"max": 2**63 - 1, "min": -(2**63)},
    Type.UINT32: {"max": 2**32 - 1},
    Type.UINT64: {"max": 2**64 - 1},
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
