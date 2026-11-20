local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

local function control_law(pitch)
  return 0.05 * (45 - pitch)
end

-- [docs]
local ok, err = pcall(function()
  while true do
    conn.krpc.next_tick()
    local pitch = vessel:flight().pitch
    vessel.control.pitch = control_law(pitch)
  end
end)
conn.krpc.release_tick()
if not ok then error(err, 0) end
-- [/docs]
