import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def deploy():
    for parachute in vessel.parts.parachutes:
        if not parachute.deployed:
            parachute.deploy()


conn.run(deploy)
# [/docs]
