local krpc = require 'krpc'

local conn = krpc.connect('Kerbal Engineer Redux Example')
local vessel = conn.space_center.active_vessel
local ker = conn.kerbal_engineer_redux

local stages = ker.stages(vessel)
for i = #stages, 1, -1 do
  local stage = stages[i]
  print(string.format('Stage %d: %.0f m/s, TWR %.2f', stage.number, stage.delta_v, stage.available_twr))
end

local burn = ker.burn_time(vessel, 500)
if burn == nil then
  print('Not enough delta-v for the burn')
else
  print(string.format('A 500 m/s burn takes %.1f seconds', burn.duration))
end
