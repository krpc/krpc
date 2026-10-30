local krpc = require 'krpc'
local conn = krpc.connect('My Example Program', '192.168.1.10', 1000)
print(conn.krpc.get_status().version)
