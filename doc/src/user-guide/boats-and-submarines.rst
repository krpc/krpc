.. currentmodule:: SpaceCenter

Boats and Submarines
====================

A hull floats when the water it pushes aside weighs more than the hull does.
:attr:`Flight.displacement` is the volume of water the vessel pushes aside when it is
fully submerged, and :meth:`Flight.buoyant_force_at` is the force that volume pushes back
with:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantForce.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

Compare that force with the vessel's weight. A hull with more buoyancy than its weight
floats. A submarine trims its tanks to move between the two.
:attr:`Part.displacement` and :meth:`Part.buoyant_force_at` give the same figures for one
part.

.. note::

   :meth:`Flight.buoyant_force_at` applies :attr:`SpaceCenter.buoyancy_scalar`, which is 1.2
   in stock KSP. A buoyant force in the game is a fifth larger than
   `Archimedes' principle <https://en.wikipedia.org/wiki/Archimedes%27_principle>`_ alone
   gives.

Trim
----

A hull at rest carries its center of buoyancy on the same vertical line as its center of
mass. A horizontal offset between the two turns the weight and the buoyancy into a couple
that rolls or pitches the hull. That offset is the trim.

Read both in the vessel's surface reference frame. Its x axis points up, so the trim is
what the other two axes carry:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/CenterOfBuoyancy.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:attr:`Flight.center_of_buoyancy` is null when no part of the vessel is in the water, or the body
has no ocean.

The couple with the hull fully submerged is also available as a torque about the center of mass.
It is zero when the fully submerged center of buoyancy lies directly above or below the center of
mass:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyantTorque.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:meth:`Flight.buoyant_torque_at` is the torque with the hull fully submerged. It holds
steady while the hull rides the waves, where :attr:`Flight.buoyant_torque` follows the
live waterline.

In the Editor
-------------

A hull can be trimmed before it is launched. :meth:`EditorVessel.flight` returns a
:class:`SimulatedFlight`, which has the fully submerged members of :class:`Flight`. Its
:attr:`SimulatedFlight.surface_reference_frame` sets which way is up. Here a hull built
nose-up in the vehicle assembly building is simulated floating on its bottom:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/EditorTorque.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

Gravity acts along the negative x axis of the surface reference frame. A frame created
relative to :attr:`EditorVessel.reference_frame` follows the hull as it is edited.

Submersion
----------

:attr:`Flight.submerged_portion` is the fraction of the vessel below the waterline, and
:attr:`Flight.depth` is how far its center of mass sits below the surface. The same pair
is available per part, alongside :attr:`Part.splashed` and the depth of the part's
shallowest and deepest points.

Pressure
--------

Below the waterline, :attr:`Flight.static_pressure` and :attr:`Part.static_pressure`
include the weight of the water. With part pressure limits enabled in the difficulty
settings, a part is destroyed when its static plus dynamic pressure exceeds
:attr:`Part.max_pressure`:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimits.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

The loop reads the parts with separate calls, which can land in different physics ticks. A
:ref:`server side function <server-side-functions>` reads every part in the same tick,
with a single call:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/PressureLimitsFunction.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

:meth:`CelestialBody.pressure_at` takes a negative altitude for a point under the sea, to
plan how deep a dive can go.

Example
-------

This script reports whether the active vessel floats, and how far its center of buoyancy
sits from its center of mass:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.py
         :language: python

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.cs
         :language: csharp

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.cpp
         :language: cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.java
         :language: java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.lua
         :language: lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/boats-and-submarines/BuoyancyTrim.js
         :language: javascript
