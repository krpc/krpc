local encoder = {}

local pb = require "pb"

function encoder.varint(x)
  local data = ""
  function write(y)
    data = y
  end
  pb.varint_encoder(write, x)
  return data
end

return encoder
