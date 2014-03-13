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

    vessel = ksp.SpaceCenter.ActiveVessel
    orbit = vessel.Orbit
    control = vessel.Control
    flight = vessel.Flight
    resources = vessel.Resources

    # Set the throttle to 100% and enable SAS
    control.Throttle = 1
    control.SAS = True

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    control.ActivateNextStage()

    # Ascend to 10km and ditch SRBs when they're empty
    print 'Vertical ascent...'
    srbs_separated = False
    while True:

        # Check altitude, exit loop if higher than 10km
        altitude = flight.Altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 10000:
            break

        # Check if the solid boosters need to be ditched
        # (We assume this will happen before we reach 10km)
        if not srbs_separated:
            solidFuel = resources.GetResource('SolidFuel')
            print '  Solid fuel = %.1f T' % solidFuel
            if solidFuel < 0.1:
                print '  SRB separation!'
                control.ActivateNextStage()
                srbs_separated = True

        time.sleep(1)

    # Disable SAS, pitch the vessel to 50 degrees to the west, then hold position using SAS
    print 'Gravity turn...'
    control.SAS = False
    control.Yaw = 0.1
    while flight.Pitch > 50:
        time.sleep(0.25)
    control.Yaw = 0
    control.SAS = True

    # Raise apoapsis to above 80km
    while True:

        # Apoapsis is relative to the center of Kerbin, so subtract 600km
        apoapsis = orbit.Apoapsis - 600000
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            break

        time.sleep(1)

    # Disable the control inputs and coast to apoapsis
    print 'Coasting to apoapsis...'
    control.SAS = False
    control.Throttle = 0
    while True:

        # Check altitude, exit loop if higher than 79km
        altitude = flight.Altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 79000:
            break
        time.sleep(1)

    # Circularize the orbit
    print 'Circularizing...'
    control.SAS = False
    control.Yaw = 0.1
    while flight.Pitch > 0:
        time.sleep(0.25)
    control.Yaw = 0
    control.SAS = True
    # Raise periapsis until eccentricity starts the increase (happens just after reaching 0)
    control.Throttle = 1

    eccentricity = orbit.Eccentricity
    time.sleep(1)

    while True:

        apoapsis = orbit.Apoapsis - 600000
        periapsis = orbit.Periapsis - 600000
        prevEccentricity = eccentricity;
        eccentricity = orbit.Eccentricity

        print '  Orbit = %.1f km x %.1f km (e = %.3f)' % ((apoapsis/1000), (periapsis/1000), orbit.Eccentricity)

        if prevEccentricity - eccentricity < 0:
            break

        if eccentricity > 0.1:
            time.sleep(1)
        else:
            # We are close, so reduce throttle and check eccentricity more frequently
            control.Throttle = 0.1
            time.sleep(0.2)

    control.Throttle = 0
    control.SAS = False

    print 'Program complete'

if __name__ == "__main__":
    main()
