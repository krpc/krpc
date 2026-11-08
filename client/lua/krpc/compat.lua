-- The integer support of each Lua version the client runs on: 5.1 to 5.4, and LuaJIT, which
-- runs the Lua 5.1 language. pl.compat covers the other differences between them.

local compat = {}

-- Whether numbers include a 64-bit integer type, as they do from 5.3 on. Without one, a 64-bit
-- integer is held in a double, which is exact up to 2^53.
compat.native_integers = math.type ~= nil

if compat.native_integers then
  compat.mininteger = math.mininteger
  compat.maxinteger = math.maxinteger

  --- The given number as an integer. Raises an error if it has a fractional part.
  function compat.tointeger(x)
    local i = math.tointeger(x)
    if i == nil then
      error('Value must be an integer, got ' .. tostring(x))
    end
    return i
  end
else
  -- The nearest doubles, -2^63 and 2^63
  compat.mininteger = -9223372036854775808
  compat.maxinteger = 9223372036854775807

  function compat.tointeger(x)
    return x
  end
end

-- Bitwise operations on non-negative integers below 2^31, such as the values of a flags
-- enumeration. Lua 5.1 has no bitwise operators, and 5.3's do not parse in 5.1.
local function _bitwise(a, b, op)
  local result, bit = 0, 1
  while a > 0 or b > 0 do
    local x, y = a % 2, b % 2
    if op(x, y) then
      result = result + bit
    end
    a, b, bit = math.floor((a - x) / 2), math.floor((b - y) / 2), bit * 2
  end
  return result
end

function compat.bor(a, b)
  return _bitwise(a, b, function(x, y) return x == 1 or y == 1 end)
end

function compat.band(a, b)
  return _bitwise(a, b, function(x, y) return x == 1 and y == 1 end)
end

-- The bits of a that are not in b
function compat.bandnot(a, b)
  return _bitwise(a, b, function(x, y) return x == 1 and y == 0 end)
end

return compat
