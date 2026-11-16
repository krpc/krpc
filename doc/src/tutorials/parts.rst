.. currentmodule:: SpaceCenter

Interacting with Parts
======================

The following examples demonstrate use of the :ref:`python-api-parts` functionality to
achieve various tasks. The :ref:`parts guide <user-guide-parts>` explains the tree of
parts, attachment modes, fuel lines, staging and part modules.

Deploying all Parachutes
------------------------

Sometimes things go horribly wrong. The following script does its best to save
your Kerbals by deploying all the parachutes:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.lua
         :language: lua

   .. tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.js
         :language: javascript

The script deploys the parachutes one call at a time, and the game can advance between the
calls. A :ref:`server side function <server-side-functions>` deploys them all in the same
physics tick, with a single call:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutesFunction.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutesFunction.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

'Control From Here' for Docking Ports
-------------------------------------

The following example will find a standard sized Clamp-O-Tron docking port, and
control the vessel from it:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.lua
         :language: lua

   .. tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.js
         :language: javascript

Combined Specific Impulse
-------------------------

The following script calculates the combined specific impulse of all currently
active and fueled engines on a rocket. See here for a description of the maths:
https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.lua
         :language: lua

   .. tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.js
         :language: javascript

The script reads the thrust and specific impulse of each engine with separate calls, which can
land in different physics ticks. A :ref:`server side function <server-side-functions>`
reads them all in the same tick, with a single call:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/CombinedIspFunction.py
         :language: python
         :start-after: [docs]
         :end-before: [/docs]

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/parts/CombinedIspFunction.cs
         :language: csharp
         :start-after: [docs]
         :end-before: [/docs]
         :dedent:

   .. tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/parts/CombinedIspFunction.js
         :language: javascript
         :start-after: [docs]
         :end-before: [/docs]

.. seealso:: The combined specific impulse of each stage is also available
   directly from the staging API, as :attr:`Stage.specific_impulse` on the
   object returned by :meth:`Vessel.stage_at`.
