import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
abort = conn.add_stream(getattr, vessel.control, 'abort')


def check_abort1(x):
    print 'Abort 1 called with a value of', x


def check_abort2(x):
    print 'Abort 2 called with a value of', x

abort.add_callback(check_abort1)
abort.add_callback(check_abort2)
abort.start()

# Keep the program running...
while True:
    pass
