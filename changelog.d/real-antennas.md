## service/SpaceCenter

- Support RealAntennas
  - Add its link budget to `Antenna`: gain, transmit power, tech level, band, data rate,
    beamwidth, power draw and enabled (#1320)
  - Add antenna targeting to `Antenna` (#1320)
  - Add data rates and antennas to `CommLink` (#1320)
  - List each antenna of a part with several in `Parts.Antennas` (#1320)
  - Add `SpaceCenter.RealAntennasAvailable` (#1320)
