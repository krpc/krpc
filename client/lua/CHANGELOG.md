## [v0.7.0] - unreleased
- **Breaking:** Support `Types.none` for any nullable type (#1017)
- Fix a service failing to load when one of its procedures defaults to a member of an
  enumeration defined by a service that had not been loaded yet (#1044)
- Add `krpc.limits`, naming the extremes of the numeric types kRPC carries over the wire,
  such as `krpc.limits.SINT32_MAX` and `krpc.limits.DOUBLE_LOWEST`. Lua names none of them
  itself, as `math.maxinteger` and `math.mininteger` need Lua 5.3, and a parameter that
  defaults to one is now documented as the constant rather than the decimal value (#1045)
- Reduce the cost of a remote procedure call. The request carrying a call is written as the
  bytes it comes to, rather than built as a protocol buffer message and serialized, and so are
  the lists, sets, tuples and dictionaries a call carries, which are read back the same way. A
  value is encoded and decoded by looking up its type code rather than by asking the type what
  class it is, a float or a double is packed without building a protobuf field encoder for it,
  the size of a message is read directly rather than by handing each byte to a decoder until it
  stops raising an error, and the client connection disables Nagle's algorithm (#1056)

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
