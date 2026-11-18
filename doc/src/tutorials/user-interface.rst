User Interface
==============

The following script demonstrates how to use the UI service to display text and
handle basic user input. It adds a panel to the left side of the screen, which
can be dragged with the mouse. A vertical layout arranges its contents in a
column: a button that sets the throttle to maximum, a slider that sets it
directly, and text showing the current thrust produced by the vessel. A tooltip
is shown while the mouse rests on the button.

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.py
         :language: python

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.cs
         :language: csharp

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.cpp
         :language: cpp

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.c
         :language: c

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.java
         :language: java

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.lua
         :language: lua

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/user-interface/UserInterface.js
         :language: javascript

.. seealso::

   :class:`UI` for the other elements you can add, including text input fields,
   toggles, drop-down lists, scroll views and images.
