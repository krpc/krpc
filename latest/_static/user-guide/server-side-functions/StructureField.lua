local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local link = conn.space_center.active_vessel.comms.control_path[1]
local site_info = conn:get_call(link.get_start, link)
-- [docs]
-- The Name field of a structure a call returns
local expr = Expression.get_field(Expression.call(site_info), 'Name')
-- [/docs]
print(conn:run(expr))
