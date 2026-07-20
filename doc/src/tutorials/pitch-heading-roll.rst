.. currentmodule:: SpaceCenter

Pitch, Heading and Roll
=======================

This example calculates the pitch, heading and rolls angles of the active vessel
once per second.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/pitch-heading-roll/PitchHeadingRoll.py
         :language: python

.. note:: Pitch, heading and roll are Euler angles, and are ill-defined when the vessel
   points near vertical (pitch approaching ±90°), where heading and roll become
   ambiguous. This applies both to the computation in this example (which prints
   0, 0, 0 for a vessel pointing straight up, for example on the launchpad) and to the
   :attr:`Flight.pitch`, :attr:`Flight.heading` and :attr:`Flight.roll` telemetry. For a
   representation of the vessel's attitude that is always well-defined, use
   :attr:`Flight.rotation` or :attr:`Flight.direction`, or the auto-pilot's error
   readouts described in the :doc:`autopilot tutorial </tutorials/autopilot>`.
