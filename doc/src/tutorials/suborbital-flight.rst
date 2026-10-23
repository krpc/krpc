.. currentmodule:: SpaceCenter

Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital flight, and return
them safely to Kerbin. It covers the following topics:

* Controlling a rocket, by activating stages and setting the throttle
* Using the autopilot to point the vessel in a direction
* Using events to wait for things to happen in the game
* Tracking the amount of resources in the vessel
* Tracking flight and orbital data, such as altitude and apoapsis altitude

.. note::

   For details on how to write scripts and connect to kRPC, see the :ref:`getting-started`
   guide.

The tutorial uses the two stage rocket pictured below. :download:`Download the craft file
</crafts/SubOrbitalFlight.craft>` to fly it yourself.

The tutorial includes source code for the main client languages kRPC supports. Download the
whole program in your chosen language:

:download:`C</scripts/tutorials/suborbital-flight/SubOrbitalFlight.c>`,
:download:`C#</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs>`,
:download:`C++</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp>`,
:download:`Java</scripts/tutorials/suborbital-flight/SubOrbitalFlight.java>`,
:download:`Lua</scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua>`,
:download:`Python</scripts/tutorials/suborbital-flight/SubOrbitalFlight.py>`

.. image:: /images/tutorials/SubOrbitalFlight.png
   :align: center

Part One: Preparing for Launch
------------------------------

First we open a connection to the server, passing a name for the script. The name appears in the
server window in the game:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_connection_t conn;
         :end-at: krpc_connect(conn, "Sub-orbital flight");

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var conn = new Connection ("Sub-orbital flight");
         :end-at: var conn = new Connection ("Sub-orbital flight");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: krpc::Client conn = krpc::connect("Sub-orbital flight");
         :end-at: krpc::services::SpaceCenter space_center(&conn);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: Connection connection = Connection.newInstance("Sub-orbital flight");
         :end-at: SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :end-at: local conn = krpc.connect('Sub-orbital flight')

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: conn = krpc.connect(name="Sub-orbital flight")
         :end-at: conn = krpc.connect(name="Sub-orbital flight")

Next we get an object representing the active vessel. We send instructions to the rocket through
this object:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Vessel_t vessel;
         :end-at: krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var vessel = conn.SpaceCenter ().ActiveVessel;
         :end-at: var vessel = conn.SpaceCenter ().ActiveVessel;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: auto vessel = space_center.active_vessel().value();
         :end-at: auto vessel = space_center.active_vessel().value();

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();
         :end-at: SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: local vessel = conn.space_center.active_vessel
         :end-at: local vessel = conn.space_center.active_vessel

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: vessel = conn.space_center.active_vessel
         :end-at: vessel = conn.space_center.active_vessel

Then we prepare the rocket for launch. This sets the throttle to maximum, and has the autopilot
hold a pitch and heading of 90°, pointing straight up. It then waits a second for the settings to
take effect:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90, 90);
         :end-at: sleep(1);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: vessel.AutoPilot.TargetPitchAndHeading (90, 90);
         :end-at: System.Threading.Thread.Sleep (1000);

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: vessel.auto_pilot().target_pitch_and_heading(90, 90);
         :end-at: std::this_thread::sleep_for(std::chrono::seconds(1));

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: vessel.getAutoPilot().targetPitchAndHeading(90, 90);
         :end-at: Thread.sleep(1000);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: vessel.auto_pilot:target_pitch_and_heading(90, 90)
         :end-at: platform.sleep(1)

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: vessel.auto_pilot.target_pitch_and_heading(90, 90)
         :end-at: time.sleep(1)

Part Two: Lift-off!
-------------------

We launch by activating the first stage, which is the same as pressing the space bar:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Launch!\n");
         :end-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: Console.WriteLine ("Launch!");
         :end-at: vessel.Control.ActivateNextStage ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: std::cout << "Launch!" << std::endl;
         :end-at: vessel.control().activate_next_stage();

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: System.out.println("Launch!");
         :end-at: vessel.getControl().activateNextStage();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: print('Launch!')
         :end-at: vessel.control:activate_next_stage()

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Launch!")
         :end-at: vessel.control.activate_next_stage()

The solid fuel boosters soon run out, and the rocket has to jettison them. We wait for an event
that fires when less than 0.1 units of solid fuel are left, then activate the next stage:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Resources_t resources;
         :end-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-after: vessel.Control.ActivateNextStage ();
         :end-at: vessel.Control.ActivateNextStage ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: typedef krpc::services::KRPC::Expression Expr;
         :end-at: vessel.control().activate_next_stage();

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-after: vessel.getControl().activateNextStage();
         :end-at: vessel.getControl().activateNextStage();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel.resources:amount('SolidFuel') > 0.1 do
         :end-at: vessel.control:activate_next_stage()

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: event = conn.add_event(lambda: vessel.resources.amount("SolidFuel") < 0.1)
         :end-at: vessel.control.activate_next_stage()

