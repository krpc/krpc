local krpc = require 'krpc'

local conn = krpc.connect('SpaceCenter Example')
print(conn.space_center.active_vessel.name)
