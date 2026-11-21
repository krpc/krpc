.. _user-guide-mods:

Mods
====

kRPC supports many popular mods. None of them needs to be installed. Some services and members
require one, and the API reference notes where they do. A service's ``Available`` property gives
whether its mod is installed, and SpaceCenter has one per mod, such as ``SpaceCenter.FARAvailable``.

SpaceCenter
-----------

The SpaceCenter service supports these mods:

* `Ferram Aerospace Research`_: ``Flight`` works with the stock aerodynamics or with Ferram
  Aerospace Research, and reads from whichever is in use. Some members require it, such as
  ``Flight.LiftCoefficient``, and ``Part.Lift`` and ``Part.Drag`` are not available with it.
* `Extended Action Groups`_ adds action groups 10 to 250 to ``Control``.
* `RealFuels`_ adds ignitions, ullage and feed pressure to ``Engine``, and tank pressure and
  boil-off to ``Part``.
* ``Parachute`` objects support `RealChute`_.
* ``Fairing`` objects support `ProceduralFairings`_.
* ``SolarPanel`` objects support the curved solar panels of `Near Future Solar`_.
* ``Experiment`` objects support DMagic's `DMModuleScienceAnimateGeneric`_.
* ``Engine.ThrustReversed`` supports the thrust reversers of `Firespitter`_ and
  `KerbalActuators`_.
* `KSPCommunityPartModules`_ 0.5 or later, or `kOS`_, provides the name tag in ``Part.Tag``.
* `B9PartSwitch`_ switches a part's subtype when its field is set through ``PartField``.
* `TweakScale Rescaled`_ parts report their scaled masses, resources, thrust, torque and cost.
* `RealAntennas`_ adds the link budget and targeting to ``Antenna``, and data rates to
  ``CommLink``.
* The :doc:`RemoteTech service </python/api/remote-tech>` covers RemoteTech antennas.

kRPC is tested with these versions:

.. include:: /space-center-mods.inc

Mod Services
------------

These services cover other mods:

.. include:: /mod-services.inc

The MechJeb service supports MechJeb's releases. For MechJeb's development builds,
`krpc-mechjeb-dev <https://krpc.github.io/krpc-mechjeb-dev/>`_ is a best-effort replacement of
it, released on its own.

Other Tested Mods
-----------------

kRPC is also tested with these mods, which change the solar system and the parts that the
SpaceCenter service reads:

.. include:: /tested-mods.inc

.. include:: /mods.inc
