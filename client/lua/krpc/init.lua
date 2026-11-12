local Connection = require 'krpc.connection'
local TCPIPTransport = require 'krpc.tcpiptransport'
local LocalTransport = require 'krpc.localtransport'
local limits = require 'krpc.limits'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local errors = require 'krpc.error'

local krpc = {}

-- Exposed here so that krpc.limits.SINT32_MAX, the name a generated default value is
-- documented under, resolves for anyone who has required the package
krpc.limits = limits

-- The error classes, such as krpc.ConnectionError
for name, cls in pairs(errors) do
  krpc[name] = cls
end

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
    error(errors.ConnectionError('Could not work out which user this is, so there is no ' ..
          'default socket path to connect to; pass path instead'), 0)
  end
  return temporary .. separator .. 'krpc-' .. user .. '.sock'
end

-- Send the connection request, and return the server's response to it
local function handshake(transport, name, handshake_timeout)
  transport:send(encoder.encode_in_message('ConnectionRequest', {
    client_name = name,
    protocol_version = PROTOCOL_VERSION
  }))
  local ok, data = pcall(transport.receive_message_data, transport, handshake_timeout)
  if not ok then
    if tostring(data):find('timeout', 1, true) then
      error(errors.ConnectionError('Failed to connect: the server did not answer the ' ..
            'connection request within ' .. handshake_timeout .. ' seconds'), 0)
    end
    error(errors.ConnectionError('Failed to connect', data), 0)
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
    error(errors.ConnectionError(text, nil, nil, reason), 0)
  end
  if message.connection_response == nil then
    error(errors.ConnectionError(
      'Failed to connect: expected a response to the connection request'), 0)
  end
  return message.connection_response
end

-- The handshake is the same whatever carries it. The handshake timeout is in seconds, 5 if it
-- is nil, and false waits indefinitely, such as for a server that asks the player to approve
-- each connection.
local function connect_using(transport, name, handshake_timeout)
  name = name or ''
  if handshake_timeout == nil then
    handshake_timeout = DEFAULT_HANDSHAKE_TIMEOUT
  elseif handshake_timeout == false then
    handshake_timeout = nil
  end

  local opened, failure = pcall(transport.connect, transport)
  if not opened then
    -- A local socket path too long is the caller's error, and passes through
    if type(failure) == 'string' and failure:find('^bad argument') then
      error(failure, 0)
    end
    error(errors.ConnectionError('Failed to connect to the server', failure), 0)
  end
  local ok, response = pcall(handshake, transport, name, handshake_timeout)
  if not ok then
    transport:close()
    if not errors.RemoteError:class_of(response) then
      response = errors.ConnectionError('Failed to connect', response)
    end
    error(response, 0)
  end
  if response.status ~= protobuf.enum('ConnectionResponse.Status', 'OK') then
    transport:close()
    local status = protobuf.enum('ConnectionResponse.Status', response.status)
    local text = 'Failed to connect: the server refused the connection (' ..
      tostring(status or response.status) .. ')'
    if response.message ~= '' then
      text = text .. ': ' .. response.message
    end
    error(errors.ConnectionError(text, nil, status), 0)
  end
  if response.protocol_version ~= PROTOCOL_VERSION then
    transport:close()
    error(errors.ConnectionError('Failed to connect: the server does not support protocol ' ..
          'version ' .. PROTOCOL_VERSION .. '. Use a client from the same kRPC release as the ' ..
          'server.'), 0)
  end

  -- The session has started, so a client that fails to set up tells the server it is closing
  local created, connection = pcall(Connection, transport)
  if not created then
    pcall(function()
      transport:send(encoder.encode_in_message('Disconnect', {
        reason = protobuf.enum('Disconnect.Reason', 'CLOSED')
      }))
      transport:half_close_and_close()
    end)
    transport:close()
    if not errors.RemoteError:class_of(connection) then
      connection = errors.ConnectionError("The server's service definitions are malformed",
                                          connection)
    end
    error(connection, 0)
  end
  return connection
end

function krpc.connect(name, address, port, handshake_timeout)
  address = address or DEFAULT_ADDRESS
  port = port or DEFAULT_PORT
  return connect_using(TCPIPTransport(address, port), name, handshake_timeout)
end

-- Connect to a server on the same machine, over a unix domain socket named by the given
-- path. The path defaults to the one the server uses.
function krpc.connect_local(name, path, handshake_timeout)
  path = path or default_path()
  return connect_using(LocalTransport(path), name, handshake_timeout)
end

-- The identifier of a remote object on the server, e.g. to pass to Expression.constant_object
function krpc.get_object_id(obj)
  if obj == nil or rawequal(obj, Types.none) then
    error('The object is none')
  end
  return obj._object_id
end

return krpc
