## [v0.8.0]
- **Breaking:** Connect with protocol version 2, and fail to connect to a server from before
  0.8 (#1219)
- Add a `handshake_timeout` parameter to `connect` and `connect_local` (#1219)
- Tell the server when the connection closes (#1219)
- **Breaking:** Return `get_status` as a struct, with the settings and statistics under
  `settings` and `statistics` (#1244)
- **Breaking:** Return a stream or an event as the id of its stream (#1266)
- **Breaking:** Replace `KRPC.compile_function` and `KRPC.run_function` with `Client:compile` and
  `Client:run` (#1267)
- Add `Connection:get_call`, for `KRPC.Expression.call` (#1267)
- **Breaking:** Leave internal procedures out of the services, such as `KRPC.add_stream` (#1269)

- Null values
  - **Breaking:** Return `nil` for a null result, in place of `Types.none` (#1278)
  - Pass a `nil` argument as null, for a parameter that can be null and has no default (#1278)
  - Fix a `nil` argument dropping the arguments after it (#1278)

- Add `union`, `intersection`, `difference` and `contains` to the values of a flags enumeration (#1311)
- Warn once per member on standard error on a use of a deprecated service, procedure, method,
  property, class, enumeration, enumeration value or structure (#1322)
- Warn when `get_call` builds the call of a deprecated procedure, method or property (#1322)
- Deprecate each member of a deprecated service, class or enumeration (#1322)
- **Deprecated:** `krpc.platform.sleep`, in favor of luasocket's `socket.sleep`. It now sleeps
  without holding a CPU core, and for fractions of a second (#1323)

- Errors
  - **Breaking:** Raise errors as objects of the new classes `krpc.RemoteError`,
    `krpc.ConnectionError`, `krpc.ServerError` and `krpc.EncodingError`, in place of strings.
    An argument the client rejects still raises a string (#1333)
  - **Breaking:** Drop the source position and the `Service.Name:` prefix from the message of
    an error, which the error's fields now carry (#1333)
  - **Breaking:** Raise `ConnectionError` for malformed service definitions (#1333)
  - **Breaking:** Raise `bad argument #2 to 'connect_local'` for a local socket path that is too
    long (#1333)

- **Breaking:** Convert names with `UInt`, `ULong`, `SInt`, `FoV`, a plural acronym or digits
  next to an acronym as written, e.g. `Type.uint`, `Camera.fov` and `MapFilterType.evas` in place of
  `u_int`, `fo_v` and `ev_as` (#1339)
- Add `Connection:get_call_template`, which builds a call with no instance or arguments, for
  `Expression.call_with_arguments` (#1352)
- Take a procedure, such as `Engine.get_has_fuel`, as its call template where a call is expected (#1352)
- Order remote objects by id with `<` (#1377)
- Fix a returned enumeration value comparing unequal to the same named value (#1377)
- Raise an error for a handshake timeout that is not positive (#1377)

## [v0.7.0]
- **Breaking:** Support `Types.none` for any nullable type, including a structure field, list
  element, tuple item and dictionary value (#1017, #1091)
- **Breaking:** Return messages, such as the server status, as plain tables (#1142)
- **Breaking:** Rename `conn.li_dar` to `conn.lidar`, for the `LIDAR` service (#1149)
- **Breaking:** Send protocol version 1 when connecting, and fail to connect to a server from
  before 0.7 (#1162)
- Support Lua 5.3 and 5.4, where 64-bit values decode to exact integers (#1142)
- Depend on `lua-protobuf` in place of `protobuf`, and require luasocket 3.0 or later
  (#1142, #1060)
- Add `krpc.connect_local`, which connects to a server on the same machine over a unix domain
  socket, on Linux and macOS (#1065)
- Support structure types, built from their field values in order with named access (#1066)
- Skip a service member naming an unknown type with a warning, and connect (#1066)
- Add `krpc.limits`, the extremes of the numeric types kRPC carries, such as
  `krpc.limits.SINT32_MAX` (#1045)
- Add `krpc.get_object_id`, which returns the id of a remote object for
  `Expression.constant_object` (#1160)
- Reduce the cost of a remote procedure call (#1056)
- Fix a service failing to load when a default value names an enumeration of a service not yet
  loaded (#1044)
- Fix a type occasionally being built twice, leaving an enumeration or class type unusable
  (#1068)
- Fix the client assigning global variables, such as `data` and `typ` (#1142)

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
