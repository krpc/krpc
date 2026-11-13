## client/python

- **Breaking:** Convert names with `UInt`, `ULong`, `SInt`, `FoV`, a plural acronym or digits
  next to an acronym as written, e.g. `Type.uint`, `Camera.fov` and `MapFilterType.evas` in place of
  `u_int`, `fo_v` and `ev_as` (#1339)

## client/lua

- **Breaking:** Convert names with `UInt`, `ULong`, `SInt`, `FoV`, a plural acronym or digits
  next to an acronym as written, e.g. `Type.uint`, `Camera.fov` and `MapFilterType.evas` in place of
  `u_int`, `fo_v` and `ev_as` (#1339)

## client/cpp

- **Breaking:** Convert names with `UInt`, `ULong`, `SInt`, `FoV`, a plural acronym or digits
  next to an acronym as written, e.g. `Type::uint`, `Camera::fov` and `MapFilterType::evas` in
  place of `u_int`, `fo_v` and `ev_as` (#1339)

## client/java

- **Breaking:** Name enumeration constants in upper snake case, e.g. `AltimeterMode.AGL`,
  `AlarmType.SOI_CHANGE` and `TypeCode.UINT32` in place of `A_GL`, `S_OI_CHANGE` and `U_INT32`
  (#1339)
- **Breaking:** Lower case a leading acronym in method and field names, e.g.
  `CelestialBody.mslPosition` and `Type.uint` in place of `mSLPosition` and `uInt` (#1339)
