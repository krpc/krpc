## [v0.8.0]
- **Breaking:** Connect with protocol version 2 (#1219)
- **Breaking:** Remove `KRPC_SINGLE_CONNECTION`, as every mechanism opens one connection
  (#1219)
- **Breaking:** Return `KRPC_ERROR_CONNECTION_REFUSED` from `krpc_connect` when the server
  denies the connection or does not support the client's protocol version (#1219)
- **Breaking:** Return `KRPC_ERROR_CONNECTION_FAILED` from `krpc_connect` when the server's
  response cannot be read (#1219)
- Add `krpc_disconnect`, which sends the server a disconnect message before closing the
  connection (#1219)
- Skip messages an earlier session left on a serial line while connecting (#1219)
- Name the references in service documentation by their C names (#1185)
- Document every parameter of a service function, with the value returned under
  `returnValue` (#1185)
- Fix the documentation of the services that Doxygen warns about, such as code holding an
  `@` (#1185)

- Transports
  - **Breaking:** Build every transport into the library, and choose one when opening a
    connection with `krpc_open_serial`, `krpc_open_tcp` or `krpc_open_localsocket`, in place of
    `krpc_open` (#1176)
  - **Breaking:** Remove `KRPC_COMMUNICATION_TCP`, `KRPC_COMMUNICATION_LOCALSOCKET` and
    `krpc_connection_config_t`, and the `tcp` and `localsocket` vcpkg features (#1176)
  - **Breaking:** Replace `KRPC_COMMUNICATION_CUSTOM` with a `krpc_transport_t` that a program
    fills in (#1176)
  - **Breaking:** On Arduino, open any `Stream`, such as `SoftwareSerial` or a USB port, with
    `krpc_open_stream`, or start a serial port with `krpc_open_serial` (#1176)

- **Breaking:** Return `krpc_KRPC_GetStatus` as `krpc_KRPC_ServerStatus_t`, whose `version` is
  allocated on the heap (#1244)
- **Breaking:** Call procedures by the hashed ids, which needs a server of the same version (#1265)
- **Breaking:** Return a stream or an event as the `uint64_t` id of its stream (#1266)
- **Breaking:** Replace `krpc_KRPC_CompileFunction` and `krpc_KRPC_RunFunction` with
  `krpc_compile_function` and `krpc_run_function`, in `krpc_cnano/functions.h` (#1267)
- Add a `_call` function for each procedure, such as `krpc_SpaceCenter_Flight_Speed_call`, for
  `krpc_KRPC_Expression_Call` (#1267)
- **Breaking:** Leave internal procedures out of the services, such as `krpc_KRPC_AddStream` (#1269)
- Relicense the C-nano client and the Arduino library under the MIT license (#1281)
- Mark deprecated classes, enumerations, enumeration values, structures and structure fields with
  `KRPC_DEPRECATED` (#1322)
- Mark the `_call` functions of deprecated procedures with `KRPC_DEPRECATED` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)

- Errors
  - **Breaking:** Replace the error codes with one per kind of error: `KRPC_ERROR_CONNECTION`,
    `KRPC_ERROR_SERVER`, `KRPC_ERROR_ENCODING`, `KRPC_ERROR_ARGUMENT` and `KRPC_ERROR_NO_MEMORY`.
    `KRPC_ERROR_CONNECTION_REFUSED`, `KRPC_ERROR_BUFFER_TOO_SMALL`, `KRPC_ERROR_IO` and
    `KRPC_ERROR_EOF` stay (#1333)
  - Add `krpc_get_error_info`, the details of the last error: the ids of a server exception, the
    status of a refused connection, the reason of a disconnect and the transport's failure (#1333)
  - Add a `KRPC_EXCEPTION_` macro for each exception of a service, and `KRPC_IS_EXCEPTION` to
    check the last error against it (#1333)
  - Add `KRPC_ERROR_CODES_ONLY`, which keeps only the code of each error (#1333)
  - **Breaking:** Give a message for every error under `KRPC_ERROR_MESSAGES`, with the message of
    its cause, which a transport gives with the new `krpc_set_error_cause` (#1333)
  - **Breaking:** Return `KRPC_ERROR_ARGUMENT` for a null string argument, a null argument to a
    `krpc_open_` function, and a local socket path that is too long (#1333)

- Add a `KRPC_CLASS_`, `KRPC_ENUMERATION_` or `KRPC_STRUCT_` macro for each type of a service, with
  its ids for server side functions (#1336)
- Add a `KRPC_FIELD_` macro for each field of a structure, with its position for
  `krpc_KRPC_Expression_GetFieldByIndex` (#1336)
- Add a `_call_template` function for each member, which fills in a call with no instance or
  arguments, for `krpc_KRPC_Expression_CallWithArguments` (#1352)
- Fix the clang-tidy warning from `KRPC_FREE_LIST` and `KRPC_FREE_SET` on a list or set of strings (#1375)
- Fix the Arduino library failing to compile with `pb_common.h: No such file or directory` (#1387)

## [v0.7.0]
- **Breaking:** Support null for any nullable type. A nullable non-class return adds a
  `bool* returnValueIsNull` out-parameter, and a nullable argument is a pointer (#1017)
- **Breaking:** Renumber the `KRPC` service's procedures, so a program built against an
  earlier version must be rebuilt (#1124)
- **Breaking:** Rename `dockingcamera.h` to `docking_camera.h` (#1149)
- **Breaking:** Send protocol version 1 when connecting, and fail to connect to a server from
  before 0.7 (#1162)
- Support structure types, as a C struct with a member per field, and nullable structure fields
  and collection elements, as the value beside a `bool` presence flag (#1066, #1091)
- Add `KRPC_COMMUNICATION_TCP` and `KRPC_COMMUNICATION_LOCALSOCKET`, with `tcp` and
  `localsocket` vcpkg features, to communicate over TCP/IP or a unix domain socket
  (#1055, #1065)
- Add `KRPC_SINGLE_CONNECTION`, for a `KRPC_COMMUNICATION_CUSTOM` mechanism that opens its own
  connection to each server (#1055)
- Reduce a remote procedure call from tens of reads and writes to one write and two reads (#1056)
- Add `KRPC_BUFFER_SIZE`, how much of a message to hold in memory while it is sent or received,
  1024 bytes by default and 128 on Arduino (#1056)
- Include the header of every service a service header uses, so service headers can be included
  in any order (#1150)
- Update to protobuf v36.2 and nanopb 0.4.9.2 (#1139)

## [v0.6.0]
- Distribute via vcpkg (#874)
- **Breaking:** Drop autotools build; CMake is now the only supported build system (#870)
- **Breaking:** Modernize CMake build: require CMake 3.15+, target-based usage requirements (#834)
- **Breaking:** nanopb is now an external dependency (fetched via `FetchContent` or found on the system) rather than being bundled in the release archive; the Arduino package still bundles nanopb (#870)
- **Breaking:** Pre-generated protocol buffer code and `krpc.proto` are bundled in the release archive; set `KRPC_REGENERATE_PROTO=ON` to regenerate the code from `krpc.proto` using the nanopb generator (#872)
- Add CMake package config so consumers can use `find_package(krpc_cnano CONFIG REQUIRED)` (#870)
- CMake dependency options (`KRPC_FETCH_DEPS`, `KRPC_FETCH_PROTOBUF`, `KRPC_FETCH_NANOPB`): when OFF
  (default) the system install is required; when ON `FetchContent` is used. (#870)
- Add support for serial port communication on Windows (#872)
- Mark deprecated functions with the `KRPC_DEPRECATED` attribute macro (#904)
- Generated functions are now defined as static inline, so the generated
  service headers can be safely included from multiple translation units (#948)
- **Breaking:** On Arduino, a read now fails with `KRPC_ERROR_EOF` when the serial timeout
  elapses instead of retrying forever (#1002)
- Add `KRPC_ERROR_MESSAGES` option, which captures the message describing an error returned
  by the server so that it can be read using `krpc_get_error_message`. (#1002)
- Fix corruption of received data on POSIX systems when a serial read returns fewer bytes than
  were asked for (#1002)

## [v0.5.2]
- The arduino compatible version of the library now uses the same "krpc_cnano/..." include directory as the version released on GitHub.

## [v0.5.0]
- Update to protobuf 22.0
- Update to nanopb 0.4.7
- Rename `KRPC_COMMS_CUSTOM` to `KRPC_COMMUNICATION_CUSTOM`

## [v0.4.8]
- Update to protobuf v3.6.1
- Change second parameter of `krpc_open` to be a configuration parameter of type `krpc_connection_config_t` to allow additional configuration options to be passed, such as baud rate for Arduino serial port (#487)
- Fix encoding issue in procedure call messages that was causing compilation on Arduino Due to fail

## [v0.4.7]
- Fix include paths on Arduino (#482)

## [v0.4.6]
- Include directory and main header now suffixed with "_cnano" to avoid name clashes with C++ client

## [v0.4.5]
- Fix connection timing out on Arduino when connecting to a server with new client confirmation enabled (#446)

## [v0.4.3]
- Remove `KRPC_NO_PRINT_ERRORS` option (now enabled by default) and replace with `KRPC_PRINT_ERRORS_TO_STDERR`
- Enable `PB_NO_ERRMSG` by default in Arduino version of library
- Invoke remote procedures using service and produce identifiers instead of names to reduce code size and communication overhead

## [v0.4.1]
- Initial version
