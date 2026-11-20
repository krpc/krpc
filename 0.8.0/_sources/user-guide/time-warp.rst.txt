.. _user-guide-time-warp:

Time Warp
=========

KSP speeds up time in two ways. *Rails* warp, up to 100,000 times normal, stops simulating the
vessel's physics and moves it along its orbit. *Physics* warp, up to 4 times normal, runs the
physics simulation faster, so the vessel still flies under its own power. This guide describes how
to use both from a script.

Warping to a Time
-----------------

``SpaceCenter.WarpTo`` warps until a universal time, then stops. This example warps to ten seconds
before the vessel reaches apoapsis:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/time-warp/WarpTo.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

The call returns once the time is reached. It uses rails warp when the game allows it, and physics
warp otherwise, such as in an atmosphere. It slows the warp as the time approaches, and arrives close
to it rather than exactly on it. Warp to a few seconds before an event you need to act on, such as
a burn.

Its two optional arguments cap the rate: ``maxRailsRate`` for rails warp, 100,000 by default, and
``maxPhysicsRate`` for physics warp, 2 by default.

``WarpTo`` takes many physics ticks, so it ends any :ref:`hold on the tick
<holding-a-tick>` that your client has. It raises an exception if the game scene
changes during the warp. A :ref:`server side function <server-side-functions>` cannot call it.

Warp Factors
------------

A warp factor is the position of the warp rate on the game's warp control, not the rate itself.
``SpaceCenter.RailsWarpFactor`` sets rails warp, from 0 to 7, and
``SpaceCenter.PhysicsWarpFactor`` sets physics warp, from 0 to 3. Zero stops warping. This
example prints the highest rails warp factor the game allows now, then sets rails warp to 3:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C#

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C++

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.cpp
         :language: cpp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: C

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.c
         :language: c
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Java

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.java
         :language: java
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.lua
         :language: lua
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/time-warp/WarpFactor.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

Setting one kind of warp stops the other. A factor above the highest the game allows is lowered to
it, with no exception. ``SpaceCenter.WarpMode`` gives the kind of warp in use, and
``SpaceCenter.WarpRate`` the rate, which is 1 when time passes normally. The game eases the rate
toward a new factor over about a second of game time, so ``WarpRate`` lags behind a change.

Limits on Rails Warp
--------------------

In flight, the game limits rails warp to keep the vessel safe.
``SpaceCenter.MaximumRailsWarpFactor`` gives the highest factor it allows now, and
``SpaceCenter.CanRailsWarpAt`` whether it allows a given factor.

* There is no rails warp in an atmosphere, or under high acceleration.
* A landed vessel cannot rails warp while it moves.
* Close to a body, each factor needs a minimum altitude. The game skips these limits when the
  vessel's periapsis is clear of the atmosphere and the terrain, as it does for the player.
* A vessel in flight cannot rails warp with its throttle open, unless it is landed. Setting
  ``RailsWarpFactor`` in flight closes the throttle first.

In the space center and the tracking station, every rails warp factor is allowed. Physics warp
works in flight only.

During rails warp, the vessel is *packed*: the game stops simulating its physics and moves it
along its orbit. ``Vessel.Packed`` gives whether a vessel is packed.
