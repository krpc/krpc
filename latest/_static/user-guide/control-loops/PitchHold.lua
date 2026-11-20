-- [docs]
local krpc = require 'krpc'
local conn = krpc.connect('pitch hold')
local vessel = conn.space_center.active_vessel
local target_pitch = 45

local ok, err = pcall(function()
  for _ = 1, 500 do
    conn.krpc.next_tick()
    local flight = vessel:flight()
    local pitch_error = target_pitch - flight.pitch
    vessel.control.pitch = math.max(-1, math.min(1, 0.05 * pitch_error))
  end
end)
conn.krpc.release_tick()
vessel.control.pitch = 0
if not ok then error(err, 0) end
-- [/docs]
