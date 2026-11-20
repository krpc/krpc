import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def firing_counter():
    updates = 0

    @conn.compile
    def count():
        nonlocal updates
        if vessel.thrust > 0:
            updates += 1
        return updates

    return count


stream = conn.add_stream(firing_counter())
# [/docs]
