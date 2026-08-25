## [v0.7.0] - unreleased

- An `Antenna` whose part is destroyed, or that is no longer an antenna, and a `Comms` whose
  vessel is gone, raise `KRPC.ObjectDestroyedException` (#1051)

## [v0.6.0]
- Fix `RemoteTech.Available` incorrectly reporting false in game scenes other than flight (#937)

## [v0.3.7]
- Add `RemoteTech.Available`
- Change required RemoteTech version to 1.8.0

## [v0.3.3]
- Moved from `SpaceCenter` to separate service
- Support RemoteTech 1.7
- Add individual `Antenna` objects
- Add support for getting and setting an antennas target
