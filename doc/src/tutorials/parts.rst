Interacting with Parts
======================

The following examples demonstrate use of the :ref:`api-parts` functionality to
achieve various tasks. More details on specific topics can also be found in the
API documentation:

* :ref:`api-parts-trees-of-parts`
* :ref:`api-parts-attachment-modes`
* :ref:`api-parts-fuel-lines`
* :ref:`api-parts-staging`

Deploying all Parachutes
------------------------

Sometimes things go horribly wrong. The following script does its best to save
your Kerbals by deploying all the parachutes:

.. code-block:: python

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   for parachute in vessel.parts.parachutes:
       parachute.deploy()

'Control From Here' for Docking Ports
-------------------------------------

The following example will find a standard sized Clamp-O-Tron docking port, and
control the vessel from it:

.. code-block:: python

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   ports = vessel.parts.docking_ports:
   port = list(filter(lambda p: p.part.title == 'Clamp-O-Tron Docking Port', ports))[0]
   vessel.parts.controlling = port

Combined Specific Impulse
-------------------------

The following script calculates the combined specific impulse of all currently
active and fueled engines on a rocket. See here for a description of the maths:
http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines

.. code-block:: python

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   active_engines = filter(lambda e: e.active and e.has_fuel, vessel.parts.engines)

   print('Active engines:')
   for engine in active_engines:
       print('   ', engine.part.title, 'in stage', engine.part.stage)

   thrust = sum(engine.thrust for engine in active_engines)
   fuel_consumption = sum(engine.thrust / engine.specific_impulse for engine in active_engines)
   isp = thrust / fuel_consumption

   print('Combined vaccuum Isp = %d seconds' % isp)
