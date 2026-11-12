local class = require 'pl.class'
local socket = require 'socket'
local Transport = require 'krpc.transport'

-- A transport to a server over TCP/IP
local TCPIPTransport = class(Transport)

function TCPIPTransport:_init(address, port)
  self:super()
  self._address = address
  self._port = port
end

function TCPIPTransport:_open()
  -- The server listens on IPv4, so the socket is opened for that family. A name commonly
  -- resolves to the IPv6 loopback ahead of the IPv4 one, and the attempt on it is slow to
  -- give up.
  self._socket = socket.tcp4()
  local result, err = self._socket:connect(self._address, self._port)
  if result ~= nil then
    -- Send each message as soon as it is written
    self._socket:setoption('tcp-nodelay', true)
  end
  return result, err
end

return TCPIPTransport
