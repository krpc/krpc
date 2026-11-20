.. currentmodule:: SpaceCenter

Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital flight, and return
them safely to Kerbin. It covers the following topics:

* Controlling a rocket, by activating stages and setting the throttle
* Using the autopilot to point the vessel in a direction
* Using events to wait for things to happen in the game
* Tracking the amount of resources in the vessel
* Tracking flight data, such as altitude and vertical speed
* Setting a part's properties, such as a parachute's deploy altitude

.. note::

   For details on how to write scripts and connect to kRPC, see the :ref:`getting-started`
   guide.

The tutorial flies the Kerbal 2, one of the game's stock craft. Load it in the Vehicle Assembly
Building and launch it to the pad before you run the script.

The tutorial includes source code for the main client languages kRPC supports. Download the
whole program in your chosen language:

:download:`Python</scripts/tutorials/suborbital-flight/SubOrbitalFlight.py>`,
:download:`C#</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs>`,
:download:`C++</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp>`,
:download:`C</scripts/tutorials/suborbital-flight/SubOrbitalFlight.c>`,
:download:`Java</scripts/tutorials/suborbital-flight/SubOrbitalFlight.java>`,
:download:`Lua</scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua>`,
:download:`JavaScript</scripts/tutorials/suborbital-flight/SubOrbitalFlight.js>`

Preparing for Launch
--------------------

First we open a connection to the server, passing a name for the script. The name appears in the
server window in the game. The C program also defines ``sleep_ms``, which its polling loops pause
with:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: conn = krpc.connect(name="Sub-orbital flight")
         :end-at: conn = krpc.connect(name="Sub-orbital flight")

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var conn = new Connection ("Sub-orbital flight");
         :end-at: var conn = new Connection ("Sub-orbital flight");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: krpc::Connection conn = krpc::connect("Sub-orbital flight");
         :end-at: krpc::services::SpaceCenter space_center(conn);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :end-before: krpc_SpaceCenter_Vessel_t vessel;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: Connection connection = Connection.newInstance("Sub-orbital flight");
         :end-at: SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :end-at: local conn = krpc.connect('Sub-orbital flight')

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const conn = await connect({ name: "Sub-orbital flight" });
         :end-at: const conn = await connect({ name: "Sub-orbital flight" });

Next we get an object representing the active vessel. We send instructions to the rocket through
this object:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: vessel = conn.space_center.active_vessel
         :end-at: vessel = conn.space_center.active_vessel

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

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Vessel_t vessel;
         :end-at: krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);

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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const vessel = await conn.spaceCenter.getActiveVessel();
         :end-at: const control = await vessel.getControl();

Then we prepare the rocket for launch. This sets the throttle to half, and has the autopilot hold
a pitch and heading of 90°, pointing straight up. It then waits a second for the settings to take
effect:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: vessel.auto_pilot.target_pitch_and_heading(90, 90)
         :end-at: time.sleep(1)

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

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90,
         :end-at: sleep_ms(1000);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: vessel.getAutoPilot().targetPitchAndHeading(90, 90);
         :end-at: Thread.sleep(1000);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: vessel.auto_pilot:target_pitch_and_heading(90, 90)
         :end-at: socket.sleep(1)

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: await autoPilot.targetPitchAndHeading(90, 90);
         :end-at: await new Promise((resolve) => setTimeout(resolve, 1000));

Lift-off!
---------

We launch by activating the first stage, which is the same as pressing the space bar. This lights
the engine and releases the launch clamps:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Launch!")
         :end-at: vessel.control.activate_next_stage()

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

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Launch!\n");
         :end-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: console.log("Launch!");
         :end-at: await control.activateNextStage();

Pitching Over
-------------

