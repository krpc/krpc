local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local LocalConnection = require 'krpc.localconnection'
local limits = require 'krpc.limits'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'

local krpc = {}

-- Exposed here so that krpc.limits.SINT32_MAX, the name a generated default value is
-- documented under, resolves for anyone who has required the package
krpc.limits = limits

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_RPC_PORT = 50000
local PROTOCOL_VERSION = 1

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
  rpc_connection:send_message('ConnectionRequest', {
    type = protobuf.enum('ConnectionRequest.Type', 'RPC'),
    client_name = name,
    protocol_version = PROTOCOL_VERSION
  })
  local response = rpc_connection:receive_message('ConnectionResponse')
  if response.status ~= protobuf.enum('ConnectionResponse.Status', 'OK') then
    error('Failed to connect: ' .. response.message)
  end
  if response.protocol_version ~= PROTOCOL_VERSION then
    error('Failed to connect: the server does not support protocol version ' .. PROTOCOL_VERSION ..
          '. Use a client from the same kRPC release as the server.')
  end

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

-- The identifier of a remote object on the server, e.g. to pass to Expression.constant_object
function krpc.get_object_id(obj)
  if obj == nil or rawequal(obj, Types.none) then
    error('The object is none')
  end
  return obj._object_id
end

return krpc
