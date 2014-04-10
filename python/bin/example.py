#!/usr/bin/env python2

"""
This example script is an autopilot that will launch the supplied Test.craft
(a 2-stage rocket) and take it into orbit at 80km.
"""
# TODO: this script doesn't circularise the orbit... yet

import krpc
import time

def main():
    # Connect to the server with the default settings
    # (IP address 127.0.0.1 and port 50000)
    print 'Connecting to server...'
    ksp = krpc.connect(name='Example script')
    print 'Connected to server, version', ksp.krpc.get_status().version

    vessel = ksp.space_center.active_vessel
    orbit = vessel.orbit
    control = vessel.control
    flight = vessel.flight
    resources = vessel.resources

    # Set the throttle to 100% and enable SAS
    control.throttle = 1
    control.sas = True

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    control.activate_next_stage()

    # Ascend to 10km and ditch SRBs when they're empty
    print 'Vertical ascent...'
    srbs_separated = False
    while True:

        # Check altitude, exit loop if higher than 10km
        altitude = flight.altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 10000:
            break

        # Check if the solid boosters need to be ditched
        # (We assume this will happen before we reach 10km)
        if not srbs_separated:
            solid_fuel = resources.get_resource('SolidFuel')
            print '  Solid fuel = %.1f T' % solid_fuel
            if solid_fuel < 15.1:
                print '  SRB separation!'
                control.activate_next_stage()
                srbs_separated = True

        time.sleep(1)

    # Disable SAS, pitch the vessel to 50 degrees to the west, then hold position using SAS
    print 'Gravity turn...'
    control.sas = False
    control.yaw = 0.1
    while flight.pitch > 50:
        time.sleep(0.25)
    control.yaw = 0
    control.sas = True

    # Raise apoapsis to above 80km
    while True:

        # Apoapsis is relative to the center of Kerbin, so subtract 600km
        apoapsis = orbit.apoapsis - 600000
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            break

        time.sleep(1)

    # Disable the control inputs and coast to apoapsis
    print 'Coasting to apoapsis...'
    control.sas = False
    control.throttle = 0
    while True:

        # Check altitude, exit loop if higher than 79km
        altitude = flight.altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 79000:
            break
        time.sleep(1)

    # Circularize the orbit
    print 'Circularizing...'
    control.sas = False
    control.yaw = 0.1
    while abs(flight.pitch) > 3:
        time.sleep(0.25)
    control.yaw = 0
    control.sas = True
    # Raise periapsis until eccentricity starts the increase (happens just after reaching 0)
    control.throttle = 1

    eccentricity = orbit.eccentricity
    time.sleep(1)

    while True:

        apoapsis = orbit.apoapsis - 600000
        periapsis = orbit.periapsis - 600000
        prev_eccentricity = eccentricity;
        eccentricity = orbit.eccentricity

        print '  Orbit = %.1f km x %.1f km (e = %.3f)' % ((apoapsis/1000), (periapsis/1000), orbit.eccentricity)

        if prev_eccentricity - eccentricity < 0:
            break

        if eccentricity > 0.1:
            time.sleep(1)
        else:
            # We are close, so reduce throttle and check eccentricity more frequently
            control.throttle = 0.1
            time.sleep(0.2)

    control.throttle = 0
    control.sas = False

    print 'Program complete'

if __name__ == "__main__":
    main()
