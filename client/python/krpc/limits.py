"""The extremes of the numeric types kRPC carries over the wire.

Python has no name for any of these. Its integers are unbounded, so there is no largest
``int`` (``sys.maxint`` was removed with Python 3), and ``sys.maxsize`` is the largest
container length rather than a wire type's limit. Its floats are C doubles, so
``sys.float_info`` describes ``DOUBLE`` but says nothing about the 32-bit ``FLOAT``.

A service may declare one of these as a parameter's default value. The generated stubs
name the constant here rather than writing the decimal expansion, which for
``9223372036854775807`` or ``1.7976931348623157e+308`` says far less about what was
meant.

The minimum of an unsigned type is ``0``, so it has no constant.
"""

import sys

# The largest and most negative finite 64-bit float
DOUBLE_MAX = sys.float_info.max
DOUBLE_LOWEST = -sys.float_info.max

# The largest and most negative finite 32-bit float, as the doubles they widen to. The
# largest is the full 24-bit mantissa scaled to the largest exponent.
FLOAT_MAX = (2 - 2**-23) * 2**127
FLOAT_LOWEST = -FLOAT_MAX

SINT32_MAX = 2**31 - 1
SINT32_MIN = -(2**31)

SINT64_MAX = 2**63 - 1
SINT64_MIN = -(2**63)

UINT32_MAX = 2**32 - 1

UINT64_MAX = 2**64 - 1
