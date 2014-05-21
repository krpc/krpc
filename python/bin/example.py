#!/usr/bin/env python2

"""
This example script is an autopilot that will launch the supplied Example.craft
(a 3-stage rocket) and take it into a (roughly) circular orbit at 80km.
"""

import krpc
import time
import math

turn_start_altitude = 250
turn_end_altitude = 70000
target_altitude = 80000

def main():
    # Connect to the server with the default settings
    # (IP address 127.0.0.1 and port 50000)
    print 'Connecting to server...'
    ksp = krpc.connect(name='Example script')
    print 'Connected to server, version', ksp.krpc.get_status().version

    vessel = ksp.space_center.active_vessel
    orbit = vessel.orbit
    control = vessel.control
    auto_pilot = vessel.auto_pilot
    flight = vessel.flight()
    resources = vessel.resources

    # Set the throttle to 100% and enable SAS
    control.throttle = 1

    # Countdown...
    print '3'; time.sleep(1)
    print '2'; time.sleep(1)
    print '1'; time.sleep(1)

    # Activate the first stage
    print 'Launch!'
    control.activate_next_stage()
    auto_pilot.set_rotation(90, 90)

    time.sleep(1)

    srbs_separated = False
    stage_separated = False
    while True:

        alt = flight.true_altitude
        if alt > turn_start_altitude and alt < turn_end_altitude:
            frac = (alt - turn_start_altitude) / (turn_end_altitude - turn_start_altitude)
            frac *= 12
            frac -= 6
            sig = 1 / (1 + math.exp(-frac))
            turn = sig * 90

            auto_pilot.set_rotation(90-turn, 90)

        # Separate SRBs when finished
        if not srbs_separated:
            solid_fuel = resources.amount('SolidFuel', stage=3, cumulative=False) - 64
            print '  Solid fuel = %.1f T' % solid_fuel
            if solid_fuel < 0.1:
                print '  SRB separation!'
                control.activate_next_stage()
                srbs_separated = True

        # Separate launch stage when finished
        if srbs_separated and not stage_separated:
            liquid_fuel = resources.amount('LiquidFuel', stage=2, cumulative=False)
            oxidizer = resources.amount('Oxidizer', stage=2, cumulative=False)
            print '  LF = %.1f T, Ox = %.1f T' % (liquid_fuel, oxidizer)
            if liquid_fuel < 0.1:
                print '  Stage separation!'
                control.activate_next_stage()
                stage_separated = True

        # Disable engines when 80km apoapsis is reached
        apoapsis = orbit.apoapsis_altitude
        print '  Apoapsis = %.1f km' % (apoapsis/1000)
        if apoapsis > 80000:
            control.throttle = 0
            break

        time.sleep(1)

    # Point at 0 degrees pitch, west
    auto_pilot.set_rotation(0, 90)

    # Wait until altitude is higher than 79km
    print 'Coasting to apoapsis...'
    while True:
        altitude = flight.true_altitude
        print '  Altitude = %.1f km' % (altitude/1000)
        if altitude > 79000:
            break
        time.sleep(1)

    # Circularize the orbit by raising periapsis until eccentricity
    # starts to increase (which happens just after reaching 0)
    print 'Circularizing...'
    control.throttle = 1
    eccentricity = orbit.eccentricity
    time.sleep(1)

    while True:
        apoapsis = orbit.apoapsis_altitude
        periapsis = orbit.periapsis_altitude
        prev_eccentricity = eccentricity
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
    auto_pilot.disengage()
    print 'Program complete'

if __name__ == "__main__":
    main()
