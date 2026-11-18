local krpc = require 'krpc'
local conn = krpc.connect()
local v = conn.space_center.active_vessel:flight().prograde
print(string.format('(%.2f, %.2f, %.2f)', v[1], v[2], v[3]))
