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
  self._socket = socket.tcp()
  result,err = self._socket:connect(self._address, self._port)
  if result == nil then
    error('Socket error: ' .. err)
  end
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
  data, err = self._socket:receive(length)
  if data == nil then
    error('Socket error: ' .. err)
  end
  return data
end

function Connection:send_message(message)
  -- Send a protobuf message.
  data = encoder.encode_message_with_size(message)
  self:send(data)
end

function Connection:receive_message(typ)
  -- Receive a protobuf message.
  local size
  local data = ''
  while true do
    data = data .. self:receive(1)
    local ok, result = pcall(decoder.decode_size, data)
    if ok then
      size = result
      break
    end
  end

  data = self:receive(size)
  return decoder.decode_message(data, typ)
end

return Connection
