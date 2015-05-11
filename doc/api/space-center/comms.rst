Comms
=====

.. class:: Comms

   Used to interact with `RemoteTech`_. Created using a call to
   :attr:`Vessel.Comms`.

   .. note:: This class requires `RemoteTech`_ to be installed.

   .. attribute:: HasFlightComputer

      Gets whether the vessel has a `RemoteTech`_ flight computer on board.

      :rtype: bool

   .. attribute:: HasConnection

      Gets whether the vessel can receive commands from the KSC or a command
      station.

      :rtype: bool

   .. attribute:: HasConnectionToGroundStation

      Gets whether the vessel can transmit science data to a ground station.

      :rtype: bool

   .. attribute:: SignalDelay

      Gets the signal delay when sending commands to the vessel, in seconds.

      :rtype: double

   .. attribute:: SignalDelayToGroundStation

      Gets the signal delay between the vessel and the closest ground station,
      in seconds.

      :rtype: double

   .. method:: SignalDelayToVessel (other)

      Gets the signal delay between the current vessel and another vessel, in
      seconds.

      :param Vessel other:
      :rtype: double

.. _RemoteTech: http://forum.kerbalspaceprogram.com/threads/83305-0-90-0-RemoteTech-v1-6-3-2015-02-06
