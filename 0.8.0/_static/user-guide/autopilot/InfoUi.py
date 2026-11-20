import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
vessel.auto_pilot.show_info_ui = True
# [/docs]
