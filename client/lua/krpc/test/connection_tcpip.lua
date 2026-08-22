-- The transport itself, over TCP/IP, against a server the test listens on rather than a
-- kRPC server.
local luaunit = require 'luaunit'

TestConnectionTCPIP = require 'krpc.test.test_connection_tcpip'

os.exit(luaunit.LuaUnit:run())
