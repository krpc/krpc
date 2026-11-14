User Guides
===========

These guides describe how kRPC and the game behave, independent of the client language.

The Game
--------

How a vessel is built, how the game is organized into scenes and saves, and how time passes.

.. toctree::
   :maxdepth: 1

   user-guide/parts
   user-guide/game-scenes-and-saves
   user-guide/time-warp

Positions and Directions
------------------------

How kRPC represents positions, directions and rotations, and the reference frames they are given
in.

.. toctree::
   :maxdepth: 1

   user-guide/vectors-and-rotations
   user-guide/reference-frames

Flying
------

How to steer a vessel with the auto-pilot, and how to write a control loop that acts on every
physics tick.

.. toctree::
   :maxdepth: 1

   user-guide/autopilot
   user-guide/control-loops

The Server
----------

How the server runs your calls: streams and events, the lifetime of the objects it returns,
functions that run inside the game, and the settings that trade call throughput against the game's
frame rate.

.. toctree::
   :maxdepth: 1

   user-guide/streams-and-events
   user-guide/object-lifetime
   user-guide/server-side-functions
   user-guide/server-performance
