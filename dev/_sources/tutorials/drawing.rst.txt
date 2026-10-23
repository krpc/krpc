Drawing
=======

The following script draws three objects for a vessel in orbit around Kerbin. A
dashed green path shows a circular target orbit 250 km up. A flag marks the
north pole in the map view, and a yellow arrow points prograde from the vessel.
The script updates the label on the arrow with the vessel's orbital speed.

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/drawing/DrawingExample.lua
         :language: lua

The arrow is drawn in the flight scene. Switch to the map view to see the target
orbit and the flag. Drawn objects remain until the script disconnects, which is
why the script ends in a loop.

.. seealso::

   :class:`Drawing` for the other objects you can draw, including circles, arcs,
   boxes, spheres, points and navball markers.
