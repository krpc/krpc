local socket = require 'socket'

local platform = {}

local warned = false

--- Deprecated: use socket.sleep from luasocket
function platform.sleep(s)
  if not warned then
    warned = true
    io.stderr:write('krpc.platform.sleep is deprecated, use socket.sleep from luasocket\n')
  end
  socket.sleep(s)
end

return platform
