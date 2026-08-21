## [v0.7.0] - unreleased
- Add server side functions: a program the client builds on the server out of the operations
  the `KRPC.Expression` class provides, and then evaluates there. A function can call the
  procedures of any service, including on each value of a collection (#517), pass objects
  around as constants (#503), compute over operands of differing numeric types (#521), process
  collections, and declare local variables, loop, branch, assign and return early. It can build
  and read the tuples and structures a service defines, and produce a value of any type kRPC
  carries (#1069)
- Add `KRPC.RunFunction`, which evaluates a server side function exactly once within a single
  physics tick and returns the value it produces, encoded for its own type. This is the way to
  run a function that changes the game, which an event or a stream would repeat on every update
  (#1069)
- Add `KRPC.AddExpressionStream`, which streams the value a server side function computes on
  each update rather than the result of a single procedure call. `KRPC.Expression.ReturnType`
  reports the type of those values, so a client that does not know it in advance can decode
  them (#1069)
- Add the `StdLib` service, a standard library for use within server side functions: scalar
  mathematics, and vector and quaternion operations over the tuples the SpaceCenter service
  uses for positions, directions and rotations. It is written as an ordinary service, which is
  how anything else extends what a server side function can do (#1069)
- Expand `KRPC.Type` to name the class, enumeration, structure and collection types a service
  defines, alongside the value types it did not cover, and to report what a type is through its
  `Code`, `Service`, `Name` and `Types` properties (#1069)
- Fix `KRPC.Expression.Count`, which failed for every collection a procedure returns and worked
  only for a collection built within the expression itself (#1069)
- **Breaking:** the index into a tuple given to `KRPC.Expression.Get` must be a constant. It was
  evaluated while the expression was being built, so a procedure call in that position ran once,
  there, rather than whenever the expression is evaluated (#1069)
- An error raised while evaluating an event's expression is reported to the client as an error
  on the event's stream, rather than propagating out of the update that evaluates every stream
  and stopping the rest of them (#1069)
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
