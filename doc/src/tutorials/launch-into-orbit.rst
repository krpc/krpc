Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The craft
file for the rocket can be :download:`downloaded here
</crafts/LaunchIntoOrbit.craft>` and the entire python script :download:`from
here </scripts/LaunchIntoOrbit.py>`.

The following code connects to the server, gets the active vessel, sets up a
bunch streams to get flight telemetry then prepares the rocket for launch.

.. code-block:: python

   import krpc, time, math

   turn_start_altitude = 250
   turn_end_altitude = 45000
   target_altitude = 150000

   conn = krpc.connect(name='Launch into orbit')
   vessel = conn.space_center.active_vessel

   # Set up streams for telemetry
   ut = conn.add_stream(getattr, conn.space_center, 'ut')
   altitude = conn.add_stream(getattr, vessel.flight(), 'mean_altitude')
   apoapsis = conn.add_stream(getattr, vessel.orbit, 'apoapsis_altitude')
   periapsis = conn.add_stream(getattr, vessel.orbit, 'periapsis_altitude')
   eccentricity = conn.add_stream(getattr, vessel.orbit, 'eccentricity')
   srb_fuel = conn.add_stream(vessel.resources.amount, 'SolidFuel', stage=3, cumulative=False)
   launcher_fuel = conn.add_stream(vessel.resources.amount, 'LiquidFuel', stage=2, cumulative=False)

   # Pre-launch setup
   vessel.control.sas = False
   vessel.control.rcs = False
   vessel.control.throttle = 1

   # Countdown...
   print('3...'); time.sleep(1)
   print('2...'); time.sleep(1)
   print('1...'); time.sleep(1)
   print('Launch!')

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. code-block:: python

   # Activate the first stage
   vessel.control.activate_next_stage()
   vessel.auto_pilot.set_rotation(90, 90)

   # Main ascent loop
   srbs_separated = False
   turn_angle = 0
   while True:

       # Gravity turn
       if altitude() > turn_start_altitude and altitude() < turn_end_altitude:
           frac = (altitude() - turn_start_altitude) / (turn_end_altitude - turn_start_altitude)
           new_turn_angle = frac * 90
           if abs(new_turn_angle - turn_angle) > 0.5:
               turn_angle = new_turn_angle
               vessel.auto_pilot.set_rotation(90-turn_angle, 90)

       # Separate SRBs when finished
       if not srbs_separated:
           if srb_fuel() < 0.1:
               vessel.control.activate_next_stage()
               srbs_separated = True
               print('SRBs separated')

       # Decrease throttle when approaching target apoapsis
       if apoapsis() > target_altitude*0.9:
           print('Approaching target apoapsis')
           break

Next, the program fine tunes the apoapsis, using 10% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. code-block:: python

   # Disable engines when target apoapsis is reached
   vessel.control.throttle = 0.25
   while apoapsis() < target_altitude:
       pass
   print('Target apoapsis reached')
   vessel.control.throttle = 0

   # Wait until out of atmosphere
   print('Coasting out of atmosphere')
   while altitude() < 70500:
       pass

It is now time to plan the circularization burn. First, we calculate the delta-v
required to circularize the orbit using the `vis-viva equation
<http://en.wikipedia.org/wiki/Vis-viva_equation>`_. We then calculate the burn
time needed to achieve this delta-v, using the `Tsiolkovsky rocket equation
<http://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation>`_.

.. code-block:: python

   # Plan circularization burn (using vis-viva equation)
   print('Planning circularization burn')
   mu = vessel.orbit.body.gravitational_parameter
   r = vessel.orbit.apoapsis
   a1 = vessel.orbit.semi_major_axis
   a2 = r
   v1 = math.sqrt(mu*((2./r)-(1./a1)))
   v2 = math.sqrt(mu*((2./r)-(1./a2)))
   delta_v = v2 - v1
   node = vessel.control.add_node(ut() + vessel.orbit.time_to_apoapsis, prograde=delta_v)

   # Calculate burn time (using rocket equation)
   F = vessel.thrust
   Isp = vessel.specific_impulse * 9.82
   m0 = vessel.mass
   m1 = m0 / math.exp(delta_v/Isp)
   flow_rate = F / Isp
   burn_time = (m0 - m1) / flow_rate

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. code-block:: python

   # Orientate ship
   print('Orientating ship for circularization burn')
   vessel.auto_pilot.set_direction((0,1,0), reference_frame=node.reference_frame, wait=True)

   # Wait until burn
   print('Waiting until circularization burn')
   burn_ut = ut() + vessel.orbit.time_to_apoapsis - (burn_time/2.)
   lead_time = 5
   conn.space_center.warp_to(burn_ut - lead_time)

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. code-block:: python

   # Execute burn
   print('Ready to execute burn')
   time_to_apoapsis = conn.add_stream(getattr, vessel.orbit, 'time_to_apoapsis')
   while time_to_apoapsis() - (burn_time/2.) > 0:
       pass
   print('Executing burn')
   vessel.control.throttle = 1
   time.sleep(burn_time - 0.1)
   print('Fine tuning')
   vessel.control.throttle = 0.05
   remaining_burn = conn.add_stream(node.remaining_burn_vector, node.reference_frame)
   while remaining_burn()[1] > 0:
       pass
   vessel.control.throttle = 0
   node.remove()

   print('Launch complete')

The rocket should now be in a circular 150km orbit above Kerbin.
