## [v0.7.0] - unreleased

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `Stream.Start` and `Event.Start`
    (#1122)
  - **Breaking:** Return `true` from `Stream.Wait` and `Event.Wait` on an update, and `false` on
    a timeout (#1122)
  - **Breaking:** Add an `IConnection.Invoke` overload taking the return type, which a class
    implementing `IConnection` must provide (#1122)
  - Wait for a stream's first value in `Stream.Get` (#1122)
  - End a stream on an error from the server, and every stream with `ConnectionException` when
    the connection closes or fails (#1122)
  - Throw `InvalidOperationException` when reading or waiting on a removed stream or event, and
    from `AddStream` on a connection with no stream server (#1122)
  - Add `AddEndCallback` to streams and events, called with the exception that ends the stream
    (#1122)
  - Call a callback added to a stream or event straight away for a value or firing already
    received (#1122)
  - Fix `Event.Wait` missing an earlier firing, and waking only one of two waiting threads
    (#1122)
  - Fix the stream update thread ending the process on a value it cannot decode or a message it
    cannot parse (#1122)
  - Fix a second removal of a stream removing a newer stream for the same call, and stream
    callbacks throwing `InvalidCastException` on an error from the server (#1122)

- Server side functions
  - Add `Connection.Compile`, which compiles a lambda into a server side function (#1128)
  - Compile operators, conditionals, casts, `System.Math`, string operations, collection, tuple
    and structure constructors, `Console.WriteLine` and the common LINQ operators (#1128)
  - Throw `FunctionCompilationException` naming a construct a lambda cannot contain, including
    a call that depends on the current culture (#1128)
  - Add `Connection.Run` and `Connection.AddEvent`, and a `Connection.AddStream<T>` overload
    taking a server side function (#1128)
  - **Breaking:** Compile a lambda given to `AddStream`, so the remote calls in its arguments
    are evaluated on every update (#1128)
  - **Breaking:** Replace `RemoteObject.id` with `RemoteObject.GetObjectId`, which returns the
    id of a remote object for `Expression.ConstantObject` (#1160)

- Types and errors
  - **Breaking:** Throw the matching .NET exception, such as `NullReferenceException`,
    `KeyNotFoundException` or `TimeoutException`, for an error reported by any service, in place
    of `RPCException` (#1124)
  - **Breaking:** Support `null` for any nullable type, including a structure field, list
    element, tuple item and dictionary value. Value types use the nullable form (`int?`)
    (#1017, #1091)
  - Throw `InvalidOperationException` from `Stream.Get` for a null value the stream's type
    cannot hold (#1128)
  - Support structure types, generated as a `struct` with a constructor, equality and ordering
    over its fields (#1066)
  - Fix an `ArgumentException` from the server carrying its message as the parameter name
    (#1111)

- Connections
  - Add `Connection.ConnectLocal`, which connects to a server on the same machine over a unix
    domain socket (#1065)
  - Add a `timeout` parameter to the `Connection` constructor, bounding how long a connection is
    waited for (#1065)
  - Reduce the cost of a remote procedure call, most for one that returns a collection or an
    object (#1056)
  - Update to Google.Protobuf 3.36.2 (#1139)

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
