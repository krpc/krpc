## [v0.7.0] - unreleased
- **Breaking:** Support `null` for any nullable type; nullable value types use the nullable
  form (`int?`) (#1017)
- Reduce the cost of a remote procedure call. A value is encoded and decoded without a
  protobuf stream of its own, its bytes are written into a buffer of their own rather than one
  the thread keeps, a response is read a block at a time and parsed straight out of the read
  buffer, the request a call is built into is kept from one call to the next, and the client
  connections disable Nagle's algorithm (#1056)
- Reduce the cost of a call that returns a collection or an object. What kind of value a type
  carries, and how to build one, is worked out the first time the type is seen rather than for
  every value encoded or decoded, and a collection works out how to encode and decode its
  items once for the collection rather than once for every item (#1056)

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
