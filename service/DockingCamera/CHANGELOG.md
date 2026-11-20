## [v0.8.0]

- Stop logging each read of `Camera.Image` (#1235)
- **Breaking:** Support the public Docking Camera (KURS) 1.3.9 release, in place of an
  unreleased fork (#1232)
- **Breaking:** `Camera.Image` throws an exception if the camera is off or not started, in place
  of returning an empty array (#1232)
- Support the cameras Docking Camera (KURS) adds to docking ports (#1232)
- **Breaking:** `Camera.Image` is a method taking a `StdLib.ImageFormat` and a JPEG quality (#1317)
- Keep working when a Docking Camera update removes members, and fail only the RPCs that use them (#1341)

## [v0.7.0]

- A `Camera` whose part is destroyed, or that is no longer a camera, raises
  `KRPC.ObjectDestroyedException` (#1051)

## [v0.6.0]
- Fix `DockingCamera.Available` incorrectly reporting false in game scenes other than flight (#937)

## [v0.5.0]
- Initial version
