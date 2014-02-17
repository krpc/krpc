#!/usr/bin/env python2

import krpc
import time

client = krpc.connect(name='Example script');

control = krpc.Control(client)
orbit = krpc.Orbit(client)

client.Control.Set(throttle=1.0, sas=True)
time.sleep(1.0)
client.Control.ActivateNextStage()
time.sleep(30)
client.Control.ActivateNextStage()

time.sleep(180-30)
client.Control.ActivateNextStage()

while True:
    orbitData = client.Orbit.Get()
    print '%.0f x %.0f' % (orbitData.apoapsis, orbitData.periapsis)
    time.sleep(0.5)
