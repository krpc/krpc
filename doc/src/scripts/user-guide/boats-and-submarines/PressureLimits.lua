local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
for _, part in ipairs(vessel.parts.all) do
  if part.static_pressure + part.dynamic_pressure > part.max_pressure then
    print(part.title .. ' is over its pressure limit')
  end
end
-- [/docs]
