.. currentmodule:: SpaceCenter

Docking
=======

This tutorial docks two vessels in orbit. The script flies one vessel's docking port into the
port of another, and draws its approach in the game. It covers the following topics:

* Measuring positions and velocities in a docking port's reference frame
* Pointing the vessel with the autopilot
* Moving the vessel with its RCS thrusters
* Drawing lines in the game with the Drawing service

You need two vessels in orbit a few tens of meters apart, each with a docking port. The vessel
you fly also needs RCS thrusters and monopropellant. Before you run the script, right-click your
vessel's docking port and choose *Control from here*. Then right-click the other vessel's docking
port and choose *Set as target*.

The tutorial includes source code for the main client languages kRPC supports. Download the
whole program in your chosen language:

:download:`Python</scripts/tutorials/docking/Docking.py>`,
:download:`C#</scripts/tutorials/docking/Docking.cs>`,
:download:`C++</scripts/tutorials/docking/Docking.cpp>`,
:download:`C</scripts/tutorials/docking/Docking.c>`,
:download:`Java</scripts/tutorials/docking/Docking.java>`,
:download:`Lua</scripts/tutorials/docking/Docking.lua>`,
:download:`JavaScript</scripts/tutorials/docking/Docking.js>`

Finding the Ports
-----------------

First we connect to the server and find the two docking ports. Our port is the part the vessel is
controlled from, and the target port is the one you set as the target:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/docking/Docking.py
         :language: python
         :start-at: conn = krpc.connect(name="Docking")
         :end-before: # Turn to face the target port

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/docking/Docking.cs
         :language: csharp
         :start-at: var conn = new Connection ("Docking");
         :end-before: // Turn to face the target port

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/docking/Docking.cpp
         :language: cpp
         :start-at: krpc::Connection conn = krpc::connect("Docking");
         :end-before: // Turn to face the target port

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/docking/Docking.c
         :language: c
         :start-at: krpc_connection_t conn;
         :end-before: // Turn to face the target port

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/docking/Docking.java
         :language: java
         :start-at: Connection connection = Connection.newInstance("Docking");
         :end-before: // Turn to face the target port

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/docking/Docking.lua
         :language: lua
         :start-at: local conn = krpc.connect('Docking')
         :end-before: -- Turn to face the target port

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/docking/Docking.js
         :language: javascript
         :start-at: const conn = await connect({ name: "Docking" });
         :end-before: // Turn to face the target port

The script measures everything in the target port's reference frame. Its origin is at the target
port, and its y-axis points out of the port, along the path our port must take to dock. See the
:ref:`reference-frames` guide for more.

Turning to Face the Target
--------------------------

Our port has to point back along the target port's y-axis, which is the direction ``(0, -1, 0)``
in its frame. The autopilot turns the vessel to face that way, and ``wait`` blocks until it does.
The vessel is controlled from our port, so the autopilot points the port:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/docking/Docking.py
         :language: python
         :start-at: # Turn to face the target port
         :end-before: # Draw the docking axis, and a line from our port to the target

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/docking/Docking.cs
         :language: csharp
         :start-at: // Turn to face the target port
         :end-before: // Draw the docking axis, and a line from our port to the target

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/docking/Docking.cpp
         :language: cpp
         :start-at: // Turn to face the target port
         :end-before: // Draw the docking axis, and a line from our port to the target

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/docking/Docking.c
         :language: c
         :start-at: // Turn to face the target port
         :end-before: // Draw the docking axis, and a line from our port to the target

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/docking/Docking.java
         :language: java
         :start-at: // Turn to face the target port
         :end-before: // Draw the docking axis, and a line from our port to the target

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/docking/Docking.lua
         :language: lua
         :start-at: -- Turn to face the target port
         :end-before: -- Draw the docking axis, and a line from our port to the target

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/docking/Docking.js
         :language: javascript
         :start-at: // Turn to face the target port
         :end-before: // Draw the docking axis, and a line from our port to the target

This also switches off SAS, as the autopilot does the steering, and switches on RCS for the
approach.

Drawing the Approach
--------------------

