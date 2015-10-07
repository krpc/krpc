local krpc = require 'krpc.init'
local conn = krpc.connect()
local v = conn.space_center.active_vessel:flight().prograde
print(v[1], v[2], v[3])
