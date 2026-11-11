import krpc

conn = krpc.connect(name="SCANsat Example")
vessel = conn.space_center.active_vessel
scansat = conn.scan_sat

for scanner in scansat.scanners(vessel):
    scanner.active = True

body = vessel.orbit.body
coverage = scansat.coverage(body, scansat.ScanType.altimetry)
print("Altimetry coverage of %s: %.1f%%" % (body.name, coverage * 100))

flight = vessel.flight()
elevation = scansat.elevation(body, flight.latitude, flight.longitude)
if elevation is None:
    print("The terrain below the vessel is not scanned")
else:
    print("The terrain below the vessel is at %.0f m" % elevation)
