local krpc = require 'krpc'
local conn = krpc.connect('Remote example', 'my.domain.name', 1000)
print(conn.krpc.get_status().version)
