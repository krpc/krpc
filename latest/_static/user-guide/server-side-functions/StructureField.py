import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
expression = conn.krpc.Expression
site_info = conn.get_call(getattr, vessel.comms.control_path[0], "start")
# [docs]
# The Name field of a structure a call returns
expr = expression.get_field(expression.call(site_info), "Name")
# [/docs]
