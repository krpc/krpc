## core

- Support flags enumerations, a `KRPCEnum` with `[Flags]` whose values a client can combine (#1311)
- Add `flags` to the `Enumeration` message, set for a flags enumeration (#1311)
- Combine flags enumeration values in server side functions with `KRPC.Expression.And`, `Or` and
  `ExclusiveOr` (#1311)

## service/SpaceCenter

- **Breaking:** Make `MapFilterType` a flags enumeration, and change `MapFilterType.All` from -1
  to 32767 (#1311)

## client/csharp

- Generate a flags enumeration with `[Flags]` (#1311)

## client/cpp

- Generate the operators `|`, `&`, `^` and `~` for a flags enumeration (#1311)

## client/java

- **Breaking:** Generate a flags enumeration, such as `SpaceCenter.MapFilterType`, as a class of
  constants with `or`, `and`, `xor`, `andNot` and `contains` (#1311)

## client/lua

- Add `union`, `intersection`, `difference` and `contains` to the values of a flags enumeration (#1311)

## client/python

- Support flags enumerations, such as `SpaceCenter.MapFilterType`, as an `enum.Flag` (#1311)
- Raise `EncodingError` for an enumeration result the enumeration does not declare, in place of
  `ValueError` (#1311)

## tools/krpctools

- Support flags enumerations in `clientgen` and `docgen` (#1311)
