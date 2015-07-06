local decoder = {}

local pb = require "pb"

local function hexlify(data)
  result = ""
  for i = 1, data:len() do
    local c = data:byte(i)
    result = result .. string.format("%02x", c)
  end
  return result
end

function decoder.guid(data)
  parts = { hexlify(data:sub(1,4):reverse()),
            hexlify(data:sub(5,6):reverse()),
            hexlify(data:sub(7,8):reverse()),
            hexlify(data:sub(9,10)),
            hexlify(data:sub(11,16)) }
  return table.concat(parts, "-")
end

function decoder.varint(x)
  return pb.varint_decoder(x, 0)
end

return decoder
