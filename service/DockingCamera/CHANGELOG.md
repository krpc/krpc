## [v0.7.0] - unreleased

- A `Camera` whose part the game has destroyed, or that is no longer a camera, raises
  `KRPC.ObjectDestroyedException` rather than reaching into a part that is gone, and is
  freed rather than kept for the rest of the session (#1051)

## [v0.6.0]
- Fix `DockingCamera.Available` incorrectly reporting false in game scenes other than flight (#937)

## [v0.5.0]
- Initial version
