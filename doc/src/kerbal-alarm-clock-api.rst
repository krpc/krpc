Kerbal Alarm Clock API
======================

Provides RPCs to interact with the `Kerbal Alarm Clock`_ mod. Provides the
following classes:

.. toctree::
   :includehidden:
   :maxdepth: 2

   python-api/kerbal-alarm-clock/kerbal-alarm-clock
   python-api/kerbal-alarm-clock/alarm
   python-api/kerbal-alarm-clock/alarm-type
   python-api/kerbal-alarm-clock/alarm-action

Example
-------

The following example creates a new alarm for the active vessel. The alarm is
set to trigger after 10 seconds have passed, and display a message.

.. code-block:: python

   import krpc
   conn = krpc.connect(name='Kerbal Alarm Clock Example')

   alarm = conn.kerbal_alarm_clock.create_alarm(
       conn.kerbal_alarm_clock.AlarmType.raw,
       'My New Alarm',
       conn.space_center.ut+10)

   alarm.notes = '10 seconds have now passed since the alarm was created.'
   alarm.action = conn.kerbal_alarm_clock.AlarmAction.message_only

.. _Kerbal Alarm Clock: http://forum.kerbalspaceprogram.com/threads/24786
