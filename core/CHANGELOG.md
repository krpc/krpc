## [v0.7.0] - unreleased
- Encode and decode values without reflection, cutting the server cost of a call that passes
  or returns an object by about three quarters (#1091)
- **Breaking:** Nullability is carried by `Type.nullable` at every position a value sits in,
  replacing `Parameter.nullable` and `Procedure.return_is_nullable` (#1091)
- A structure field can be nullable, declared by `Nullable` on its `KRPCProperty` attribute
  or by a `Nullable<T>` field type (#1091)
- A list element, a tuple item and a dictionary value can be nullable, declared by a
  `Nullable<T>` type or by a path of positions on `KRPCNullable`, such as
  `[KRPCNullable (Position.Element)]` (#1091)
- Add extension members: a service can add methods and properties to a class belonging to
  another service, declared as C# extension methods annotated with `KRPCMethod` or
  `KRPCProperty` (#1077)
- Add structure types: a compound value with named fields, declared with `KRPCStruct` on a C#
  `struct` and sent to the client in full (#1066)
- Add a communication protocol carrying protocol buffer messages over a unix domain socket,
  for clients running on the same machine as the game (#1065)
- Add `KRPC.HoldTick` and `KRPC.ReleaseTick`, which hold the game on its current physics tick
  so a control loop can read, compute and write within one tick (#1070)
- Add `KRPC.NextTick`, which releases the tick being held and holds the one after it (#1070)
- Fix the game freezing for as long as a serial connection takes to carry an RPC; serial ports
  are read and written on their own threads (#1035)
- Add `KRPC.ObjectDestroyedException`, raised when the game object that an object refers to no
  longer exists (#1051)
- Objects whose game objects the game no longer has are removed from the object store when a
  game state is loaded (#1051)
- Fix one server stopping emptying the object store that every server shares, which
  invalidated the objects held by clients of the servers still running (#1020)
- Support for nullable values across all types (#1017)
- **Breaking:** Null values are signaled by an `is_null` field rather than by an object id of
  0. Class-typed arguments are no longer implicitly nullable (#1017)
- Fix a request that names an object the server no longer has leaving the connection
  stuck (#1019)

## [v0.6.0]
- Add `Version` property to `Core`, set by the server plugin on startup (#848)
- Enable `TCP_NODELAY` on client TCP connections, reducing RPC round-trip latency (#879)
- Surface deprecated members (annotated with `[Obsolete]`) in the service definition and over the wire (#904)
- Add `KRPC.GameScene` and make it settable to switch the current game scene (#897)
- **Deprecated:** `KRPC.CurrentGameScene`, kept as a read-only alias of `KRPC.GameScene` (#897)
- Add `AstronautComplex`, `MissionControl`, `ResearchAndDevelopment`, `Administration` and `MissionBuilder` game scenes (#897)
- Fix the game scenes a procedure is available in never being sent by `GetServices`; the
  `game_scenes` field of every procedure was always empty (#991)
- Reduce copying and allocation overhead receiving protobuf messages (#972)
- Fix the websocket servers rejecting connection requests that are split across multiple reads (#973)
- Fix websocket connection URL query parameter parsing (#973)
- Fix locale issues with type codes in the service description, and HTTP and websocket protocol tokens (#993)

## [v0.5.4]
- Initial version, split off from `KRPC.dll`
