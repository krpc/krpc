local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local LocalConnection = require 'krpc.localconnection'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local limits = require 'krpc.limits'
local schema = require 'krpc.schema.KRPC'

local krpc = {}

-- Exposed here so that krpc.limits.SINT32_MAX, the name a generated default value is
-- documented under, resolves for anyone who has required the package
krpc.limits = limits

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000

-- A default path for a socket of the given name, matching the server's own default. Windows
-- has no runtime directory for this, so its per-user application data directory stands in.
-- The fallback names a fixed directory, as TMPDIR moves the temporary one for the client and
-- not for the server.
local function default_path(name)
  local windows = package.config:sub(1, 1) == '\\'
  local separator = windows and '\\' or '/'
  local directory = os.getenv(windows and 'LOCALAPPDATA' or 'XDG_RUNTIME_DIR')
  if directory ~= nil and directory ~= '' then
    return directory .. separator .. 'krpc' .. separator .. name
  end
  local temporary = '/tmp'
  local user = os.getenv('USER') or os.getenv('LOGNAME')
  if windows then
    temporary = os.getenv('TMP') or os.getenv('TEMP') or '.'
    user = os.getenv('USERNAME')
  end
  -- Lua can only read the environment, and a path without a user name in it would be shared
  -- between accounts
  if user == nil or user == '' then
    error('Could not work out which user this is, so there is no default socket path to ' ..
          'connect to; pass rpc_path instead')
  end
  return temporary .. separator .. 'krpc-' .. user .. separator .. name
end

-- The handshake is the same whatever carries it
local function connect_using(rpc_connection, name)
  name = name or ''

  rpc_connection:connect()
  local request = schema.ConnectionRequest()
  request.type = schema.CONNECTIONREQUEST_TYPE_RPC_ENUM.number
  request.client_name = name
  rpc_connection:send_message(request)
  local response = rpc_connection:receive_message(schema.ConnectionResponse)
  -- A successful (OK) status is the protobuf default, so it is omitted from the message and
  -- response.status reads as nil; the guard ensures only a present, non-OK status is an error.
  if response.status and response.status ~= schema.CONNECTIONRESPONSE_STATUS_OK_ENUM.number then
    error('Failed to connect: ' .. response.message)
  end
  local client_identifier = response.client_identifier

  return Client(rpc_connection)
end

function krpc.connect(name, address, rpc_port)
  address = address or DEFAULT_ADDRESS
  rpc_port = rpc_port or DEFAULT_RPC_PORT
  return connect_using(Connection(address, rpc_port), name)
end

-- Connect to a server on the same machine, over a unix domain socket named by the given
-- path. The path defaults to the one the server uses.
function krpc.connect_local(name, rpc_path)
  rpc_path = rpc_path or default_path('rpc')
  return connect_using(LocalConnection(rpc_path), name)
end

return krpc
