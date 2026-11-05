local krpc = require 'krpc'
local conn = krpc.connect()
local space_center = conn.space_center

-- The target vessel is nil when nothing is targeted
local target = space_center.target_vessel
if target then
  print(target.name)
end

-- Assigning nil clears the target
space_center.target_vessel = nil
