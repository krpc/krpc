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
    print 'Connected to server, version', ksp.KRPC.GetStatus().version

    # Set the throttle to 100% and enable SAS
    ksp.Control.Throttle = 1
    ksp.Control.SAS = True

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    ksp.Control.ActivateNextStage()

    # Ascend to 10km and ditch SRBs when they're empty
    print 'Vertical ascent...'
    srbs_separated = False
    while True:

        # Check altitude, exit loop if higher than 10km
        altitude = ksp.Flight.Altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 10000:
            break

        # Check if the solid boosters need to be ditched
        # (We assume this will happen before we reach 10km)
        if not srbs_separated:
            resources = ksp.Vessel.GetResources()
            print '  Solid fuel = %.1f T' % resources.solidFuel
            if resources.solidFuel < 0.1:
                print '  SRB separation!'
                ksp.Control.ActivateNextStage()
                srbs_separated = True

        time.sleep(1)

    # Disable SAS, pitch the vessel to 50 degrees to the west, then hold position using SAS
    print 'Gravity turn...'
    ksp.Control.SAS = False
    ksp.Control.Yaw = 0.1
    while ksp.Flight.Pitch > 50:
        time.sleep(0.25)
    ksp.Control.Yaw = 0
    ksp.Control.SAS = True

    # Raise apoapsis to above 80km
    while True:

        # Apoapsis is relative to the center of Kerbin, so subtract 600km
        apoapsis = ksp.Orbit.Apoapsis - 600000
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            break

        time.sleep(1)

    # Disable the control inputs and coast to apoapsis
    print 'Coasting to apoapsis...'
    ksp.Control.SAS = False
    ksp.Control.Throttle = 0
    while True:

        # Check altitude, exit loop if higher than 79km
        altitude = ksp.Flight.Altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 79000:
            break
        time.sleep(1)

    # Circularize the orbit
    print 'Circularizing...'
    ksp.Control.SAS = False
    ksp.Control.Yaw = 0.1
    while ksp.Flight.Pitch > 0:
        time.sleep(0.25)
    ksp.Control.Yaw = 0
    ksp.Control.SAS = True
    # Raise periapsis until eccentricity starts the increase (happens just after reaching 0)
    ksp.Control.Throttle = 1

    eccentricity = ksp.Orbit.Eccentricity
    time.sleep(1)

    while True:

        apoapsis = ksp.Orbit.Apoapsis - 600000
        periapsis = ksp.Orbit.Periapsis - 600000
        prevEccentricity = eccentricity;
        eccentricity = ksp.Orbit.Eccentricity

        print '  Orbit = %.1f km x %.1f km (e = %.3f)' % ((apoapsis/1000), (periapsis/1000), ksp.Orbit.Eccentricity)

        print (prevEccentricity - eccentricity)
        if prevEccentricity - eccentricity < 0:
            break

        if eccentricity > 0.1:
            time.sleep(1)
        else:
            # We are close, so reduce throttle and check eccentricity more frequently
            ksp.Control.Throttle = 0.1
            time.sleep(0.2)

    ksp.Control.Throttle = 0
    ksp.Control.SAS = False

    print 'Program complete'

if __name__ == "__main__":
    main()
