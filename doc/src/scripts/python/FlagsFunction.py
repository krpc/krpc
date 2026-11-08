import krpc

conn = krpc.connect()
space_center = conn.space_center
MapFilterType = space_center.MapFilterType

# Stream whether map mode shows both probes and ships, computed on the server
craft = MapFilterType.probes | MapFilterType.ships
shows_craft = conn.add_stream(lambda: craft in space_center.map_filter)
print(shows_craft())
