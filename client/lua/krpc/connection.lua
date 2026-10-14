local class = require 'pl.class'
local socket = require 'socket'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'

local Connection = class()

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
    -- A call writes a request and then waits for its response, so there is never a second
    -- small write for Nagle's algorithm to hold the first one back for.
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

function Connection:send(data)
  -- Send data to the connection. Blocks until all data has been sent.
   while data:len() > 0 do
     local pos, err = self._socket:send(data)
     if pos == nil then
       error('Socket error: ' .. err)
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
    error('Socket error: ' .. err)
  end
  return data
end

function Connection:send_message(type_name, message)
  -- Send a protobuf message of the named type.
  local data = encoder.encode_message_with_size(type_name, message)
  self:send(data)
end

function Connection:receive_message(type_name)
  -- Receive a protobuf message of the named type
  return decoder.decode_message(self:receive_message_data(), type_name)
end

function Connection:receive_message_data()
  -- Receive the bytes of a protobuf message. Its size arrives in front of it as a varint, read
  -- a byte at a time, as its length is only known once its last byte has arrived.
  local size = 0
  local shift = 1
  -- A message size is a 32 bit value, so five bytes carry the whole of it. A varint that has
  -- not ended by then is not a size, however much more of it arrives.
  for _ = 1, 5 do
    local byte = self:receive(1):byte()
    size = size + (byte % 128) * shift
    if byte < 128 then
      return self:receive(size)
    end
    shift = shift * 128
  end
  error('Failed to decode the size of a message')
end

return Connection
