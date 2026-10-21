## [v0.7.0] - unreleased

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `Stream.start`,
    `Stream.startAndWait` and `Event.start` (#1122)
  - **Breaking:** Return `true` from `Stream.waitForUpdateWithTimeout` and
    `Event.waitForWithTimeout` on an update, and `false` on a timeout (#1122)
  - Wait for a stream's first value in `Stream.get` (#1122)
  - End a stream on an error from the server, and every stream with a `StreamException` when
    the connection closes or fails (#1122)
  - Throw `StreamException` when reading or waiting on a removed stream or event (#1122)
  - Add `addEndCallback` to streams and events, called with the exception that ends the stream
    (#1122)
  - Call a callback added to a stream or event straight away for a value or firing already
    received (#1122)
  - Fix `Event.waitFor` missing an earlier firing, and waking only one of two waiting threads
    (#1122)
  - Fix the stream update thread stopping on a value it cannot decode (#1122)
  - Fix a second removal of a stream removing a newer stream for the same call, and an error
    ending a stream throwing `ClassCastException` in its callbacks (#1122)

- Server side functions
  - Add `Connection.run` and `Connection.addEvent`, and a `Connection.addStream` overload
    taking a server side function (#1129)
  - Add `RemoteObject.getObjectId`, which returns the id of a remote object for
    `Expression.constantObject` (#1160)

- Types and errors
  - **Breaking:** Throw the matching Java exception, such as `NullPointerException`,
    `NoSuchElementException` or `ArithmeticException`, for an error reported by any service, in
    place of `RPCException` (#1124)
  - **Breaking:** Support `null` for any nullable type, including a structure field, list
    element, tuple item and dictionary value. Primitives use the boxed type (`Integer`, ...)
    (#1017, #1091)
  - Support structure types, generated as a class with a constructor, getters, `equals`,
    `hashCode` and `Comparable` over its fields (#1066)

- Connections
  - **Breaking:** Send protocol version 1 when connecting, and fail to connect to a server from
    before 0.7 (#1162)
  - **Breaking:** Require Java 17 or later, for unix domain sockets (#1065)
  - Add `Connection.newLocalInstance`, which connects to a server on the same machine over a
    unix domain socket (#1065)
  - Add `Connection.newInstance` overloads taking a `timeout`, bounding how long a connection
    is waited for (#1065)
  - Update to protobuf-java 4.36.2 (#1139)

## [v0.6.0]
- Update to protobuf v4.35.1, guava 33.4.8-jre, antlr4-runtime 4.13.2 (#850)
- Mark deprecated members with the `@Deprecated` annotation and an `@deprecated` javadoc tag (#904)
- Reduce copying when encoding values (#979)
- Fix `Stream.getRate` always returning zero, whatever the rate had been set to (#1004)
- Fix looking up the method to stream or call (#1004)
- An exception thrown by a stream or event callback no longer ends the stream update thread (#1004)
- Fix a deadlock between the stream update thread and a thread waiting for an update (#1004)
- An error from a service with no generated stubs loaded now raises an `RPCException` describing it (#1004)

## [v0.5.0]
- Update to protobuf v3.22.0

## [v0.4.8]
- Make `Connection` class implement `AutoClosable` (#491)
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
- Clean up error reporting:
  - `ConnectionException` thrown when connection to a server fails
    This inherits `IOException`
  - Decoding messages now throws an unchecked `EncodingException`,
    as these errors should not occur as a result of user input.
- Minor code clean up, and source now checked with CheckStyle

## [v0.3.10]
- Update to protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0

## [v0.3.8]
- Update to protobuf v3.2.0

## [v0.3.4]
- Update to protobuf v3.0.0-beta-3

## [v0.2.3]
- Make client thread safe
- Support JDK 1.7+

## [v0.2.2]
- Initial version
