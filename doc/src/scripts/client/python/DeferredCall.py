import krpc
from krpc import defer

conn = krpc.connect()
vessel = conn.space_center.active_vessel
sc = conn.space_center


# warp_to pauses execution and resumes on a later tick, so the function
# starts it with defer and carries on without waiting for it
def warp_and_arm():
    defer(sc.warp_to(sc.ut + 60))
    vessel.control.rcs = True


conn.run_function(warp_and_arm)
