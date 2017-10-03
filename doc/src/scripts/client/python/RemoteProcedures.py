import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel

vessel.name = "My Vessel"

flight_info = vessel.flight()
print(flight_info.mean_altitude)
