## [v0.7.0] - unreleased
- Support a nullable structure field, list element, tuple item and dictionary value, which is
  `Types.none` when absent (#1091)
- Support structure types, built from their field values in order and giving them named
  access (#1066)
- A service definition naming an unknown type skips the member that names it with a warning,
  instead of failing to connect (#1066)
- **Breaking:** Support `Types.none` for any nullable type (#1017)
- Add `krpc.connect_local`, which connects to a server on the same machine over a unix domain
  socket. On Windows this needs the `luasocket-unix-windows` rock, now a dependency (#1065)
- Fix a service failing to load when one of its procedures defaults to a member of an
  enumeration from a service that had not been loaded yet (#1044)
- Add `krpc.limits`, naming the extremes of the numeric types kRPC carries over the wire, such
  as `krpc.limits.SINT32_MAX` (#1045)
- Reduce the cost of a remote procedure call, writing and reading messages as bytes rather
  than as protocol buffer messages (#1056)
- Require luasocket 3.0 or later (#1060)
- Fix a type occasionally being built a second time, leaving an enumeration or class type in a
  service unusable (#1068)

## [v0.6.0]
- Fix `attributes` module to always return boolean for `is_a_class_member` and `is_a_class_property_accessor` (#850)
- Fix service, method and property names being converted to snake case using the machine's locale (#993)
- Calling a procedure with an argument that needs coercing no longer leaves a global named
  `ok` behind (#1003)
- Remove `encoder.client_name`; a leftover from before the protocol used protocol buffers (#1003)

## [v0.5.0]
- Update to protobuf v3.22.0

## [v0.4.8]
- Update to protobuf v3.6.1

## [v0.4.5]
- Update to protobuf v3.5.1

## [v0.4.0]
- Updated protocol in line with server changes

## [v0.3.10]
- Update to protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0

## [v0.3.8]
- Update to protobuf v3.2.0

## [v0.2.2]
- Remove support for protobuf enumeration and custom protobuf messages

## [v0.2.1]
- None

## [v0.2.0]
- Update to protobuf 3.0.0-beta-2

## [v0.1.12]
- None, bumped version number

## [v0.1.11]
- Initial version
