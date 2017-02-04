import krpc
conn = krpc.connect()
status = conn.krpc.get_status()
print('Data in = %.1f KB/s' % (status.bytes_read_rate/1024.0))
print('Data out = %.1f KB/s' % (status.bytes_written_rate/1024.0))
