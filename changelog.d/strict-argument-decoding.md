## service/SpaceCenter

- **Breaking:** Reject a combination of `MapFilterType` values in `SpaceCenter.MapFilter` (#1280)

## core

- **Breaking:** Reject an argument with bytes left over or an unknown field (#1280)
- **Breaking:** Reject a 32-bit number out of range, a tuple with the wrong number of items, an
  undeclared enum value, or a bool other than 0 or 1 (#1280)
- Fail only the call whose argument the server cannot decode, and run the other calls in the
  request (#1280)
