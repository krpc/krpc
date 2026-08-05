User Interface
==============

The following script demonstrates how to use the UI service to display text and
handle basic user input. It adds a panel to the left side of the screen, which
the user can drag around with the mouse. A vertical layout arranges the panel's
contents in a column, so nothing needs positioning by hand: a button that sets
the throttle to maximum, with a tooltip shown while the mouse rests on it, a
slider that sets the throttle directly, and text displaying the current thrust
produced by the vessel.

.. tabs::

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.c
         :language: c

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.cpp
         :language: cpp

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.py
         :language: python
