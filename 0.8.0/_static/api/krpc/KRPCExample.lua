local krpc = require 'krpc'

local conn = krpc.connect('KRPC Example')
print(conn.krpc.get_status().version)
