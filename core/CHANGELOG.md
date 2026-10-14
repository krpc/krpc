## [v0.7.0] - unreleased
- Add `KRPC.GameStateLoads`, the number of game states that have finished loading (#1121)
- **Breaking:** Remove `KRPC.CurrentGameScene`, replaced by `KRPC.GameScene` (#1140)
- Extend `KRPC.Expression` to build server side functions, which the server evaluates
  (#503, #517, #521, #1124)
- Allow a function to call a procedure that returns no value (#1124)
- Add `KRPC.Expression.CallWithArguments`, which computes the arguments of a call (#1124)
- Allow a function to raise and handle exceptions (#1124)
- Add `KRPC.DivideByZeroException`, `KRPC.NotSupportedException`, `KRPC.TimeoutException` and
  `KRPC.OverflowException`, raised for the matching errors by any service (#1124)
- Add `KRPC.Error`, a general error a function can raise (#1124)
- Allow a function to declare local variables, loop, branch, assign and return early (#1124)
- Allow a function to work on strings (#1124)
- Allow a function to select, reshape and combine collections, including the keys and values of
  a dictionary (#1124)
- Allow a function to change a list, set or dictionary (#1124)
- Add `KRPC.Expression.CreateStruct`, `GetField`, `CreateEmptyList`, `CreateEmptySet` and
  `CreateEmptyDictionary`, which build and read structures and empty collections (#1124)
- Add `KRPC.Expression.ConstantLong`, `ConstantUInt`, `ConstantULong`, `ConstantBytes`,
  `ConstantEnum`, `ConstantObject` and `ConstantNull` (#1124)
- Add `KRPC.Expression.IsNull`, `Negate`, `Conditional`, `ConditionalAnd` and `ConditionalOr`
  (#1124)
- Track which values of a function can be null, and raise an error when a null reaches a value
  whose type does not allow one (#1124)
- Add `KRPC.NullReferenceException`, raised for a null by any service, and by a function when a
  null reaches a value whose type does not allow one (#1124)
- Allow a function to use nullable booleans and structures (#1124)
- Add `KRPC.KeyNotFoundException`, raised for a missing key by any service, and by a function
  reading a missing dictionary key (#1124)
- Add `KRPC.IndexOutOfRangeException`, raised for an index out of range by any service, and by
  a function indexing a list or string (#1124)
- Add `KRPC.RunFunction`, which runs a server side function once within a single physics tick,
  with arguments of the types `KRPC.Expression.ParameterTypes` gives (#1124)
- Add `KRPC.CompileFunction`, which compiles a server side function before its first run
  (#1124)
- Add `KRPC.Expression.StateVariable`, a variable that keeps its value between evaluations of
  a function (#1124)
- Add `KRPC.AddFunctionStream`, which streams the value a server side function computes on each
  update (#1124)
- Add `KRPC.Expression.ReturnType` and `HasReturnType`, which report the type of the value a
  function produces, and whether it produces one (#1124)
- Add the `StdLib` service, a standard library of scalar mathematics and of vector and quaternion
  operations over the tuples the SpaceCenter service uses (#1125)
- Add `StdLib.Print`, which writes a line to the game's log, for debugging server side functions
  (#1125)
- Expand `KRPC.Type` to name every type a procedure takes or returns, including long, unsigned,
  bytes, class, enumeration, structure and collection types, and a nullable form of any type
  (#1124)
- Add `KRPC.TypeCode`, and the `Code`, `Service`, `Name`, `Types` and `Nullable` properties of
  `KRPC.Type`, which report what a type is (#1124)
- Fix `KRPC.Expression.Count`, which failed for every collection a procedure returns (#1124)
- Fix `KRPC.Expression.OrderBy` to order strings by character code on every system locale
  (#1124)
- Fix `KRPC.Expression.Equal` and `NotEqual`, which compared objects, tuples, bytes and
  collections by reference (#1124)
- Fix `KRPC.Expression.CreateSet` to compare bytes, collections and structures by content
  (#1124)
- Fix `KRPC.Expression.Contains` and `ToSet`, which compared objects, tuples, bytes and
  collections by reference (#1124)
- Fix `KRPC.Expression.And`, `Or`, `ExclusiveOr`, `LeftShift` and `RightShift` for operands of
  different integer types (#1124)
- Fix `KRPC.Expression.Divide` and `Modulo` to wrap for the smallest integer divided by -1
  (#1124)
- Fix `KRPC.Expression.Sum`, `Min`, `Max` and `Average` over unsigned and long integers, and
  wrap `Sum` on overflow (#1124)
- **Breaking:** Rename `KRPC.Expression.Function` to `KRPC.Expression.Lambda` (#1124)
- **Breaking:** Renumber the procedures of the `KRPC` service, so a client that calls them by
  id must use stubs generated for this version (#1124)
- **Breaking:** Rename the parameter of `KRPC.AddEvent` from `expression` to `function` (#1124)
- **Breaking:** Give `KRPC.Expression.Power` the common type of its operands, rather than the
  type of the first (#1124)
- **Breaking:** Require the index into a tuple given to `KRPC.Expression.Get` to be a constant
  (#1124)
- **Breaking:** Limit `KRPC.Expression.CreateTuple` to seven elements, the most a kRPC tuple
  type can hold (#1124)
- **Breaking:** Reject calls to `KRPC` service procedures in a function (#1124)
- Widen a number, tuple or collection given to `KRPC.Expression.Invoke`, and convert each element
  of a tuple or collection given to `KRPC.Expression.Cast` (#1124)
- Widen the numbers given to `KRPC.Expression.CreateList`, `CreateSet` and `CreateDictionary` to
  a common type (#1124)
- Widen the values of the collections given to `KRPC.Expression.Concat` to a common type
  (#1124)
- Add `KRPC.ObjectNotLoadedException`, for an object that exists but is not loaded, such as a
  part of an unloaded vessel (#1120)
- **Breaking:** Throw `KRPC.UnsupportedSceneException` for a procedure called outside its game
  scenes (#1119)
- Hold a stream or event over an object that is not loaded, or a procedure not available in
  the game scene, until it is available (#1122, #877)
- **Breaking:** Create a new stream for every `KRPC.AddStream`, so removing one leaves the
  others for the same call (#1122, #902)
- **Breaking:** Start a stream when it is added, removing `KRPC.StartStream` and the `start`
  argument of `KRPC.AddStream` (#1122)
- Fix a failing stream or event, including one whose value fails to encode, stopping the other
  streams (#1122)
- Throw an exception when streaming a procedure that returns a stream or an event, or calling
  one in an expression (#1122)
- Fix an expression calling a failing procedure throwing a `NullReferenceException` in place of
  the procedure's exception (#1122)
- Add `KRPC.StreamRemovedException`, sent as the final result of a stream the server ends, such
  as an event that has finished (#1122)
- Ignore a call to `KRPC.SetStreamRate` for a stream that has ended (#1122)
- Throw an exception from `KRPC.RemoveStream` for a stream id that was never issued (#1122)
- Fix the server keeping memory for every stream ever sent on a connection (#1122)
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
