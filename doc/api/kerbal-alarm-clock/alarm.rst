Alarm
=====

.. class:: Alarm

   Represents an alarm. Obtained by calling :attr:`KerbalAlarmClock.Alarms`,
   :meth:`KerbalAlarmClock.AlarmWithName` or
   :meth:`KerbalAlarmClock.AlarmsWithType`.

   .. attribute:: Action

      Gets or sets the action that the alarm triggers.

      :rtype: :class:`AlarmAction`

   .. attribute:: Margin

      Gets or sets the number of seconds before the event that the alarm will
      fire.

      :rtype: double

   .. attribute:: Time

      Gets or sets the time at which the alarm will fire.

      :rtype: double

   .. attribute:: Type

      Gets or sets the type of the alarm.

      :rtype: :class:`AlarmType`

   .. attribute:: ID

      Gets the unique identifier for the alarm.

      :rtype: string

   .. attribute:: Name

      Gets or sets the short name of the alarm.

      :rtype: string

   .. attribute:: Notes

      Gets or sets the long description of the alarm.

      :rtype: string

   .. attribute:: Remaining

      Gets the number of seconds until the alarm will fire.

      :rtype: double

   .. attribute:: Repeat

      Gets or sets whether the alarm should be repeated after it has fired.

      :rtype: bool

   .. attribute:: RepeatPeriod

      Gets or sets the time delay to automatically create an alarm after it has
      fired.

      :rtype: double

   .. attribute:: Vessel

      Gets or sets the vessel that the alarm is attached to.

      :rtype: :class:`Vessel`

   .. attribute:: XferOriginBody

      Gets or sets the celestial body the vessel is departing from.

      :rtype: :class:`CelestialBody`

   .. attribute:: XferTargetBody

      Gets or sets the celestial body the vessel is arriving at.

      :rtype: :class:`CelestialBody`

   .. method:: Delete ()

      Deletes the alarm.
