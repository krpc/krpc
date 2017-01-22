.. currentmodule:: SpaceCenter

Interacting with Parts
======================

The following examples demonstrate use of the :ref:`python-api-parts` functionality to
achieve various tasks. More details on specific topics can also be found in the
API documentation:

* :ref:`python-api-parts-trees-of-parts`
* :ref:`python-api-parts-attachment-modes`
* :ref:`python-api-parts-fuel-lines`
* :ref:`python-api-parts-staging`

Deploying all Parachutes
------------------------

Sometimes things go horribly wrong. The following script does its best to save
your Kerbals by deploying all the parachutes:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/DeployParachutes.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/DeployParachutes.cpp
         :language: cpp

   .. tab:: Lua

      .. literalinclude:: /scripts/DeployParachutes.lua
         :language: lua

   .. tab:: Java

      .. literalinclude:: /scripts/DeployParachutes.java
         :language: java

   .. tab:: Python

      .. literalinclude:: /scripts/DeployParachutes.py
         :language: python

'Control From Here' for Docking Ports
-------------------------------------

The following example will find a standard sized Clamp-O-Tron docking port, and
control the vessel from it:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/ControlFromHere.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/ControlFromHere.cpp
         :language: cpp

   .. tab:: Lua

      .. literalinclude:: /scripts/ControlFromHere.lua
         :language: lua

   .. tab:: Java

      .. literalinclude:: /scripts/ControlFromHere.java
         :language: java

   .. tab:: Python

      .. literalinclude:: /scripts/ControlFromHere.py
         :language: python

Combined Specific Impulse
-------------------------

The following script calculates the combined specific impulse of all currently
active and fueled engines on a rocket. See here for a description of the maths:
http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/CombinedIsp.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/CombinedIsp.cpp
         :language: cpp

   .. tab:: Lua

      .. literalinclude:: /scripts/CombinedIsp.lua
         :language: lua

   .. tab:: Java

      .. literalinclude:: /scripts/CombinedIsp.java
         :language: java

   .. tab:: Python

      .. literalinclude:: /scripts/CombinedIsp.py
         :language: python
