## [v0.7.0] - unreleased
- Add `Connection.CompileFunction`, which compiles a lambda taking no arguments into a server
  side function that computes the same result on the server (#1069)
- Evaluate the calls a lambda makes on remote objects and services on the server, and everything
  else once when compiling it (#1069)
- Accept operators, conditionals, casts, `System.Math` methods, string operations, collection,
  tuple and structure constructors, and the LINQ operators (#1069)
- Throw `FunctionCompilationException` naming an unsupported construct (#1069)
- Add `Connection.RunFunction`, which runs a server side function on the server within a single
  physics tick and returns the value it produces (#1069)
- Add a `Connection.AddStream<T>` overload taking a server side function, which streams the
  value one computes (#1069)
- Create an event from a server side function with `Connection.AddEvent` (#1069)
- Compile a lambda passed directly to `RunFunction`, `AddStream` or `AddEvent` (#1069)
- Add `Function.Defer`, which starts a call to a procedure that pauses execution, such as
  `SpaceCenter.WarpTo`, without waiting for it (#1069)
- Add a `Connection.CompileFunction` overload taking a lambda with no result (#1069)
- Compile any lambda passed to `Connection.AddStream` that is not a single call or property
  access (#521)
- Support a nullable structure field, list element, tuple item and dictionary value; a
  value-typed one uses the nullable form (`int?`) (#1091)
- Support structure types, a compound value with named fields a service defines, generated as
  a `struct` with a constructor, `IEquatable`, `IComparable` and the comparison operators over
  its fields (#1066)
- Stream expressions can be multiplied by a constant (#1062)
- **Breaking:** Support `null` for any nullable type; nullable value types use the nullable
  form (`int?`) (#1017)
- Add `Connection.ConnectLocal`, which connects to a server on the same machine over unix
  domain sockets (#1065)
- Add a `timeout` parameter to the `Connection` constructor, bounding how long a connection is
  waited for (#1065)
- Reduce the cost of a remote procedure call, encoding and decoding without a protobuf stream
  and parsing a response straight out of the read buffer (#1056)
- Reduce the cost of a call that returns a collection or an object (#1056)

## [v0.6.0]
- **Breaking:** Requires .NET Framework 4.7.2 or later (#948)
- Update to protobuf v3.35.1 (#850)
- Mark deprecated members with the `[Obsolete]` attribute (#904)
- Disposing a connection now stops and joins the stream update thread (#1005)
- An error from a service whose exception types were never registered now raises an
  `RPCException` describing it (#1005)
- An exception thrown by a stream or event callback no longer escapes the stream update thread (#1005)
- Fix a deadlock between the stream update thread and a thread waiting for an update (#1005)

## [v0.5.0]
- Update to protobuf v3.22.0
- Drop support for net35

## [v0.4.8]
- Update to protobuf v3.6.1

## [v0.4.6]
- Add methods to remove callbacks from streams and events (#451)

## [v0.4.5]
- Update to protobuf v3.5.1

## [v0.4.3]
- Add rate control for streams (#116, #141)

## [v0.4.0]
- Updated protocol in line with server changes
- Add support for RPCs and streams to throw exceptions

## [v0.3.11]
- Update to protobuf v3.4.1

## [v0.3.10]
- Add support for .NET 3.5 (allows use of the client from within KSP itself)
- Update to protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0

## [v0.3.8]
- Update to protobuf v3.2.0

## [v0.3.7]
- Update to protobuf v3.1.0
- Remove pre-release flag from nuget version

## [v0.3.5]
- Fix race condition where the connection constructor returns before the stream server connection has been established
- Make `Connection` and `StreamManager` disposable so that they clean up resources correctly
- Fix issue where network streams are closed prematurely
- Fix issue with receiving partial protobuf messages

## [v0.3.4]
- Update to protobuf v3.0.0-beta-3

## [v0.2.3]
- Make client thread safe

## [v0.2.2]
- Remove support for protobuf enumeration and custom protobuf messages

## [v0.2.1]
- Add documentation to generated service code
- Add support for streams

## [v0.2.0]
- Initial version
