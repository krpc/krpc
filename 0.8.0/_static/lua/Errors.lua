local krpc = require 'krpc'
local conn = krpc.connect()
local ok, err = pcall(conn.space_center.load, 'missing')
if not ok then
  print('Failed to load the save: ' .. err)
end
