local krpc = require 'krpc'
local conn = krpc.connect('Example')
print(conn.krpc:get_status().version)
