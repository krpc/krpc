## [v0.8.0] - unreleased

- **Breaking:** Refuse a service whose parameter defaults to a value its enumeration does not
  declare (#1192)
- Exceptions
  - Report the .NET exception each kRPC exception stands for, as `mapped_type` in
    `KRPC.Exception` (#1172)
  - Allow a `<see cref>` in service documentation to link to an exception, including a .NET
    exception that a kRPC exception maps (#1185)

## [v0.7.0]

- Server side functions
  - Extend `KRPC.Expression` to build server side functions, which the server evaluates
    (#503, #517, #521, #1124)
  - Allow a function to declare local variables, loop, branch, assign, return early, raise and
    handle exceptions, and call a procedure that returns no value (#1124)
  - Allow a function to work on strings, nullable values, and lists, sets and dictionaries,
    including changing them (#1124)
  - Add `KRPC.RunFunction`, which runs a function once within a single physics tick, and
    `KRPC.CompileFunction` and `KRPC.AddFunctionStream` (#1124)
  - Add `KRPC.Expression.StateVariable`, a variable that keeps its value between evaluations of
    a function (#1124)
  - Add `KRPC.Expression.CallWithArguments`, `CreateStruct`, `GetField`, `IsNull`, `Negate`,
    `Conditional`, `ConditionalAnd`, `ConditionalOr`, the empty collections and a constant for
    every type (#1124)
  - Add `KRPC.Expression.ReturnType` and `HasReturnType` (#1124)
  - Add `KRPC.TypeCode` and the `Code`, `Service`, `Name`, `Types` and `Nullable` properties of
    `KRPC.Type`, which names every type a procedure takes or returns (#1124)
  - Add the `StdLib` service, of scalar mathematics and vector and quaternion operations, with
    `StdLib.Print` for debugging a function (#1125)
  - Widen numbers, tuples and collections to a common type in `KRPC.Expression.Invoke`, `Cast`,
    `CreateList`, `CreateSet`, `CreateDictionary` and `Concat` (#1124)
  - Fix `KRPC.Expression.Count` on a returned collection, and `OrderBy` ordering strings by the
    system locale (#1124)
  - Fix `KRPC.Expression.Equal`, `NotEqual`, `Contains`, `ToSet` and `CreateSet` comparing
    objects, tuples, bytes and collections by reference (#1124)
  - Fix the integer arithmetic of `KRPC.Expression` for mixed operand types, on overflow and for
    the smallest integer divided by -1 (#1124)
  - **Breaking:** Rename `KRPC.Expression.Function` to `Lambda`, and the parameter of
    `KRPC.AddEvent` from `expression` to `function` (#1124)
  - **Breaking:** Renumber the procedures of the `KRPC` service, so a client that calls them by
    id must use stubs generated for this version (#1124)
  - **Breaking:** Type `KRPC.Expression.Power` by both operands, require a constant index in
    `Get`, limit `CreateTuple` to seven elements, and reject calls to `KRPC` procedures (#1124)

- Errors
  - Add `KRPC.NullReferenceException`, `KeyNotFoundException`, `IndexOutOfRangeException`,
    `DivideByZeroException`, `NotSupportedException`, `TimeoutException` and
    `OverflowException`, raised by any service and by a function (#1124)
  - Add `KRPC.Error`, a general error a function can raise (#1124)
  - Add `KRPC.ObjectDestroyedException`, for an object whose game object is gone, and
    `KRPC.ObjectNotLoadedException`, for one that is not loaded (#1051, #1120)
  - **Breaking:** Throw `KRPC.UnsupportedSceneException` for a procedure called outside its
    game scenes (#1119)

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `KRPC.StartStream` and the `start`
    argument of `KRPC.AddStream` (#1122)
  - **Breaking:** Create a new stream for every `KRPC.AddStream`, so removing one leaves the
    others for the same call (#1122, #902)
  - Hold a stream or event over an unloaded object, or a procedure unavailable in the game
    scene, until it is available (#1122, #877)
  - Add `KRPC.StreamRemovedException`, the final result of a stream the server ends (#1122)
  - Throw an exception when streaming a procedure that returns a stream or event, and from
    `KRPC.RemoveStream` for an unknown stream id (#1122)
  - Fix a failing stream or event stopping the others, and the server keeping memory for every
    stream sent on a connection (#1122)

- Types and protocol
  - **Breaking:** Support null for any nullable type, signaled by an `is_null` field. Class-typed
    arguments are not implicitly nullable (#1017)
  - **Breaking:** Carry nullability in `Type.nullable` at every position, replacing
    `Parameter.nullable` and `Procedure.return_is_nullable` (#1091)
  - **Breaking:** Return `KRPC.Clients` as a list of `KRPC.ClientInfo` structures (#1148)
  - Allow a structure field, list element, tuple item and dictionary value to be nullable,
    declared with `Nullable<T>` or `KRPCNullable` (#1091)
  - Add structure types, a compound value with named fields, declared with `KRPCStruct` on a C#
    `struct` (#1066)
  - Add `StdLib.ColorRGB` and `StdLib.ColorRGBA`, the colors of the SpaceCenter, Drawing and UI
    services (#1148)
  - Add extension members, which a service declares on another service's class as C# extension
    methods (#1077)
  - Add a communication protocol over a unix domain socket, for clients on the same machine as
    the game (#1065)
  - Fix a serial connection freezing the game while it carries an RPC (#1035)
  - Add a protocol version to the connection handshake, sent over WebSockets as the subprotocol
    `krpc.N` and reported by `KRPC.GetStatus` (#1162)
  - Fix an exception on a serial connection request that carries no `ConnectionRequest` (#1162)
  - Encode and decode values without reflection, cutting the server cost of a call that passes
    or returns an object by about three quarters (#1091)

- Game state
  - **Breaking:** Remove `KRPC.CurrentGameScene`, replaced by `KRPC.GameScene` (#1140)
  - Add `KRPC.HoldTick`, `KRPC.ReleaseTick` and `KRPC.NextTick`, which hold the game on one
    physics tick so a control loop can read, compute and write within it (#1070)
  - Add `KRPC.GameStateLoads`, the number of game states that have finished loading (#1121)
  - Remove objects whose game objects are gone from the object store when a game state loads
    (#1051)
  - Fix one server stopping emptying the object store that every server shares (#1020)
  - Fix a request naming an object the server no longer has leaving the connection stuck
    (#1019)

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
