## [v0.7.0] - unreleased
- Add structure types: a compound value with named fields, whose value is sent to the client in
  full rather than as a reference to an object that stays on the server. A structure is declared
  with `KRPCStruct` on a C# `struct`, its fields are the properties annotated with
  `KRPCProperty`, and its value is encoded as the values of those fields in the order the
  structure declares them (#1066)
- Add a communication protocol carrying protocol buffer messages over a unix domain socket, for
  clients running on the same machine as the game. It carries the same messages as the TCP/IP
  protocol over a cheaper path: a client that makes many calls in quick succession completes
  around 20% more of them per physics update, and around 35% more when the values are large.
  A client that makes one call and then waits is unaffected, as the wait is governed by the
  game's update rate. Available on Linux, macOS, and Windows 10 1803 and later (#1065)
- Add `KRPC.HoldTick` and `KRPC.ReleaseTick`, which hold the game on its current physics tick so
  that a program can read the game state, compute with it and write the result back without the
  game advancing in between. Intended for control loops, which are otherwise served one RPC per
  tick whenever they spend longer than the receive timeout between calls. The game runs no
  physics and draws no frame while a tick is held, and a hold ends by itself after
  `TickHoldTimeout` (#1070)
- Fix the game freezing for as long as a serial connection takes to carry an RPC; serial ports are
  now read and written on their own threads rather than on the thread that runs the game. Data
  waiting to be sent is buffered up to 1 MB, beyond which the connection is dropped (#1035)
- Add `KRPC.ObjectDestroyedException`, raised when the game object that an object refers to
  no longer exists (#1051)
- Objects whose game objects the game no longer has are removed from the object store when a
  game state is loaded, and using an object that has been removed raises
  `KRPC.ObjectDestroyedException` rather than an argument error (#1051)
- Fix one server stopping emptying the object store that every server shares, which invalidated
  the objects held by clients of the servers still running (#1020)
- Support for nullable values across all types (#1017)
- **Breaking:** Null values are now signaled by an `is_null` field rather than by an object id
  of 0, and nullability is enforced uniformly for every type. Class-typed arguments are no longer implicitly nullable (#1017)
- Fix a request that names an object the server no longer has leaving the connection stuck, with
  every later call failing; the failure is now reported to the client as the error it is (#1019)

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
