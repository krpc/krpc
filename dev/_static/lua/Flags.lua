local krpc = require 'krpc'
local conn = krpc.connect()
local MapFilterType = conn.space_center.MapFilterType

-- Show only probes and ships in map mode
conn.space_center.map_filter = MapFilterType.probes:union(MapFilterType.ships)

-- Check whether map mode shows debris
local filter = conn.space_center.map_filter
print(filter:contains(MapFilterType.debris))

-- Show debris as well
conn.space_center.map_filter = filter:union(MapFilterType.debris)
