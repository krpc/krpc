local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

local function control_law(pitch)
  return 0.05 * (45 - pitch)
end

-- [docs]
while true do
  conn.krpc.hold_tick()
  local ok, err = pcall(function()
    local pitch = vessel:flight().pitch
    vessel.control.pitch = control_law(pitch)
  end)
  conn.krpc.release_tick()
  if not ok then error(err, 0) end
end
-- [/docs]
