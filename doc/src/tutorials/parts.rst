.. currentmodule:: SpaceCenter

Interacting with Parts
======================

The following examples demonstrate use of the :ref:`python-api-parts` functionality to
achieve various tasks. More details on specific topics can also be found in the
API documentation:

.. tabs::

   .. tab:: C#

      * :ref:`csharp-api-parts-trees-of-parts`
      * :ref:`csharp-api-parts-attachment-modes`
      * :ref:`csharp-api-parts-fuel-lines`
      * :ref:`csharp-api-parts-staging`

   .. tab:: C++

      * :ref:`cpp-api-parts-trees-of-parts`
      * :ref:`cpp-api-parts-attachment-modes`
      * :ref:`cpp-api-parts-fuel-lines`
      * :ref:`cpp-api-parts-staging`

   .. tab:: Java

      * :ref:`java-api-parts-trees-of-parts`
      * :ref:`java-api-parts-attachment-modes`
      * :ref:`java-api-parts-fuel-lines`
      * :ref:`java-api-parts-staging`

   .. tab:: Lua

      * :ref:`lua-api-parts-trees-of-parts`
      * :ref:`lua-api-parts-attachment-modes`
      * :ref:`lua-api-parts-fuel-lines`
      * :ref:`lua-api-parts-staging`

   .. tab:: Python

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

   .. tab:: Java

      .. literalinclude:: /scripts/DeployParachutes.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/DeployParachutes.lua
         :language: lua

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

   .. tab:: Java

      .. literalinclude:: /scripts/ControlFromHere.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/ControlFromHere.lua
         :language: lua

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

   .. tab:: Java

      .. literalinclude:: /scripts/CombinedIsp.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/CombinedIsp.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/CombinedIsp.py
         :language: python
