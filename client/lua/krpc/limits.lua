-- The extremes of the numeric types kRPC carries over the wire.
--
-- Lua names none of them itself: math.maxinteger and math.mininteger arrive in Lua 5.3, and
-- this client targets 5.1 and 5.2. A service may declare one of these as a parameter's default
-- value, in which case the documentation names the constant here rather than writing out the
-- decimal value, which says far less about what was meant.
--
-- The minimum of an unsigned type is 0, so it has no constant.
--
-- Every Lua 5.1 and 5.2 number is a double, which holds each value here exactly except the two
-- 64-bit integer maxima: SINT64_MAX and UINT64_MAX round up to 2^63 and 2^64. That is a
-- property of the number type, not of these constants - a literal written in their place rounds
-- identically, as does a 64-bit integer arriving from the server.

local limits = {}

-- The largest and most negative finite 64-bit float
limits.DOUBLE_MAX = 1.7976931348623157e+308
limits.DOUBLE_LOWEST = -1.7976931348623157e+308

-- The largest and most negative finite 32-bit float
limits.FLOAT_MAX = 3.4028234663852886e+38
limits.FLOAT_LOWEST = -3.4028234663852886e+38

limits.SINT32_MAX = 2147483647
limits.SINT32_MIN = -2147483648

limits.SINT64_MAX = 9223372036854775807
limits.SINT64_MIN = -9223372036854775808

limits.UINT32_MAX = 4294967295

limits.UINT64_MAX = 18446744073709551615

return limits
