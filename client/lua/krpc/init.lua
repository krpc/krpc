local Client = require 'krpc.client'
local Connection = require 'krpc.connection'
local LocalConnection = require 'krpc.localconnection'
local limits = require 'krpc.limits'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'

local krpc = {}

-- Exposed here so that krpc.limits.SINT32_MAX, the name a generated default value is
-- documented under, resolves for anyone who has required the package
krpc.limits = limits

local DEFAULT_ADDRESS = '127.0.0.1'
local DEFAULT_PORT = 50000
local PROTOCOL_VERSION = 2
-- Seconds to wait for the server to accept a connection
local DEFAULT_HANDSHAKE_TIMEOUT = 5

-- The default socket path, matching the server's own default. Windows has no runtime directory
-- for this, so its per-user application data directory stands in. The fallback is in a fixed
-- directory, as TMPDIR moves the temporary one for the client and not for the server.
local function default_path()
  local windows = package.config:sub(1, 1) == '\\'
  local separator = windows and '\\' or '/'
  local directory = os.getenv(windows and 'LOCALAPPDATA' or 'XDG_RUNTIME_DIR')
  if directory ~= nil and directory ~= '' then
    return directory .. separator .. 'krpc.sock'
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
          'connect to; pass path instead')
  end
  return temporary .. separator .. 'krpc-' .. user .. '.sock'
end

-- Send the connection request, and return the server's response to it
local function handshake(connection, name, handshake_timeout)
  connection:send(encoder.encode_in_message('ConnectionRequest', {
    client_name = name,
    protocol_version = PROTOCOL_VERSION
  }))
  local ok, data = pcall(connection.receive_message_data, connection, handshake_timeout)
  if not ok then
    if tostring(data):find('timeout', 1, true) then
      error('Failed to connect: the server did not answer the connection request within ' ..
            handshake_timeout .. ' seconds', 0)
    end
    error(data, 0)
  end
  local message = decoder.decode_message(data, 'Message')
  local disconnect = message.disconnect
  if disconnect ~= nil then
    local reason = protobuf.enum('Disconnect.Reason', disconnect.reason)
    local text = 'Failed to connect: the server ended the connection (' ..
      tostring(reason or disconnect.reason) .. ')'
    if disconnect.message ~= '' then
      text = text .. ': ' .. disconnect.message
    end
    error(text, 0)
  end
  if message.connection_response == nil then
    error('Failed to connect: expected a response to the connection request', 0)
  end
  return message.connection_response
end

-- The handshake is the same whatever carries it. The handshake timeout is in seconds, 5 if it
-- is nil, and false waits indefinitely, such as for a server that asks the player to approve
-- each connection.
local function connect_using(connection, name, handshake_timeout)
  name = name or ''
  if handshake_timeout == nil then
    handshake_timeout = DEFAULT_HANDSHAKE_TIMEOUT
  elseif handshake_timeout == false then
    handshake_timeout = nil
  end

  connection:connect()
  local ok, response = pcall(handshake, connection, name, handshake_timeout)
  if not ok then
    connection:close()
    error(response, 0)
  end
  if response.status ~= protobuf.enum('ConnectionResponse.Status', 'OK') then
    connection:close()
    local status = protobuf.enum('ConnectionResponse.Status', response.status)
    local text = 'Failed to connect: the server refused the connection (' ..
      tostring(status or response.status) .. ')'
    if response.message ~= '' then
      text = text .. ': ' .. response.message
    end
    error(text, 0)
  end
  if response.protocol_version ~= PROTOCOL_VERSION then
    connection:close()
    error('Failed to connect: the server does not support protocol version ' .. PROTOCOL_VERSION ..
          '. Use a client from the same kRPC release as the server.', 0)
  end

  -- The session has started, so a client that fails to set up tells the server it is closing
  local created, client = pcall(Client, connection)
  if not created then
    pcall(function()
      connection:send(encoder.encode_in_message('Disconnect', {
        reason = protobuf.enum('Disconnect.Reason', 'CLOSED')
      }))
      connection:half_close_and_close()
    end)
    connection:close()
    error(client, 0)
  end
  return client
end

function krpc.connect(name, address, port, handshake_timeout)
  address = address or DEFAULT_ADDRESS
  port = port or DEFAULT_PORT
  return connect_using(Connection(address, port), name, handshake_timeout)
end

-- Connect to a server on the same machine, over a unix domain socket named by the given
-- path. The path defaults to the one the server uses.
function krpc.connect_local(name, path, handshake_timeout)
  path = path or default_path()
  return connect_using(LocalConnection(path), name, handshake_timeout)
end

-- The identifier of a remote object on the server, e.g. to pass to Expression.constant_object
function krpc.get_object_id(obj)
  if obj == nil or rawequal(obj, Types.none) then
    error('The object is none')
  end
  return obj._object_id
end

return krpc
