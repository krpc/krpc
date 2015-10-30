local krpc = require 'krpc.init'
local conn = krpc.connect()
local q = conn.space_center.active_vessel:flight().rotation
print(q[1], q[2], q[3], q[4])
