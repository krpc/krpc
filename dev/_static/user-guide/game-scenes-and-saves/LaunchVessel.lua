local krpc = require 'krpc'
local conn = krpc.connect()
local space_center = conn.space_center

-- [docs]
for _, name in ipairs(space_center.launchable_vessels('VAB')) do
  print(name)
end
space_center.launch_vessel_from_vab('Kerbal X')
print(space_center.active_vessel.name)
-- [/docs]
