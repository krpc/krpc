Enumeration Types
=================

VesselType
----------

.. class:: VesselType

.. data:: VesselType.Ship

.. data:: VesselType.Station

.. data:: VesselType.Lander

.. data:: VesselType.Probe

.. data:: VesselType.Rover

.. data:: VesselType.Base

.. data:: VesselType.Debris

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

.. data:: VesselSituation.Docked

.. data:: VesselSituation.Escaping

.. data:: VesselSituation.Flying

.. data:: VesselSituation.Landed

.. data:: VesselSituation.Orbiting

.. data:: VesselSituation.PreLaunch

.. data:: VesselSituation.Splashed

.. data:: VesselSituation.SubOrbital
