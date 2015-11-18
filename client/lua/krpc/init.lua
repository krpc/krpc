local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'

local krpc = {}

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000
local DEFAULT_STREAM_PORT = 50001

function krpc.connect(name, address, rpc_port, stream_port)
  name = name or ''
  address = address or DEFAULT_ADDRESS
  rpc_port = rpc_port or DEFAULT_RPC_PORT
  stream_port = stream_port or DEFAULT_STREAM_PORT

  if rpc_port == stream_port then
    error('RPC and Stream port are the same')
  end

  -- Connect to RPC server
  local rpc_connection = Connection(address, rpc_port)
  rpc_connection:connect()
  rpc_connection:send(encoder.RPC_HELLO_MESSAGE .. encoder.client_name(name))
  local client_identifier = rpc_connection:receive(encoder.CLIENT_IDENTIFIER_LENGTH)

  -- Connect to Stream server
  local stream_connection
  if stream_port then
    stream_connection = Connection(address, stream_port)
    stream_connection:connect()
    stream_connection:send(encoder.STREAM_HELLO_MESSAGE .. client_identifier)
    local ok_message = stream_connection:receive(2)
    assert(ok_message == decoder.OK_MESSAGE)
  end

  return Client(rpc_connection, stream_connection)
end

return krpc
