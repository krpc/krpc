local krpc = require 'krpc'
local conn = krpc.connect()
local status = conn.krpc:get_status()
print(string.format('Data in = %.2f KB/s', status.bytes_read_rate/1024))
print(string.format('Data out = %.2f KB/s', status.bytes_written_rate/1024))
