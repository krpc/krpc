-- The behavior the client has whatever transport it talks over, and without a server to
-- talk to: the number limits, the number paths without integer types, and the Lua types
-- values take.
local luaunit = require 'luaunit'

TestEncodeDecode = require 'krpc.test.test_encodedecode'
TestLimits = require 'krpc.test.test_limits'
TestTypes = require 'krpc.test.test_types'

os.exit(luaunit.LuaUnit:run())
