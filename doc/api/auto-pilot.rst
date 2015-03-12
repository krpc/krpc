AutoPilot
=========

.. class:: AutoPilot

   Provides basic auto-piloting utilities for a vessel. Created by calling
   :meth:`Vessel.AutoPilot`.

.. method:: AutoPilot.SetRotation (pitch, heading, roll = NaN, reference_frame = Vessel.OrbitalReferenceFrame)

   Points the vessel in the specified direction, and holds it there. Setting the
   roll angle is optional. This method returns immediately, and the auto-pilot
   continues to set the rotation of the vessel, until
   :meth:`AutoPilot.Disengage` is called.

   :param double pitch: The desired pitch above/below the horizon, in degrees. A
                        value between -90° and +90° degrees.
   :param double heading: The desired heading in degrees. A valud between 0° and
                          360°.
   :param double roll: Optional desired roll angle relative to the horizon, in
                       degrees. A value between -180° and +180°.
   :param ReferenceFrame reference_frame: The reference frame that the pitch,
                                          heading and roll are in. Defaults to
                                          the vessels orbital reference frame.

.. method:: AutoPilot.SetDirection (direction, roll = NaN, reference_frame = Vessel.OrbitalReferenceFrame)

   Points the vessel along the specified direction vector, and holds it
   there. Setting the roll angle is optional. This method returns immediately,
   and the auto-pilot continues to set the rotation of the vessel, until
   :meth:`AutoPilot.Disengage` is called.

   :param Vector3 direction: The desired direction (pitch and heading) as a unit
                            vector.
   :param double roll: Optional desired roll angle relative to the horizon, in
                       degrees. A value between -180° and 180°.
   :param ReferenceFrame reference_frame: The reference frame that the direction
                                          vector is in. Defaults to the vessels
                                          orbital reference frame.

.. method:: AutoPilot.Error

   Gets the error, in degrees, between the direction the ship has been asked to
   point in and the actual direction it is pointing in. If the auto-pilot has
   not been engaged, returns NaN.

   :rtype: `double`

.. method:: AutoPilot.Disengage ()

   Disengage the auto-pilot.  Has no effect unless :meth:`AutoPilot.SetRotation`
   or :meth:`AutoPilot.SetDirection` have been called previously.
