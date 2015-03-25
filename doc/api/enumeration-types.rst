Enumeration Types
=================

VesselType
----------

.. class:: VesselType

   .. data:: Ship

   .. data:: Station

   .. data:: Lander

   .. data:: Probe

   .. data:: Rover

   .. data:: Base

   .. data:: Debris

Example
^^^^^^^

.. code-block:: python

   # Check if the active vessel is a station:
   if ksp.space_center.active_vessel.type == krpc.space_center.VesselType.station:
       print 'It is a station'
   else:
       print 'It is NOT a station'

VesselSituation
---------------

.. class:: VesselSituation

   .. data:: Docked

   .. data:: Escaping

   .. data:: Flying

   .. data:: Landed

   .. data:: Orbiting

   .. data:: PreLaunch

   .. data:: Splashed

   .. data:: SubOrbital
