local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local schema = require 'krpc.schema.KRPC'

local krpc = {}

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000

function krpc.connect(name, address, rpc_port)
  name = name or ''
  address = address or DEFAULT_ADDRESS
  rpc_port = rpc_port or DEFAULT_RPC_PORT

  -- Connect to RPC server
  local rpc_connection = Connection(address, rpc_port)
  rpc_connection:connect()
  local request = schema.ConnectionRequest()
  request.type = schema.CONNECTIONREQUEST_TYPE_RPC_ENUM.number
  request.client_name = name
  rpc_connection:send_message(request)
  local response = rpc_connection:receive_message(schema.ConnectionResponse)
  -- FIXME: status field reported as not set when set to OK, as that's the default value
  if response.status and response.status ~= schema.CONNECTIONRESPONSE_STATUS_OK_ENUM.number then
    error('Failed to connect: ' .. response.message)
  end
  local client_identifier = response.client_identifier

  return Client(rpc_connection)
end

return krpc
