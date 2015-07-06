local platform = {}

function platform.hexlify(data)
  result = ""
  for i = 1, data:len() do
    local c = data:byte(i)
    result = result .. string.format("%02x", c)
  end
  return result
end

return platform
