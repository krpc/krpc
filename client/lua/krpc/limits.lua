-- The extremes of the numeric types kRPC carries over the wire.
--
-- A service may declare one of these as a parameter's default value, and the documentation
-- then names the constant here instead of the decimal value.
--
-- The minimum of an unsigned type is 0, so it has no constant.
--
-- From Lua 5.3 on, the 64-bit integer extremes are integers. A uint64 above 2^63 - 1 wraps to
-- a negative integer, so UINT64_MAX is -1, the value with every bit set. In Lua 5.1 and 5.2
-- every number is a double, which holds each value here exactly except the two 64-bit integer
-- maxima: SINT64_MAX and UINT64_MAX round up to 2^63 and 2^64.

local compat = require 'krpc.compat'

local limits = {}

-- The largest and most negative finite 64-bit float
limits.DOUBLE_MAX = 1.7976931348623157e+308
limits.DOUBLE_LOWEST = -1.7976931348623157e+308

-- The largest and most negative finite 32-bit float
limits.FLOAT_MAX = 3.4028234663852886e+38
limits.FLOAT_LOWEST = -3.4028234663852886e+38

limits.SINT32_MAX = 2147483647
limits.SINT32_MIN = -2147483648

limits.SINT64_MAX = compat.maxinteger
limits.SINT64_MIN = compat.mininteger

limits.UINT32_MAX = 4294967295

if compat.native_integers then
  limits.UINT64_MAX = -1
else
  limits.UINT64_MAX = 18446744073709551615
end

return limits
