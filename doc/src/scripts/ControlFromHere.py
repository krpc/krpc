import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
part = vessel.parts.with_title('Clamp-O-Tron Docking Port')[0]
vessel.parts.controlling = part
