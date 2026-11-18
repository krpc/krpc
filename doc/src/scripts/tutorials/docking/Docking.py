import math
import time

import krpc

conn = krpc.connect(name="Docking")
space_center = conn.space_center
vessel = space_center.active_vessel
port = vessel.parts.controlling.docking_port
target = space_center.target_docking_port
frame = target.reference_frame

# Turn to face the target port
print("Turning to face the target")
vessel.control.sas = False
vessel.control.rcs = True
vessel.auto_pilot.reference_frame = frame
vessel.auto_pilot.target_direction = (0, -1, 0)
vessel.auto_pilot.engaged = True
vessel.auto_pilot.wait()

# Draw the docking axis, and a line from our port to the target
axis = conn.drawing.add_line((0, 0, 0), (0, 50, 0), frame)
axis.color = conn.std_lib.ColorRGB(0, 1, 0)
approach = conn.drawing.add_line((0, 0, 0), (0, 0, 0), frame)
approach.color = conn.std_lib.ColorRGB(1, 1, 0)

# Approach the target port along its axis
print("Approaching")
position = conn.add_stream(port.position, frame)
velocity = conn.add_stream(vessel.velocity, frame)
states = space_center.DockingPortState
while port.state not in (states.docking, states.docked):
    x, y, z = position()
    approach.start = (x, y, z)

    # Line up on the axis, then close in along it
    off_axis = math.hypot(x, z)
    goal_y = 0 if off_axis < 0.5 else max(y, 5)
    ex, ey, ez = -x, goal_y - y, -z
    distance = math.sqrt(ex * ex + ey * ey + ez * ez)
    speed = min(1, max(0.1, 0.2 * distance))
    scale = speed / max(distance, 0.01)

    # Thrust towards the velocity we want, in the vessel's frame
    vx, vy, vz = velocity()
    thrust = (
        2 * (ex * scale - vx),
        2 * (ey * scale - vy),
        2 * (ez * scale - vz),
    )
    right, forward, down = space_center.transform_direction(
        thrust, frame, vessel.reference_frame
    )
    vessel.control.right = max(-1, min(1, right))
    vessel.control.forward = max(-1, min(1, forward))
    vessel.control.up = max(-1, min(1, -down))
    time.sleep(0.05)

# Stop the thrusters, and remove the drawing
vessel.control.right = 0
vessel.control.forward = 0
vessel.control.up = 0
vessel.auto_pilot.engaged = False
conn.drawing.clear()
print("Docked")
