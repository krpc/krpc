## core

- Support flags enumerations, a `KRPCEnum` with `[Flags]` whose values a client can combine (#1311)
- Add `flags` to the `Enumeration` message, set for a flags enumeration (#1311)
- Combine flags enumeration values in server side functions with `KRPC.Expression.And`, `Or` and
  `ExclusiveOr` (#1311)

## service/SpaceCenter

- **Breaking:** Make `MapFilterType` a flags enumeration, and change `MapFilterType.All` from -1
  to 32767 (#1311)
