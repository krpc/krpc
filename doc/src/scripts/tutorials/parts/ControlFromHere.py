import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# with_title matches the localized display name; for language-independent code
# use the internal name instead, e.g. parts.with_name("dockingPort2").
part = vessel.parts.with_title("Clamp-O-Tron Docking Port")[0]
vessel.parts.controlling = part
