local krpc = require 'krpc'

local conn = krpc.connect('SCANsat Example')
local vessel = conn.space_center.active_vessel
local scansat = conn.scan_sat

for _, scanner in ipairs(scansat.scanners(vessel)) do
  scanner.active = true
end

local body = vessel.orbit.body
local coverage = scansat.coverage(body, scansat.ScanType.altimetry)
print(string.format('Altimetry coverage of %s: %.1f%%', body.name, coverage * 100))

local flight = vessel:flight()
local elevation = scansat.elevation(body, flight.latitude, flight.longitude)
if elevation == nil then
  print('The terrain below the vessel is not scanned')
else
  print(string.format('The terrain below the vessel is at %.0f m', elevation))
end
