local krpc = require 'krpc'
local conn = krpc.connect()
print(conn.krpc.get_status().version)
