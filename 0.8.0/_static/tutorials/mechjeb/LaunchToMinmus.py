import re

import krpc

conn = krpc.connect(name="Launch to Minmus")
sc = conn.space_center
vessel = sc.active_vessel
mj = conn.mech_jeb


def wait_for(condition):
    """Wait until the condition is true."""
    event = conn.add_event(condition)
    with event.condition:
        event.wait()


def print_steps(autopilot):
    """Print each step of the autopilot. Returns the status stream."""
    steps = []

    def print_step(status):
        # The step, without the figures that change on each update
        lines = status.splitlines()
        step = re.split("[0-9\u221e]", lines[0])[0] if lines else ""
        step = step.rstrip(" :=")
        if step and step not in steps[-1:]:
            steps.append(step)
            print(step)

    status = conn.add_stream(getattr, autopilot, "status")
    status.add_callback(print_step)
    return status


def print_orbit():
    orbit = vessel.orbit
    print(
        "Orbit of %s, %.0f km by %.0f km"
        % (
            orbit.body.name,
            orbit.apoapsis_altitude / 1000,
            orbit.periapsis_altitude / 1000,
        )
    )


# The target orbit, set as in the Ascent Guidance window
ascent = mj.ascent_guidance
ascent.ascent_type = mj.AscentType.classic
ascent.desired_orbit_altitude = 100000
ascent.desired_inclination = 0

# The Ascent Settings window
settings = ascent.settings
settings.autostage = True
settings.limit_angle_of_attack = True
settings.max_angle_of_attack = 5
settings.autowarp = True

# Print each step of the autopilot
status = print_steps(ascent)

ascent.enabled = True
vessel.control.activate_next_stage()

# Wait for the autopilot to disengage, once the orbit is circular
wait_for(lambda: not ascent.enabled)
status.remove()
print_orbit()

planner = mj.maneuver_planner
planner.autowarp = True

# Stage once a stage burns out, during the burns too
mj.utilities.staging.enabled = True


def execute_nodes():
    """Execute the vessel's maneuver nodes, and wait for the burns."""
    planner.execute_all_nodes()
    wait_for(lambda: not planner.executing_node)


# Target Minmus, and wait for MechJeb to see the target
minmus = sc.bodies["Minmus"]
sc.target_body = minmus
wait_for(lambda: mj.target_controller.normal_target_exists)

# Match planes with Minmus, at the nearest relative node
plane = planner.operation_plane
plane.time_selector.time_reference = mj.TimeReference.rel_nearest_ad
plane.make_nodes()
execute_nodes()

# The Hohmann transfer, to an encounter with no capture burn
transfer = planner.operation_transfer
transfer.time_selector.time_reference = mj.TimeReference.computed
transfer.capture = False
transfer.make_nodes()
execute_nodes()

# Fine tune the encounter, for a periapsis 20 km above Minmus
correction = planner.operation_course_correction
correction.final_periapsis_altitude = 20000
correction.make_nodes()
execute_nodes()

# Warp into Minmus' sphere of influence
sc.warp_to(sc.ut + vessel.orbit.time_to_soi_change + 60)

# Circularize at the periapsis
circularize = planner.operation_circularize
circularize.time_selector.time_reference = mj.TimeReference.periapsis
circularize.make_nodes()
execute_nodes()

sc.clear_target()
print_orbit()

# Stage until the next stage would drop the landing legs. This drops
# the stages below the lander, and activates the lander's engine.
mj.utilities.staging.enabled = False
legs = vessel.parts.legs[0].part
while vessel.control.current_stage > legs.decouple_stage + 1:
    vessel.control.activate_next_stage()

# Limit the lander's thrust, so its lowest throttle leaves it falling
for engine in vessel.parts.engines:
    engine.thrust_limit = 0.3

# Land on Minmus' Great Flats, and wait for MechJeb to see the target
landing = mj.landing_guidance
landing.set_target(minmus, 0, -6)
wait_for(lambda: mj.target_controller.position_target_exists)

# The settings of the Landing Guidance window
landing.touchdown_speed = 1
landing.deploy_gears = True
landing.autowarp = True

# Print each step of the autopilot
status = print_steps(landing)

landing.land_at_target()

# Wait for the autopilot to disengage, once the vessel has landed
wait_for(lambda: not landing.enabled)
status.remove()

sc.clear_target()
flight = vessel.flight()
print(
    "Landed at latitude %.2f, longitude %.2f"
    % (flight.latitude, flight.longitude)
)
