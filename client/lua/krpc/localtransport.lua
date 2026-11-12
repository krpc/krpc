local class = require 'pl.class'
local Transport = require 'krpc.transport'

-- A transport to a server on the same machine, over a unix domain socket
local LocalTransport = class(Transport)

function LocalTransport:_init(path)
  self:super()
  self._path = path
end

-- luasocket carries its unix domain sockets in the socket.unix module, which it builds
-- everywhere but Windows. We load it when a socket is opened, so a luasocket without it can
-- still connect over TCP/IP.
local function unix_socket()
  local found, unix = pcall(require, 'socket.unix')
  if not found then
    error('This luasocket has no socket.unix module, so it cannot open a unix domain ' ..
          'socket. Connect over TCP/IP with krpc.connect instead')
  end
  return unix()
end

function LocalTransport:_open()
  self._socket = unix_socket()
  return self._socket:connect(self._path)
end

return LocalTransport
