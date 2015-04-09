Docking Guidance
================

The following script outputs docking guidance information. It waits until the
vessel is being controlled from a docking port, and a docking port is set as the
current target. It then prints out information about speeds and distances
relative to the docking axis.

It uses `numpy <http://www.numpy.org>`_ to do linear algebra on the vectors
returned by kRPC -- for example computing the dot product or length of a vector
-- and uses `curses <https://docs.python.org/2/howto/curses.html>`_ for terminal
output.

.. code-block:: python

   import krpc, curses, time, sys
   import numpy as np
   import numpy.linalg as la

   # Set up curses
   stdscr = curses.initscr()
   curses.nocbreak()
   stdscr.keypad(1)
   curses.noecho()

   try:

       # Connect to kRPC
       conn = krpc.connect(name='Docking Guidance')
       vessel = conn.space_center.active_vessel
       current = None
       target = None

       while True:

           stdscr.clear()
           stdscr.addstr(0,0,'-- Docking Guidance --')

           current = conn.space_center.active_vessel.parts.controlling.docking_port
           target = conn.space_center.target_docking_port

           if current is None:
               stdscr.addstr(2,0,'Awaiting control from docking port...')

           elif target is None:
               stdscr.addstr(2,0,'Awaiting target docking port...')

           else:
               # Get positions, distances, velocities and speeds relative to the target docking port
               current_position = current.position(target.reference_frame)
               velocity = current.part.velocity(target.reference_frame)
               displacement = np.array(current_position)
               distance = la.norm(displacement)
               speed = la.norm(np.array(velocity))

               # Get speeds and distances relative to the docking axis
               # (the direction the target docking port is facing in)

               # Axial = along the docking axis
               axial_displacement = np.copy(displacement)
               axial_displacement[0] = 0
               axial_displacement[2] = 0
               axial_distance = axial_displacement[1]
               axial_velocity = np.copy(velocity)
               axial_velocity[0] = 0
               axial_velocity[2] = 0
               axial_speed = axial_velocity[1]
               if axial_distance > 0:
                   axial_speed *= -1

               # Radial = perpendicular to the docking axis
               radial_displacement = np.copy(displacement)
               radial_displacement[1] = 0
               radial_distance = la.norm(radial_displacement)
               radial_velocity = np.copy(velocity)
               radial_velocity[1] = 0
               radial_speed = la.norm(radial_velocity)
               if np.dot(radial_velocity, radial_displacement) > 0:
                   radial_speed *= -1

               # Get the docking port state
               if current.state == conn.space_center.DockingPortState.ready:
                   state = 'Ready to dock'
               elif current.state == conn.space_center.DockingPortState.docked:
                   state = 'Docked'
               elif current.state == conn.space_center.DockingPortState.docking:
                   state = 'Docking...'
               else:
                   state = 'Unknown'

               # Output information
               stdscr.addstr(2,0,'Current ship: {:30}'.format(current.part.vessel.name[:30]))
               stdscr.addstr(3,0,'Current port: {:30}'.format(current.part.title[:30]))
               stdscr.addstr(5,0,'Target ship:  {:30}'.format(target.part.vessel.name[:30]))
               stdscr.addstr(6,0,'Target port:  {:30}'.format(target.part.title[:30]))
               stdscr.addstr(8,0,'Status: {:10}'.format(state))
               stdscr.addstr(10, 0, '          +---------------------------+')
               stdscr.addstr(11, 0, '          |  Distance  |  Speed       |')
               stdscr.addstr(12, 0, '+---------+------------+--------------+')
               stdscr.addstr(13, 0, '|         |  {:>+6.2f} m  |  {:>+6.2f} m/s  |'.format(distance, speed))
               stdscr.addstr(14, 0, '|   Axial |  {:>+6.2f} m  |  {:>+6.2f} m/s  |'.format(axial_distance, axial_speed))
               stdscr.addstr(15, 0, '|  Radial |  {:>+6.2f} m  |  {:>+6.2f} m/s  |'.format(radial_distance, radial_speed))
               stdscr.addstr(16, 0, '+---------+------------+--------------+')

           stdscr.refresh()
           time.sleep(0.25)

   finally:
       # Shutdown curses
       curses.nocbreak()
       stdscr.keypad(0)
       curses.echo()
       curses.endwin()
