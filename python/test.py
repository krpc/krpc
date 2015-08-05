import krpc
import time
import sys
import numpy as np

conn = krpc.connect()
vessel = conn.space_center.active_vessel

while True:

    refframe = vessel.orbit.body.reference_frame
    surface_ref = vessel.surface_reference_frame
    v_vector = conn.space_center.transform_direction(vessel.velocity(refframe),refframe,surface_ref)

    conn.space_center.clear_drawing()
    conn.space_center.draw_line((0,0,0), v_vector, surface_ref, (1,0,0))

    print '(%+.4f, %+.4f, %+.4f)' % v_vector, '%+.4f' % np.linalg.norm(v_vector)
    time.sleep(0.1)
