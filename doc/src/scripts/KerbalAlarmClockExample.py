import krpc
conn = krpc.connect(name='Kerbal Alarm Clock Example')

alarm = conn.kerbal_alarm_clock.create_alarm(
    conn.kerbal_alarm_clock.AlarmType.raw,
    'My New Alarm',
    conn.space_center.ut+10)

alarm.notes = '10 seconds have now passed since the alarm was created.'
alarm.action = conn.kerbal_alarm_clock.AlarmAction.message_only
