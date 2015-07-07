local pb = require 'pb'
local platform = require 'krpc.platform'

local decoder = {}

function decoder.guid(data)
  parts = {
    platform.hexlify(data:sub(1,4):reverse()),
    platform.hexlify(data:sub(5,6):reverse()),
    platform.hexlify(data:sub(7,8):reverse()),
    platform.hexlify(data:sub(9,10)),
    platform.hexlify(data:sub(11,16))
  }
  return table.concat(parts, '-')
end

function decoder.varint(x)
  return pb.varint_decoder(x, 0)
end

return decoder
