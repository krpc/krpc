#!/usr/bin/env python2

"""
This example script launches Example.craft (a 3-stage rocket)
into a circular orbit at 150km.
"""

import krpc
import time
import math

turn_start_altitude = 250
turn_end_altitude = 125000
target_altitude = 150000

def main():
    # Connect to the server with the default settings
    print 'Connecting to server...'
    conn = krpc.connect(name='Example script')
    print 'Connected to server, version', conn.krpc.get_status().version

    # Get objects
    vessel = conn.space_center.active_vessel
    orbit = vessel.orbit
    control = vessel.control
    auto_pilot = vessel.auto_pilot
    flight = vessel.flight()
    resources = vessel.resources

    # Set up streams for telemetry
    ut = conn.add_stream(getattr, conn.space_center, 'ut')
    altitude = conn.add_stream(getattr, flight, 'mean_altitude')
    apoapsis = conn.add_stream(getattr, orbit, 'apoapsis_altitude')
    periapsis = conn.add_stream(getattr, orbit, 'periapsis_altitude')
    eccentricity = conn.add_stream(getattr, orbit, 'eccentricity')
    ap_error = conn.add_stream(getattr, auto_pilot, 'error')
    srb_fuel = conn.add_stream(resources.amount, 'SolidFuel', stage=3, cumulative=False)
    launcher_fuel = conn.add_stream(resources.amount, 'LiquidFuel', stage=2, cumulative=False)

    # Pre-launch setup
    control.sas = False
    control.rcs = False
    control.throttle = 1

    # Countdown...
    print '3...'; time.sleep(1)
    print '2...'; time.sleep(1)
    print '1...'; time.sleep(1)
    print 'Launch!'

    # Activate the first stage
    control.activate_next_stage()
    auto_pilot.set_rotation(90, 90)

    # Main ascent loop
    srbs_separated = False
    launcher_separated = False
    turn_angle = 0
    while True:

        # Gravity turn
        if altitude() > turn_start_altitude and altitude() < turn_end_altitude:
            frac = (altitude() - turn_start_altitude) / (turn_end_altitude - turn_start_altitude)
            new_turn_angle = frac * 90
            if abs(new_turn_angle - turn_angle) > 1:
                turn_angle = new_turn_angle
                auto_pilot.set_rotation(90-turn_angle, 90)

        # Separate SRBs when finished
        if not srbs_separated:
            if srb_fuel() - 64 < 0.1:
                control.throttle = 0.1
                time.sleep(0.1)
                control.activate_next_stage()
                time.sleep(1)
                control.throttle = 1
                srbs_separated = True
                print 'SRBs separated'

        # Separate launch stage when finished
        if srbs_separated and not launcher_separated:
            if launcher_fuel() < 0.1:
                control.throttle = 0.1
                time.sleep(0.1)
                control.activate_next_stage()
                time.sleep(1)
                control.throttle = 1
                launcher_separated = True
                print 'Launcher separated'

        # Disable engines when target apoapsis is reached
        if apoapsis() > target_altitude:
            print 'Target apoapsis reached'
            control.throttle = 0
            break

        time.sleep(0.1)

    # Wait until out of atmosphere
    print 'Coasting out of atmosphere'
    while altitude() < 70500:
        time.sleep(0.1)

    # Plan circularization burn (using vis-viva equation)
    print 'Planning circularization burn'
    mu = orbit.body.gravitational_parameter
    r = orbit.apoapsis
    a1 = orbit.semi_major_axis
    a2 = r
    v1 = math.sqrt(mu*((2./r)-(1./a1)))
    v2 = math.sqrt(mu*((2./r)-(1./a2)))
    delta_v = v2 - v1
    node = control.add_node(conn.space_center.ut + orbit.time_to_apoapsis, prograde=delta_v)

    # Calculate burn time (using rocket equation)
    F = vessel.thrust
    Isp = vessel.specific_impulse * 9.82
    m0 = vessel.mass
    m1 = m0 / math.exp(delta_v/Isp)
    flow_rate = F / Isp
    burn_time = (m0 - m1) / flow_rate

    # Wait until burn
    print 'Waiting until circularization burn'
    burn_ut = conn.space_center.ut + orbit.time_to_apoapsis - (burn_time/2.)
    lead_time = 30
    while ut() < burn_ut - lead_time:
        time.sleep(0.1)

    # Orientate ship
    print 'Orientating ship for circularization burn'
    auto_pilot.set_direction((0,0,1), reference_frame=node.reference_frame)
    while ap_error() > 0.5:
        time.sleep(0.1)

    # Execute burn
    print 'Ready to execute burn'
    time_to_apoapsis = conn.add_stream(getattr, orbit, 'time_to_apoapsis')
    while time_to_apoapsis() - (burn_time/2.) > 0:
        pass
    print 'Executing burn'
    control.throttle = 1
    time.sleep(burn_time - 0.5)
    print 'Fine tuning'
    control.throttle = 0.1
    remaining_delta_v = conn.add_stream(getattr, node, 'remaining_delta_v')
    prev_delta_v = float('inf')
    while prev_delta_v > remaining_delta_v():
        prev_delta_v = remaining_delta_v()
        time.sleep(0.1)
    control.throttle = 0
    node.remove()

    # Post-launch clean up
    auto_pilot.disengage()
    control.throttle = 0
    control.sas = False
    control.rcs = False

    print 'Launch complete'

if __name__ == "__main__":
    main()
