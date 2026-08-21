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
  -- Connect over whichever transport the harness started the server with, which it
  -- tells us about by port or by socket path
  local rpc_path = os.getenv('RPC_PATH')
  if rpc_path ~= nil then
    return krpc.connect_local('LuaClientTest', rpc_path)
  end
  return krpc.connect('LuaClientTest', 'localhost',
                      ServerTest.get_rpc_port(), ServerTest.get_stream_port())
end

-- Connect to the stream server as though it were the RPC server, to check the server
-- rejects it. Which endpoint that is depends on the transport in use.
function ServerTest:connect_to_stream_server(name)
  local stream_path = os.getenv('STREAM_PATH')
  if stream_path ~= nil then
    return krpc.connect_local(name, stream_path)
  end
  return krpc.connect(name, 'localhost',
                      ServerTest.get_stream_port(), ServerTest.get_stream_port())
end

return ServerTest
