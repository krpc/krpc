-- The transport itself, over a unix domain socket, against a server the test listens on
-- rather than a kRPC server.
local luaunit = require 'luaunit'

TestConnectionLocalSocket = require 'krpc.test.test_connection_localsocket'

os.exit(luaunit.LuaUnit:run())
