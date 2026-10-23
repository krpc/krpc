local class = require 'pl.class'
local krpc = require 'krpc.init'

ServerTest = class()

function ServerTest:setUp()
  self.conn = self.connect()
end

function ServerTest:tearDown()
  self.conn:close()
end

function ServerTest:get_port()
  local port = os.getenv('KRPC_TEST_TCPIP_PORT')
  if port == nil then
    port = 50000
  end
  return port
end

function ServerTest:connect()
  -- Connect over whichever transport the harness started the server with, which it
  -- tells us about by port or by socket path
  local path = os.getenv('KRPC_TEST_LOCALSOCKET_PATH')
  if path ~= nil then
    return krpc.connect_local('LuaClientTest', path)
  end
  return krpc.connect('LuaClientTest', 'localhost', ServerTest.get_port())
end

return ServerTest
