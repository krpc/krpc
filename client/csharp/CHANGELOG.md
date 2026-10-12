## [v0.7.0] - unreleased
- **Breaking:** Start a stream when it is added, removing `Stream.Start` and `Event.Start`
  (#1122)
- Wait for a stream's first value in `Stream.Get` (#1122)
- Call a callback added to a stream that has a value with that value straight away (#1122)
- Call a callback added to an event straight away for a firing not yet passed to a wait or
  another callback (#1122)
- **Breaking:** Add an `IConnection.Invoke` overload taking the return type, which a class
  implementing `IConnection` must provide (#1122)
- Throw `InvalidOperationException` when reading or waiting on a removed stream or event (#1122)
- End a stream on an error from the server, which every later `Get` and `Wait` throws (#1122)
- End every stream with `ConnectionException` when the connection closes or fails (#1122)
- **Breaking:** Return `true` from `Stream.Wait` and `Event.Wait` on an update, and `false` on
  a timeout (#1122)
- Fix `Event.Wait` missing a firing that arrived before it (#1122)
- Fix `Event.Wait` waking only one of two threads waiting on one event (#1122)
- Add `AddEndCallback` to streams and events, called with the exception that ends the stream
  (#1122)
- Fix the stream update thread ending the process on a value it cannot decode or a message it
  cannot parse (#1122)
- Fix a second removal of a stream removing a newer stream for the same call (#1122)
- Fix stream callbacks throwing an `InvalidCastException` on an error from the server (#1122)
- Throw `InvalidOperationException` from `AddStream` on a connection with no stream server
  (#1122)
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
- Fix an `ArgumentException` from the server carrying its message as the parameter name (#1111)

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
