#!/usr/bin/env python2

import krpc
import time

client = krpc.Client()
control = krpc.Control(client)
orbit = krpc.Orbit(client)

control.Set(throttle=1.0, sas=True)
time.sleep(1.0)
control.ActivateNextStage()
time.sleep(30)
control.ActivateNextStage()

time.sleep(180-30)
control.ActivateNextStage()

while True:
    orbitData = orbit.Get()
    print '%.0f x %.0f' % (orbitData.apoapsis, orbitData.periapsis)
    time.sleep(0.5)
