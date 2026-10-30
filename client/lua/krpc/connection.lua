local class = require 'pl.class'
local socket = require 'socket'

local Connection = class()

-- The error a size prefix over five bytes raises
Connection.MALFORMED_SIZE = 'Failed to decode the size of a message'

-- Seconds to wait for the server to close its side once ours is closed
local CLOSE_TIMEOUT = 1

function Connection:_init(address, port)
  self._address = address
  self._port = port
  self._socket = nil
end

function Connection:connect()
  local result, err = self:_open()
  if result == nil then
    error('Socket error: ' .. err)
  end
end

function Connection:_open()
  -- Open a socket to the server, and set whatever options it takes. Everything above this is
  -- transport agnostic, so a different transport only has to replace this.
  --
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

function Connection:close()
  if self._socket then
    self._socket:close()
    self._socket = nil
  end
end

--- Close the write side of the socket, then read until the server closes its side, for up to a
--- second, so that a message written last is not lost to a reset. Then close the socket.
function Connection:half_close_and_close()
  if self._socket then
    self._socket:shutdown('send')
    -- A total for the drain, where the default applies to each wait
    self._socket:settimeout(CLOSE_TIMEOUT, 't')
    self._socket:receive('*a')
  end
  self:close()
end

function Connection:send(data)
  -- Send data to the connection. Blocks until all data has been sent.
   while data:len() > 0 do
     local pos, err = self._socket:send(data)
     if pos == nil then
       error('Socket error: ' .. err, 0)
     end
     data = data:sub(pos+1)
   end
end

function Connection:receive(length)
  -- Receive data from the connection. Blocks until length bytes have been received.
  if length == 0 then
    return ''
  end
  local data, err = self._socket:receive(length)
  if data == nil then
    error('Socket error: ' .. err, 0)
  end
  return data
end

function Connection:receive_message_data(timeout)
  -- Receive the bytes of a protobuf message. Its size arrives in front of it as a varint, read
  -- a byte at a time, as its length is only known once its last byte has arrived. With a
  -- timeout, which the handshake gives, the whole message must arrive within that many seconds. A
  -- call waits indefinitely, as a timeout makes luasocket poll the socket before every read.
  local size = 0
  local shift = 1
  local deadline
  -- An infinite timeout waits with none, as luasocket on Windows fails a wait of math.huge at once
  if timeout ~= nil and timeout < math.huge then
    deadline = socket.gettime() + timeout
  end
  -- Each read waits for what is left of the timeout
  local function receive(length)
    if deadline ~= nil then
      self._socket:settimeout(math.max(deadline - socket.gettime(), 0), 't')
    end
    return self:receive(length)
  end
  -- A message size is a 32 bit value, so five bytes carry the whole of it. A varint that has
  -- not ended by then is not a size, however much more of it arrives.
  for _ = 1, 5 do
    local byte = receive(1):byte()
    size = size + (byte % 128) * shift
    if byte < 128 then
      if size > 0xFFFFFFFF then
        error(Connection.MALFORMED_SIZE, 0)
      end
      local data = receive(size)
      if deadline ~= nil then
        self._socket:settimeout(nil, 't')
      end
      return data
    end
    shift = shift * 128
  end
  error(Connection.MALFORMED_SIZE, 0)
end

return Connection
