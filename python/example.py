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

    # Set the throttle to 100% and enable SAS
    ksp.control.throttle = 1
    ksp.control.sas = True

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    ksp.control.activate_next_stage()

    # Ascend to 10km and ditch SRBs when they're empty
    print 'Vertical ascent...'
    srbs_separated = False
    while True:

        # Check altitude, exit loop if higher than 10km
        altitude = ksp.flight.altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 10000:
            break

        # Check if the solid boosters need to be ditched
        # (We assume this will happen before we reach 10km)
        if not srbs_separated:
            resources = ksp.vessel.get_resources()
            print '  Solid fuel = %.1f T' % resources.solidFuel
            if resources.solidFuel < 0.1:
                print '  SRB separation!'
                ksp.control.activate_next_stage()
                srbs_separated = True

        time.sleep(1)

    print 'Gravity turn...'
    # Disable SAS, pitch the vessel to the west, then hold position using SAS
    # TODO: get the heading from the craft to do this more accurately
    ksp.control.sas = False
    ksp.control.yaw = 0.4
    time.sleep(2)
    ksp.control.sas = True
    ksp.control.yaw = 0

    # Raise apoapsis to above 80km
    while True:

        # Apoapsis is relative to the center of Kerbin, so subtract 600km
        apoapsis = ksp.orbit.apoapsis - 600000
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            break

        time.sleep(1)

    # Disable the control inputs and coast to apoapsis
    print 'Coasting to apoapsis...'
    ksp.control.sas = False
    ksp.control.throttle = 0

    # TODO: add code to circularise the orbit

    print 'Program complete'

if __name__ == "__main__":
    main()
