local krpc = require 'krpc'
local conn = krpc.connect_local('My Example Program')
print(conn.krpc.get_status().version)
