local luaunit = require 'luaunit'
local class = require 'pl.class'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local compat = require 'krpc.compat'
local limits = require 'krpc.limits'
local Types = require 'krpc.types'

local TestEncodeDecode = class()

local types = Types()

-- The bytes of a string as hexadecimal digits, and back
local function hexlify(data)
  return (data:gsub('.', function(c) return string.format('%02x', c:byte()) end))
end

local function unhexlify(data)
  return (data:gsub('..', function(x) return string.char(tonumber(x, 16)) end))
end

local function _run_test_encode_value(typ, cases)
  for _, case in ipairs(cases) do
    local decoded = case[1]
    local encoded = case[2]
    local data = encoder.encode(decoded, typ)
    luaunit.assertEquals(hexlify(data), encoded)
  end
end

local function _run_test_decode_value(typ, cases)
  for _, case in ipairs(cases) do
    local decoded = case[1]
    local encoded = case[2]
    local value = decoder.decode(unhexlify(encoded), typ)
    luaunit.assertEquals(value, decoded)
  end
end

-- Values a 64-bit integer type holds exactly and a double does not. tostring shows an integer
-- without a fractional part or an exponent, so comparing it checks the value is an integer.
local function _run_test_64_bit_values(typ, cases)
  for _, case in ipairs(cases) do
    luaunit.assertEquals(hexlify(encoder.encode(case[1], typ)), case[2])
    luaunit.assertEquals(tostring(decoder.decode(unhexlify(case[2]), typ)),
                         tostring(case[1]))
  end
end

-- Without an integer type, the 64-bit maxima round up to 2^63 and 2^64, and a uint64 above
-- 2^63 - 1 is a double
function TestEncodeDecode:test_64_bit_values_without_integers()
  luaunit.skipIf(compat.native_integers, 'numbers have an integer type')
  _run_test_64_bit_values(types:sint64_type(), {
    {limits.SINT64_MAX, 'feffffffffffffffff01'},
    {limits.SINT64_MIN, 'ffffffffffffffffff01'},
  })
  _run_test_64_bit_values(types:uint64_type(), {
    {limits.UINT64_MAX, 'ffffffffffffffffff01'},
    {9223372036854775808, '80808080808080808001'},
    {18446744073709549568, '80f0ffffffffffffff01'},
  })
end

-- Without an integer type, math.huge stands for the largest value of an unsigned type
function TestEncodeDecode:test_unsigned_huge()
  luaunit.skipIf(compat.native_integers, 'numbers have an integer type')
  for _, typ in ipairs({types:uint32_type(), types:uint64_type()}) do
    _run_test_encode_value(typ, {{math.huge, 'ffffffffffffffff7f'}})
    _run_test_decode_value(typ, {{math.huge, 'ffffffffffffffff7f'}})
  end
end

return TestEncodeDecode