Once the rocket has cleared the launch pad, it starts to turn. We wait for an event that fires at
an altitude of 500 m:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: flight = vessel.flight()
         :end-before: print("Pitching over")

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var flight = vessel.Flight ();
         :end-before: Console.WriteLine ("Pitching over");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: auto e = krpc::services::KRPC::Expression::bind(conn);
         :end-before: std::cout << "Pitching over" << std::endl;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Flight_t flight;
         :end-before: printf("Pitching over\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: Expression.Bound e = Expression.bind(connection);
         :end-before: System.out.println("Pitching over");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel:flight().mean_altitude < 500 do
         :end-before: print('Pitching over')

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const flight = await vessel.flight();
         :end-before: console.log("Pitching over");

``vessel.flight()`` returns a :class:`Flight` object, which gives information about the rocket in
flight, such as its altitude, direction and velocity.

The event is a :ref:`server side function <server-side-functions>` that compares the altitude
with 500. The server checks it every physics tick, and the script sleeps until it fires.

The Python, C# and JavaScript clients compile the lambda into the function. The C++ and Java
clients build it from expression objects: an embedded call to ``mean_altitude``, a constant and a
comparison. The C and Lua clients do not support events, so their scripts poll in a loop, with a
short pause on each pass.

Now we tilt the rocket 20° from vertical, to a pitch of 70°, and hold a heading of 90°, due east.
A target smoothing time of 10 seconds turns the rocket over gradually, rather than all at once:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Pitching over")
         :end-at: vessel.auto_pilot.target_pitch_and_heading(70, 90)

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: Console.WriteLine ("Pitching over");
         :end-at: vessel.AutoPilot.TargetPitchAndHeading (70, 90);

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: std::cout << "Pitching over" << std::endl;
         :end-at: vessel.auto_pilot().target_pitch_and_heading(70, 90);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Pitching over\n");
         :end-before: krpc_SpaceCenter_Resources_t resources;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: System.out.println("Pitching over");
         :end-at: vessel.getAutoPilot().targetPitchAndHeading(70, 90);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: print('Pitching over')
         :end-at: vessel.auto_pilot:target_pitch_and_heading(70, 90)

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: console.log("Pitching over");
         :end-at: await autoPilot.targetPitchAndHeading(70, 90);

The engine burns until the fuel tank is empty. We wait for an event that fires when less than 0.1
units of liquid fuel are left:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: event = conn.add_event(lambda: vessel.resources.amount("LiquidFuel") < 0.1)
         :end-before: print("Flame out")

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-after: vessel.AutoPilot.TargetPitchAndHeading (70, 90);
         :end-before: Console.WriteLine ("Flame out");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-after: vessel.auto_pilot().target_pitch_and_heading(70, 90);
         :end-before: std::cout << "Flame out" << std::endl;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Resources_t resources;
         :end-before: printf("Flame out\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-after: vessel.getAutoPilot().targetPitchAndHeading(70, 90);
         :end-before: System.out.println("Flame out");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel.resources:amount('LiquidFuel') > 0.1 do
         :end-before: print('Flame out')

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const fuelEvent = await conn.addEvent(
         :end-before: console.log("Flame out");

``vessel.resources`` returns a :class:`Resources` object, which gives the amounts of the
resources in the rocket.

At flame out we cut the throttle, and the rocket coasts up to its apoapsis. The autopilot now
points the rocket prograde, so it follows its arc:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Flame out")
         :end-at: vessel.auto_pilot.target_direction = (0, 1, 0)

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: Console.WriteLine ("Flame out");
         :end-at: vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, 1.0, 0.0);

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: std::cout << "Flame out" << std::endl;
         :end-at: vessel.auto_pilot().set_target_direction(std::make_tuple(0.0, 1.0, 0.0));

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Flame out\n");
         :end-before: krpc_SpaceCenter_Orbit_t orbit;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: System.out.println("Flame out");
         :end-at: new Triplet<Double, Double, Double>(0.0, 1.0, 0.0));

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: print('Flame out')
         :end-at: vessel.auto_pilot.target_direction = {0, 1, 0}

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: console.log("Flame out");
         :end-at: await autoPilot.setTargetDirection([0, 1, 0]);

``vessel.surface_velocity_reference_frame`` has its y-axis along the rocket's velocity relative to
the surface. So the direction ``(0, 1, 0)`` in this frame points prograde. The frame turns with the
velocity, and the rocket turns with it.

Returning Safely to Kerbin
--------------------------

We separate the capsule at apoapsis, where the rocket stops climbing. This waits for an event that
fires when the vertical speed falls below zero:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: body_flight = vessel.flight(vessel.orbit.body.reference_frame)
         :end-before: print("Apoapsis reached, separating")

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: var bodyFlight = vessel.Flight (vessel.Orbit.Body.ReferenceFrame);
         :end-before: Console.WriteLine ("Apoapsis reached, separating");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: auto body_flight = vessel.flight(vessel.orbit().body().reference_frame());
         :end-before: std::cout << "Apoapsis reached, separating" << std::endl;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_Orbit_t orbit;
         :end-before: printf("Apoapsis reached, separating\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: Flight bodyFlight =
         :end-before: System.out.println("Apoapsis reached, separating");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: local body_frame = vessel.orbit.body.reference_frame
         :end-before: print('Apoapsis reached, separating')

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const orbit = await vessel.getOrbit();
         :end-before: console.log("Apoapsis reached, separating");

This calls ``vessel.flight()`` again, now with a :class:`ReferenceFrame`. We want the vertical
speed relative to the surface of Kerbin, so the flight object has to give values in a frame fixed
to Kerbin. ``vessel.orbit.body.reference_frame`` has its origin at the center of Kerbin, and
rotates with the planet. See the :ref:`reference-frames` guide for more.

Then we separate the capsule, and have the autopilot turn it retrograde. ``wait`` blocks until the
capsule points there. We then disengage the autopilot, and the capsule's drag keeps it pointing
retrograde as it falls:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: print("Apoapsis reached, separating")
         :end-at: vessel.auto_pilot.engaged = False

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: Console.WriteLine ("Apoapsis reached, separating");
         :end-at: vessel.AutoPilot.Engaged = false;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: std::cout << "Apoapsis reached, separating" << std::endl;
         :end-at: vessel.auto_pilot().set_engaged(false);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: printf("Apoapsis reached, separating\n");
         :end-at: krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: System.out.println("Apoapsis reached, separating");
         :end-at: vessel.getAutoPilot().setEngaged(false);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: print('Apoapsis reached, separating')
         :end-at: vessel.auto_pilot.engaged = false

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: console.log("Apoapsis reached, separating");
         :end-at: await autoPilot.setEngaged(false);

The Kerbals are now on a collision course with the surface. We wait until they fall to 2 km above
the surface, then deploy the parachute. You can use time warp to skip ahead to just before this
happens, and the script carries on working:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: event = conn.add_event(lambda: flight.surface_altitude < 2000)
         :end-at: vessel.control.activate_next_stage()

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

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-after: krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);
         :end-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-after: vessel.getAutoPilot().setEngaged(false);
         :end-at: vessel.getControl().activateNextStage();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: while vessel:flight().surface_altitude > 2000 do
         :end-at: vessel.control:activate_next_stage()

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const surfaceEvent = await conn.addEvent(
         :end-at: await control.activateNextStage();

The parachute semi-deploys as soon as it is safe, which slows the capsule a little. It opens fully
at its deploy altitude, 1000 m by default. We lower it to 550 m, so the capsule spends less time
hanging under its parachute.

Finally, this prints the altitude of the capsule once a second, until it lands or splashes down:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :start-at: situation = conn.space_center.VesselSituation

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :start-at: while (vessel.Situation != VesselSituation.Landed &&
         :end-at: Console.WriteLine ("Landed!");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :start-at: using krpc::services::SpaceCenter;
         :end-at: std::cout << "Landed!" << std::endl;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :start-at: krpc_SpaceCenter_VesselSituation_t situation;
         :end-at: printf("Landed!\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :start-at: while (vessel.getSituation() != VesselSituation.LANDED &&
         :end-at: System.out.println("Landed!");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :start-at: local landed = conn.space_center.VesselSituation.landed.value

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.js
         :language: javascript
         :start-at: const { landed, splashed } = conn.spaceCenter.VesselSituation;
         :end-before: await conn.close();

Your Kerbals should now be safely back on Kerbin.
