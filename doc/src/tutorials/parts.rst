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

   .. tab:: C

      * :ref:`cnano-api-parts-trees-of-parts`
      * :ref:`cnano-api-parts-attachment-modes`
      * :ref:`cnano-api-parts-fuel-lines`
      * :ref:`cnano-api-parts-staging`

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

.. note:: To interact with the fields, events and actions of a part module, use
   :attr:`Module.field_list`, :attr:`Module.event_list` and
   :attr:`Module.action_list`. These return :class:`PartField`,
   :class:`PartEvent` and :class:`PartAction` objects that can be read, set and
   triggered. They replace the module's string-dictionary members (such as
   ``Module.fields``, ``Module.get_field`` and ``Module.set_action``) that older
   scripts and forum posts use, which are deprecated.

Deploying all Parachutes
------------------------

Sometimes things go horribly wrong. The following script does its best to save
your Kerbals by deploying all the parachutes:

.. tabs::

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

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/DeployParachutes.py
         :language: python

The script deploys the parachutes one call at a time, and the game can advance between the
calls. In Python, a :ref:`server side function <tutorial-server-side-functions>` deploys them all
in the same physics tick, with a single call:

.. code-block:: python

   def deploy_parachutes():
       for parachute in vessel.parts.parachutes:
           parachute.deploy()

   conn.run(deploy_parachutes)

'Control From Here' for Docking Ports
-------------------------------------

The following example will find a standard sized Clamp-O-Tron docking port, and
control the vessel from it:

.. tabs::

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

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/ControlFromHere.py
         :language: python

Combined Specific Impulse
-------------------------

The following script calculates the combined specific impulse of all currently
active and fueled engines on a rocket. See here for a description of the maths:
https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines

.. tabs::

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

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/parts/CombinedIsp.py
         :language: python

The script reads the thrust and specific impulse of each engine with separate calls, which can
land in different physics ticks. A :ref:`server side function <tutorial-server-side-functions>`
reads them all in the same tick, with a single call:

.. tabs::

   .. tab:: C#

      .. code-block:: csharp

         var isp = connection.Run (() =>
             vessel.Parts.Engines.Where (e => e.Active && e.HasFuel)
                 .Select (e => e.Thrust).Sum () /
             vessel.Parts.Engines.Where (e => e.Active && e.HasFuel)
                 .Select (e => e.Thrust / e.SpecificImpulse).Sum ());

   .. tab:: Python

      .. code-block:: python

         def combined_isp():
             engines = [e for e in vessel.parts.engines if e.active and e.has_fuel]
             thrust = sum(engine.thrust for engine in engines)
             fuel_consumption = sum(
                 engine.thrust / engine.specific_impulse for engine in engines
             )
             return thrust / fuel_consumption

         isp = conn.run(combined_isp)

.. seealso:: The combined specific impulse of each stage is also available
   directly from the staging API, as :attr:`Stage.specific_impulse` on the
   object returned by :meth:`Vessel.stage_at`.
