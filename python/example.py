#!/usr/bin/env python2

import krpc
import time

def main():
    # Connect to the server with the default settings
    # (IP address 127.0.0.1 and port 50000)
    ksp = krpc.connect(name='Example script')
    print 'Connected to server, version', ksp.KRPC.GetStatus().version

    # Set the throttle to 100% and enable SAS
    controls = ksp.Control.GetControlInputs()
    controls.throttle = 1
    controls.sas = True
    ksp.Control.SetControlInputs(controls)

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    ksp.Control.ActivateNextStage()

    # Ascend to 10km
    print 'Vertical Ascent...'
    srbs_separated = False
    while True:

        # Check altitude
        altitude = ksp.Flight.GetFlightData().altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 10000:
            break

        # Check if the solid boosters need to be ditched
        # We assume this will happen before we reach 10km
        if not srbs_separated:
            resources = ksp.Vessel.GetResources()
            print '  Solid fuel = %.1f T' % resources.solidFuel
            if resources.solidFuel < 0.1:
                print '  SRB separation!'
                ksp.Control.ActivateNextStage()
                srbs_separated = True

        time.sleep(1)

    print 'Gravity turn...'
    # Pitch the vessel west for 4 seconds, then hold position using SAS
    # TODO: this doesn't work
    controls.sas = False
    controls.pitch = 1
    ksp.Control.SetControlInputs(controls)
    time.sleep(3)
    controls.sas = True
    controls.pitch = 0
    ksp.Control.SetControlInputs(controls)

    # Raise apoapsis to above 80km
    while True:

        # Apoapsis is relative to the center of Kerbin, so subtract 600km
        apoapsis = ksp.Orbit.GetOrbitData().apoapsis - 600000
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            break

        time.sleep(1)

    # Disable the control inputs
    controls.sas = False
    controls.throttle = 0
    ksp.Control.SetControlInputs(controls)

    print 'Program complete'

if __name__ == "__main__":
    main()
