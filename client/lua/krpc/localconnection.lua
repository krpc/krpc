local class = require 'pl.class'
local Connection = require 'krpc.connection'

-- A connection to a server on the same machine, over a unix domain socket. Only
-- opening the socket differs from a TCP connection, so that is all this replaces.
local LocalConnection = class(Connection)

function LocalConnection:_init(path)
  self:super(path, 0)
end

-- luasocket carries its unix domain sockets in a module of its own, which it builds
-- everywhere but Windows, where it comes from a rock of its own. It is loaded when a socket
-- is opened, so that an installation without it can still connect over TCP/IP.
local function unix_socket()
  local found, unix = pcall(require, 'socket.unix')
  if not found then
    error('This luasocket has no socket.unix module, so it cannot open a unix domain ' ..
          'socket. On Windows it comes from the luasocket-unix-windows rock; install ' ..
          'that, or connect over TCP/IP with krpc.connect instead')
  end
  return unix()
end

function LocalConnection:_open()
  self._socket = unix_socket()
  return self._socket:connect(self._address)
end

return LocalConnection
