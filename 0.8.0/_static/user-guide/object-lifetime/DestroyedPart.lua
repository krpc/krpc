local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local part = vessel.parts.all[1]
-- [docs]
local ok, result = pcall(function() return part.temperature end)
if ok then
  print(result)
elseif result.name == 'ObjectDestroyedException' then
  print('that part is gone')
else
  error(result)
end
-- [/docs]
