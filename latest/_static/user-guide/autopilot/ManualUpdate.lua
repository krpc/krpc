local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local ap = vessel.auto_pilot
ap.engaged = true

local function control_law(pitch)
  return pitch + 0.05 * (45 - pitch)
end

local function record(err, pitch)
  print(err, pitch)
end

-- [docs]
ap.update_mode = conn.space_center.AutoPilotUpdateMode.manual
while true do
  conn.krpc.hold_tick()
  local ok, err = pcall(function()
    ap:target_pitch_and_heading(control_law(vessel:flight().pitch), 90)
    ap:update()
    record(ap.error, vessel.control.pitch)
  end)
  conn.krpc.release_tick()
  if not ok then error(err) end
end
-- [/docs]
