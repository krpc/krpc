-- The client end to end against a server, which the harness running these starts and
-- tells them how to reach.
local luaunit = require 'luaunit'

TestClient = require 'krpc.test.test_client'
TestObjects = require 'krpc.test.test_objects'

os.exit(luaunit.LuaUnit:run())
