local krpc = require 'krpc'
local conn = krpc.connect('My Example Program')
print(conn.krpc.get_status().version)
conn:close()
