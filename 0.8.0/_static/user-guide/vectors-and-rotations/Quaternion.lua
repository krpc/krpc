local krpc = require 'krpc'
local conn = krpc.connect()
local q = conn.space_center.active_vessel:flight().rotation
print(string.format('(%.2f, %.2f, %.2f, %.2f)', q[1], q[2], q[3], q[4]))
