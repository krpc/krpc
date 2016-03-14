status = conn.krpc.get_status()
print('Data in =', (status.bytes_read_rate/1024.0), 'KB/s')
print('Data out =', (status.bytes_written_rate/1024.0), 'KB/s')
