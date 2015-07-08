-- TODO: move this elsewhere
package.path = package.path .. ';../../protoc-gen-lua/protobuf/?.lua'
package.cpath = package.cpath .. ';../../protoc-gen-lua/protobuf/?.so'

local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'

local krpc = {}

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000
local DEFAULT_STREAM_PORT = 50001

function krpc.connect(address, rpcPort, streamPort, name)
  address = address or DEFAULT_ADDRESS
  rpcPort = rpcPort or DEFAULT_RPC_PORT
  streamPort = streamPort or DEFAULT_STREAM_PORT
  name = name or ''

  -- assert rpcPort != streamPort

  -- Connect to RPC server
  rpc_connection = Connection(address, rpcPort)
  rpc_connection:connect()
  rpc_connection:send(encoder.RPC_HELLO_MESSAGE .. encoder.client_name(name))
  client_identifier = rpc_connection:receive(encoder.CLIENT_IDENTIFIER_LENGTH)

  -- Connect to Stream server
  -- TODO
  stream_connection = nil

  return Client(rpc_connection, stream_connection)
end

return krpc
