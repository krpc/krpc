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

return compat
