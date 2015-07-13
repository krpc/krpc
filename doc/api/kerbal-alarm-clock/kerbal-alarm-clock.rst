.. default-domain:: #echo $domain

KerbalAlarmClock
================

.. class:: KerbalAlarmClock

   This service provides functionality to interact with the
   `Kerbal Alarm Clock`_ mod.

   .. attribute:: Alarms

      Gets a list of all the alarms.

      :rtype: :class:`List` ( :class:`Alarm` )

   .. method:: AlarmWithName (name)

      Get the alarm with the given *name*, or ``null`` if no alarms have that
      name. If more than one alarm has the name, only returns one of them.

      :param string name: Name of the alarm to search for.

   .. method:: AlarmsWithType (type)

      Returns a list of alarms of the specified *type*.

      :param AlarmType type: Type of alarm to return.
      :rtype: :class:`List` ( :class:`Alarm` )

   .. method:: CreateAlarm (type, name, ut)

      Create an alarm and return it.

      :param AlarmType type: Type of the new alarm.
      :param string name: Name of the new alarm.
      :param double ut: Time of the new alarm.
      :rtype: :class:`Alarm`

.. _Kerbal Alarm Clock: http://forum.kerbalspaceprogram.com/threads/24786
