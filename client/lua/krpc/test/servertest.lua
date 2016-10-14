local class = require 'pl.class'
local krpc = require 'krpc.init'
local platform = require 'krpc.platform'

ServerTest = class()

function ServerTest:setUp()
  self.conn = self.connect()
end

function ServerTest:tearDown()
  self.conn:close()
end

function ServerTest:get_rpc_port()
  local port = os.getenv('RPC_PORT')
  if port == nil then
    port = 50000
  end
  return port
end

function ServerTest:get_stream_port()
  local port = os.getenv('STREAM_PORT')
  if port == nil then
    port = 50001
  end
  return port
end

function ServerTest:connect()
  return krpc.connect('LuaClientTest', 'localhost',
                      ServerTest.get_rpc_port(), ServerTest.get_stream_port())
end

return ServerTest
