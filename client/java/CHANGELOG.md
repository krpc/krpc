## [v0.7.0] - unreleased
- Support a nullable structure field, list element, tuple item and dictionary value, held as
  the boxed type (`Integer`, `Double`, …) where the value is a primitive (#1091)
- Support structure types, a compound value with named fields a service defines, generated as
  a class with a constructor, getters, `equals`, `hashCode` and `Comparable` over its
  fields (#1066)
- **Breaking:** Requires Java 17 or later, up from Java 9, for unix domain sockets (#1065)
- **Breaking:** Support `null` for any nullable type; nullable value types use the boxed type
  (`Integer`, `Double`, …) (#1017)
- Add `Connection.newLocalInstance`, which connects to a server on the same machine over unix
  domain sockets (#1065)
- Add `Connection.newInstance` overloads taking a `timeout`, bounding how long a connection is
  waited for (#1065)

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
