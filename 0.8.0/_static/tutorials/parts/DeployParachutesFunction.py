import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def deploy_parachutes():
    for parachute in vessel.parts.parachutes:
        parachute.deploy()


conn.run(deploy_parachutes)
# [/docs]
