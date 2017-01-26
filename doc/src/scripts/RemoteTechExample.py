import krpc
conn = krpc.connect(name='RemoteTech Example')
vessel = conn.space_center.active_vessel

# Set a dish target
part = vessel.parts.with_title('Reflectron KR-7')[0]
antenna = conn.remote_tech.antenna(part)
antenna.target_body = conn.space_center.bodies['Jool']

# Get info about the vessels communications
comms = conn.remote_tech.comms(vessel)
print 'Signal delay =', comms.signal_delay
