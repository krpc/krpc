local platform = {}

function platform.sleep(s)
  local ntime = os.time() + s
  repeat until os.time() > ntime
end

return platform
