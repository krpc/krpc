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

function ServerTest:connect()
  local rpc_port = os.getenv('RPC_PORT')
  local stream_port = os.getenv('STREAM_PORT')
  if rpc_port == nil then
    rpc_port = 50000
  end
  if stream_port == nil then
    stream_port = 50001
  end
  return krpc.connect('LuaClientTest', 'localhost', rpc_port, stream_port)
end

return ServerTest
