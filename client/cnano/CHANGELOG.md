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
