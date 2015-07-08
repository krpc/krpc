local platform = {}

function platform.hexlify(data)
  local result = ''
  for i = 1, data:len() do
    local c = data:byte(i)
    result = result .. string.format('%02x', c)
  end
  return result
end

function platform.unhexlify(data)
  local result = ''
  for i = 1, data:len()/2 do
     local pos = ((i-1)*2)+1
     local x = data:sub(pos,pos+1)
     result = result .. string.char(tonumber(x,16))
  end
  return result
end

function platform.sleep(s)
  local ntime = os.time() + s
  repeat until os.time() > ntime
end

return platform