``vessel.resources`` returns a :class:`Resources` object, which gives the amounts of the
resources in the rocket. The event is a :ref:`server side function <server-side-functions>`
that compares the amount of solid fuel with 0.1. The server checks it every physics tick, and
the script sleeps until it fires.

The Python and C# clients compile the lambda into the function. The C++ and Java clients build it
from expression objects: an embedded call to ``amount``, a constant and a comparison. The C and
Lua clients do not support events, so their scripts poll the amount in a loop.

Part Three: Reaching Apoapsis
-----------------------------

Next we start a gravity turn once the rocket is high enough. This waits for an event that fires
at an altitude of 10 km:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Flight_t flight;
         :end-before: printf("Gravity turn\n");

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var flight = vessel.Flight ();
         :end-before: Console.WriteLine ("Gravity turn");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: auto flight = vessel.flight();
         :end-before: std::cout << "Gravity turn" << std::endl;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: Flight flight = vessel.flight(null);
         :end-before: System.out.println("Gravity turn");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel:flight().mean_altitude < 10000 do
         :end-before: print('Gravity turn')

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: flight = vessel.flight()
         :end-before: print("Gravity turn")

``vessel.flight()`` returns a :class:`Flight` object, which gives information about the rocket in
flight, such as its altitude, direction and velocity.

Now we tilt the rocket over to a pitch of 60°, and hold a heading of 90°, due east. The autopilot
takes the new target:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Gravity turn\n");
         :end-at: krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 60, 90);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: Console.WriteLine ("Gravity turn");
         :end-at: vessel.AutoPilot.TargetPitchAndHeading (60, 90);

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: std::cout << "Gravity turn" << std::endl;
         :end-at: vessel.auto_pilot().target_pitch_and_heading(60, 90);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: System.out.println("Gravity turn");
         :end-at: vessel.getAutoPilot().targetPitchAndHeading(60, 90);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: print('Gravity turn')
         :end-at: vessel.auto_pilot:target_pitch_and_heading(60, 90)

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Gravity turn")
         :end-at: vessel.auto_pilot.target_pitch_and_heading(60, 90)

Next we wait for an event that fires when the apoapsis reaches 100 km. Then we cut the throttle,
jettison the launch stage and turn off the autopilot:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Orbit_t orbit;
         :end-at: krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-after: vessel.AutoPilot.TargetPitchAndHeading (60, 90);
         :end-at: vessel.AutoPilot.Engaged = false;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-after: vessel.auto_pilot().target_pitch_and_heading(60, 90);
         :end-at: vessel.auto_pilot().set_engaged(false);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-after: vessel.getAutoPilot().targetPitchAndHeading(60, 90);
         :end-at: vessel.getAutoPilot().setEngaged(false);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel.orbit.apoapsis_altitude < 100000 do
         :end-at: vessel.auto_pilot.engaged = false

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: event = conn.add_event(lambda: vessel.orbit.apoapsis_altitude > 100000)
         :end-at: vessel.auto_pilot.engaged = False

``vessel.orbit`` returns an :class:`Orbit` object, which describes the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

The Kerbals are now on a sub-orbital trajectory, on a collision course with the surface. We wait
until they fall to 1 km above the surface, then deploy the parachutes. You can use time warp to
skip ahead to just before this happens, and the script carries on working:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-after: krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);
         :end-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-after: vessel.AutoPilot.Engaged = false;
         :end-at: vessel.Control.ActivateNextStage ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-after: vessel.auto_pilot().set_engaged(false);
         :end-at: vessel.control().activate_next_stage();

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-after: vessel.getAutoPilot().setEngaged(false);
         :end-at: vessel.getControl().activateNextStage();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel:flight().surface_altitude > 1000 do
         :end-at: vessel.control:activate_next_stage()

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: event = conn.add_event(lambda: flight.surface_altitude < 1000)
         :end-at: vessel.control.activate_next_stage()

The parachutes are now deployed. This prints the altitude of the capsule once a second, until its
vertical speed reaches zero when it lands:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_CelestialBody_t body;
         :end-at: printf("Landed!\n");

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: while (vessel.Flight (vessel.Orbit.Body.ReferenceFrame).VerticalSpeed < -0.1) {
         :end-at: Console.WriteLine ("Landed!");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: while (vessel.flight(vessel.orbit().body().reference_frame()).vertical_speed() < -0.1) {
         :end-at: std::cout << "Landed!" << std::endl;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: while (vessel.flight(vessel.getOrbit().getBody().getReferenceFrame()).getVerticalSpeed() < -0.1) {
         :end-at: System.out.println("Landed!");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel:flight(vessel.orbit.body.reference_frame).vertical_speed < -0.1 do

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: while vessel.flight(vessel.orbit.body.reference_frame).vertical_speed < -0.1:

This calls ``vessel.flight()`` again, now with a :class:`ReferenceFrame`. We want the vertical
speed relative to the surface of Kerbin, so the flight object has to give values in a frame fixed
to Kerbin. ``vessel.orbit.body.reference_frame`` has its origin at the center of Kerbin, and
rotates with the planet. See the :ref:`reference-frames` tutorial for more.

Your Kerbals should now be safely back on the surface.
