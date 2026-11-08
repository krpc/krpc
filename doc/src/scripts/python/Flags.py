import krpc

conn = krpc.connect()
space_center = conn.space_center
MapFilterType = space_center.MapFilterType

# Show only probes and ships in map mode
space_center.map_filter = MapFilterType.probes | MapFilterType.ships

# Check whether map mode shows debris
print(MapFilterType.debris in space_center.map_filter)

# Show debris as well
space_center.map_filter |= MapFilterType.debris
