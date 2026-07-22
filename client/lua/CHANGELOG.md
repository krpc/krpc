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