The Drawing service draws lines in the game. A green line runs 50 m out along the target port's
axis, showing the path to dock along. A yellow line runs from our port to the target port, and
the script moves its start as our port moves:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/docking/Docking.py
         :language: python
         :start-at: # Draw the docking axis, and a line from our port to the target
         :end-before: # Approach the target port along its axis

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/docking/Docking.cs
         :language: csharp
         :start-at: // Draw the docking axis, and a line from our port to the target
         :end-before: // Approach the target port along its axis

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/docking/Docking.cpp
         :language: cpp
         :start-at: // Draw the docking axis, and a line from our port to the target
         :end-before: // Approach the target port along its axis

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/docking/Docking.c
         :language: c
         :start-at: // Draw the docking axis, and a line from our port to the target
         :end-before: // Approach the target port along its axis

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/docking/Docking.java
         :language: java
         :start-at: // Draw the docking axis, and a line from our port to the target
         :end-before: // Approach the target port along its axis

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/docking/Docking.lua
         :language: lua
         :start-at: -- Draw the docking axis, and a line from our port to the target
         :end-before: -- Approach the target port along its axis

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/docking/Docking.js
         :language: javascript
         :start-at: // Draw the docking axis, and a line from our port to the target
         :end-before: // Approach the target port along its axis

Approaching the Target
----------------------

Now we fly our port along the axis to the target port. Each pass of the loop picks a velocity to
fly at, and fires the RCS thrusters to reach it. The loop ends when the magnets of the two ports
catch each other, and our port's state changes to docking:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/docking/Docking.py
         :language: python
         :start-at: # Approach the target port along its axis
         :end-before: # Stop the thrusters, and remove the drawing

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/docking/Docking.cs
         :language: csharp
         :start-at: // Approach the target port along its axis
         :end-before: // Stop the thrusters, and remove the drawing

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/docking/Docking.cpp
         :language: cpp
         :start-at: // Approach the target port along its axis
         :end-before: // Stop the thrusters, and remove the drawing

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/docking/Docking.c
         :language: c
         :start-at: // Approach the target port along its axis
         :end-before: // Stop the thrusters, and remove the drawing

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/docking/Docking.java
         :language: java
         :start-at: // Approach the target port along its axis
         :end-before: // Stop the thrusters, and remove the drawing

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/docking/Docking.lua
         :language: lua
         :start-at: -- Approach the target port along its axis
         :end-before: -- Stop the thrusters, and remove the drawing

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/docking/Docking.js
         :language: javascript
         :start-at: // Approach the target port along its axis
         :end-before: // Stop the thrusters, and remove the drawing

The script picks a goal on the axis to fly towards. While our port is more than 0.5 m off the
axis, the goal is level with it, at least 5 m out from the target. So the vessel first moves
sideways onto the axis. Once it is on the axis, the goal is the target port itself.

The speed towards the goal is a fifth of the distance to it, from 0.1 m/s to 1 m/s. So the vessel
slows down as it closes in, and touches the target port gently.

The thrust is the difference between that velocity and the vessel's velocity, doubled. The RCS
controls take a direction in the vessel's reference frame: right along its x-axis, forward along
its y-axis, and up opposite its z-axis. So :meth:`SpaceCenter.transform_direction` converts the
thrust from the target port's frame to the vessel's frame.

The Python, C#, C++, Java and JavaScript scripts read the position and velocity from streams. The
C and Lua clients do not support streams, so their scripts call for them on every pass.

Finishing
---------

Once the ports have docked, we stop the thrusters, switch off the autopilot and remove the
drawing:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/docking/Docking.py
         :language: python
         :start-at: # Stop the thrusters, and remove the drawing

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/docking/Docking.cs
         :language: csharp
         :start-at: // Stop the thrusters, and remove the drawing

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/docking/Docking.cpp
         :language: cpp
         :start-at: // Stop the thrusters, and remove the drawing

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/docking/Docking.c
         :language: c
         :start-at: // Stop the thrusters, and remove the drawing

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/docking/Docking.java
         :language: java
         :start-at: // Stop the thrusters, and remove the drawing

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/docking/Docking.lua
         :language: lua
         :start-at: -- Stop the thrusters, and remove the drawing

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/docking/Docking.js
         :language: javascript
         :start-at: // Stop the thrusters, and remove the drawing

The two vessels are now docked together.
