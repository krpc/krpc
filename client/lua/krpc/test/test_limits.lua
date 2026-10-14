local luaunit = require 'luaunit'
local class = require 'pl.class'
local krpc = require 'krpc.init'
local compat = require 'krpc.compat'
local limits = require 'krpc.limits'

-- The constants are checked against values derived a different way, so a typo in one of them
-- is caught rather than being asserted against itself.
local TestLimits = class()

function TestLimits:test_double_extremes()
  luaunit.assertEquals(limits.DOUBLE_MAX, -limits.DOUBLE_LOWEST)
  -- At the top of the range: finite itself, but nothing above it is
  luaunit.assertTrue(limits.DOUBLE_MAX < math.huge)
  luaunit.assertEquals(math.huge, limits.DOUBLE_MAX * 2)
end

function TestLimits:test_float_extremes()
  luaunit.assertEquals(limits.FLOAT_MAX, -limits.FLOAT_LOWEST)
  -- The full 24-bit mantissa scaled to the largest exponent, which a double holds exactly
  luaunit.assertEquals((2 - 2^-23) * 2^127, limits.FLOAT_MAX)
  -- A 32-bit float reaches nowhere near as far as the double holding it
  luaunit.assertTrue(limits.FLOAT_MAX < limits.DOUBLE_MAX)
end

function TestLimits:test_signed_integer_extremes()
  -- Two's complement: the range spans 2^bits values, one more below zero
  luaunit.assertEquals(2^32, limits.SINT32_MAX - limits.SINT32_MIN + 1)
  luaunit.assertEquals(limits.SINT32_MIN, -limits.SINT32_MAX - 1)
  luaunit.assertEquals(-2^63, limits.SINT64_MIN)
end

function TestLimits:test_unsigned_integer_extremes()
  luaunit.assertEquals(2^32, limits.UINT32_MAX + 1)
  -- An unsigned type reaches one bit further up than the signed one of its width
  luaunit.assertEquals(limits.UINT32_MAX, limits.SINT32_MAX * 2 + 1)
end

-- From Lua 5.3 on, the 64-bit extremes are exact integers, and the uint64 maximum is the value
-- with every bit set. tostring shows an integer without a fractional part or an exponent.
function TestLimits:test_64_bit_extremes_are_integers()
  luaunit.skipIf(not compat.native_integers, 'numbers have no integer type')
  luaunit.assertEquals('9223372036854775807', tostring(limits.SINT64_MAX))
  luaunit.assertEquals('-9223372036854775808', tostring(limits.SINT64_MIN))
  luaunit.assertEquals('-1', tostring(limits.UINT64_MAX))
end

-- In Lua 5.1 and 5.2 every number is a double, so the two 64-bit integer maxima are not
-- representable and round up to the next power of two
function TestLimits:test_64_bit_maxima_round_to_the_nearest_double()
  luaunit.skipIf(compat.native_integers, 'numbers have an integer type')
  luaunit.assertEquals(2^63, limits.SINT64_MAX)
  luaunit.assertEquals(2^64, limits.UINT64_MAX)
end

-- A generated default value is documented as krpc.limits.SINT32_MAX, a name that resolves
-- only because the package exposes the module under it. Compared by identity, as two
-- separate tables holding the same constants would satisfy an equality assertion.
function TestLimits:test_exposed_from_the_package()
  luaunit.assertTrue(krpc.limits == limits)
end

return TestLimits
