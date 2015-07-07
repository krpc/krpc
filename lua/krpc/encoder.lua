local pb = require 'pb'

local encoder = {}

encoder.RPC_HELLO_MESSAGE = '\x48\x45\x4C\x4C\x4F\x2D\x52\x50\x43\x00\x00\x00'
encoder.STREAM_HELLO_MESSAGE = '\x48\x45\x4C\x4C\x4F\x2D\x53\x54\x52\x45\x41\x4D'

function encoder.client_name(name)
  local CLIENT_NAME_LENGTH = 32
  name = name or ''
  name = name:sub(1, CLIENT_NAME_LENGTH)
  return name .. string.rep('\x00', CLIENT_NAME_LENGTH - name:len())
end

function encoder.varint(x)
  local data = ''
  local function write(y)
    data = y
  end
  pb.varint_encoder(write, x)
  return data
end

return encoder
