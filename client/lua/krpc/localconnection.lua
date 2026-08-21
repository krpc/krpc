local class = require 'pl.class'
local Connection = require 'krpc.connection'

-- A connection to a server on the same machine, over a unix domain socket. Only
-- opening the socket differs from a TCP connection, so that is all this replaces.
local LocalConnection = class(Connection)

function LocalConnection:_init(path)
  self:super(path, 0)
end

-- luasocket carries its unix domain sockets in a module of its own, which it builds only
-- where the platform has the address family. It is asked for when a socket is opened rather
-- than when this module is loaded, so that a platform without it can still connect over
-- TCP/IP.
local function unix_socket()
  local found, unix = pcall(require, 'socket.unix')
  if not found then
    error('This luasocket has no socket.unix module, so it cannot open a unix domain ' ..
          'socket; connect over TCP/IP with krpc.connect instead')
  end
  return unix()
end

function LocalConnection:_open()
  self._socket = unix_socket()
  return self._socket:connect(self._address)
end

return LocalConnection
