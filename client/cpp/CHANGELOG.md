## [v0.8.0]

- Connections
  - **Breaking:** Connect over one connection, removing the `stream_port` and `stream_path`
    parameters of `connect` and `connect_local`. A server from before 0.8 refuses the
    connection (#1219)
  - **Breaking:** Give up after 5 seconds if the server does not accept the connection. Pass a
    `handshake_timeout` of zero to wait for the player to approve it (#1219)
  - Add `handshake_timeout` and `stall_timeout` parameters to `connect` and `connect_local`
    (#1219)
  - Send calls from several threads without waiting for each other's responses (#1219)
  - Tell the server when the last client is released (#1219)
  - Add `status` and `reason` to `ConnectionError` for a refused or ended connection (#1219)
  - **Breaking:** Throw `ConnectionError` when the server cannot be reached, nesting the error
    from the socket (#1219)
  - Add `Client::close`, which closes the connection for every copy of the client (#1286)
  - **Breaking:** Services, objects, streams and events hold the connection open, so they stay valid once the client is destroyed (#1286)
  - **Breaking:** A client that is not connected throws `ConnectionError`, as does a default-constructed object (#1286)

- Streams and events
  - **Breaking:** Remove a stream or event from the server when its last copy is destroyed (#1219)
  - **Breaking:** Remove `Client::remove_stream` and the `Stream` constructor taking a stream id.
    Remove a stream through its stream object (#1219)
  - **Breaking:** Wait with no deadline in `Stream::wait` and `Event::wait` by default, or for an
    infinite timeout, and throw `std::invalid_argument` for a negative or NaN one (#1219)
  - Stream a procedure or server side function with no value as a `Stream<void>` (#1198)
  - **Breaking:** Return the stream id from `KRPC::add_stream` and `KRPC::add_function_stream` (#1266)
  - **Breaking:** Remove `Client::Returns::event`, as a call that returns an event uses
    `Returns::stream` (#1266)

- Fix the documentation of the services that Doxygen warns about, such as parameter names
  that do not match (#1185)
- Throw `EncodingError` when decoding a tuple of the wrong length, which crashed (#1194)
- Exceptions
  - **Breaking:** Throw a server exception with a standard equivalent as the standard
    exception, in place of `RPCError`. Its generated name is an alias of it, e.g.
    `KRPC::ArgumentException` of `std::invalid_argument` (#1172)
  - **Breaking:** Add the server stack trace to an exception's message (#1172)

- Add `bind` to classes with static methods, for calling them without passing the client (#1195)
- **Breaking:** Return `KRPC::get_status` as the struct `KRPC::ServerStatus`, whose fields are
  members, with the settings and statistics under `settings` and `statistics` (#1244)
- **Breaking:** Leave internal procedures out of the services, such as `KRPC::add_stream` (#1269)
- Add `krpc::compile` for a server side function built with the expression API (#1269)
- Raise the exception types of the `KRPC` service, such as `KRPC::InvalidOperationException`,
  without first creating a `KRPC` service object (#1269)
- Relicense the C++ client under the MIT license (#1281)
- **Breaking:** Service constructors take the client by reference, as in `SpaceCenter sc(conn)` (#1285)
- **Breaking:** Object, stream and event constructors take the client by reference, as in `Vessel(conn, id)` (#1286)
- Service constructors accept a temporary client, as in `SpaceCenter sc(krpc::connect())` (#1286)
- Generate the operators `|`, `&`, `^` and `~` for a flags enumeration (#1311)
- Declare a deprecated service's class `[[deprecated]]` (#1322)
- Declare the `_stream` and `_call` functions of deprecated procedures `[[deprecated]]` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)
- Fix generated headers warning about their own deprecated members (#1322)
- **Breaking:** Rename the framing classes `krpc::Connection` and `krpc::LocalConnection` to
  `krpc::TCPIPTransport` and `krpc::LocalTransport`, in the header `krpc/transport.hpp` (#1332)
- **Breaking:** Rename `krpc::Client` to `krpc::Connection`, in the header `krpc/connection.hpp`
  in place of `krpc/client.hpp`. `krpc.hpp` includes it as before (#1332)
- Throw `ConnectionError` when the connection request fails to be written (#1332)
- **Breaking:** Rename the base classes `krpc::Object` and `krpc::Service` to `krpc::RemoteObject`
  and `krpc::RemoteService`, in the headers `krpc/remote_object.hpp` and `krpc/remote_service.hpp` (#1332)

- Errors
  - **Breaking:** Derive every error of the client from the new `krpc::RemoteError` (#1333)
  - **Breaking:** Rename `krpc::RPCError` to `krpc::ServerError` (#1333)
  - **Breaking:** Throw `StreamError` when the server removes a stream or event (#1333)
  - **Breaking:** Throw `std::invalid_argument` for a local socket path that is too long (#1333)
  - **Breaking:** Throw `EncodingError` for a null function value of a type that cannot hold null (#1333)

- **Breaking:** Convert names with `UInt`, `ULong`, `SInt`, `FoV`, a plural acronym or digits
  next to an acronym as written, e.g. `Type::uint`, `Camera::fov` and `MapFilterType::evas` in
  place of `u_int`, `fo_v` and `ev_as` (#1339)
- Add a static `_call_template` function for each member, which builds a call with no instance
  or arguments, for `Expression::call_with_arguments` (#1352)
- Throw `std::invalid_argument` for a default-constructed object passed as an argument or called
  through, before the call is sent (#1377)

## [v0.7.0]

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `Stream::start` and `Event::start`
    (#1122)
  - **Breaking:** Remove `Client::freeze_streams` and `Client::thaw_streams`. Calls made while
    holding the tick with `KRPC::hold_tick` give values from one physics tick (#1118)
  - **Breaking:** Return a `krpc::Condition` from the `get_condition` methods and
    `Client::get_stream_update_condition`, locked with a `krpc::RecursiveMutex` (#1122)
  - Return `true` from `Stream::wait` and `Event::wait` on an update, and `false` on a timeout
    (#1122)
  - Wait for a stream's first value when reading it (#1122)
  - End a stream on an error from the server, and every stream with `krpc::ConnectionError`
    when the connection closes or fails (#1122)
  - Throw `krpc::StreamError` when reading or waiting on a removed stream or event, or on a
    client with no stream connection (#1122, #1109)
  - Add `add_end_callback` to streams and events, called with the exception that ends the
    stream (#1122)
  - Call a callback added to a stream or event straight away for a value or firing already
    received (#1122)
  - Fix `Event::wait` missing an earlier firing, and a thread waiting on a stream staying
    blocked when the stream is removed (#1122, #1090)
  - Fix the stream update thread spinning after the server closes the connection, and ending the
    process on a bad message or a throwing callback (#1122)
  - Fix data races between a stream update and reading the value or changing the callbacks
    (#1090, #1108)
  - Fix use of freed memory by streams and callbacks once the client, stream or event they came
    from is moved or destroyed (#1122)
  - Fix two copies of a stream sharing one condition lock, a removed stream returning its last
    value, and a second removal removing a newer stream (#1122, #1090)

- Server side functions
  - Add `krpc::run`, which runs a server side function within a single physics tick and returns
    its value (#1129)
  - Add `krpc::add_stream` and `krpc::add_event`, which stream a server side function and create
    an event from one (#1129)
  - Add `krpc::get_object_id`, which returns the id of a remote object for
    `Expression::constant_object` (#1160)

- Types
  - **Breaking:** Support null for any nullable type, held as `std::optional`, including a
    structure field, list element, tuple item and dictionary value (#1017, #1091)
  - Support structure types, generated as a `struct` with a constructor, equality, ordering and
    a `std::hash` over its fields (#1066)
  - Add `krpc::hash_value` and `krpc::hash`, which hash any type the client carries, so a tuple
    or a collection can key an unordered container (#1067)
  - Include the header of every service a service header uses, so service headers can be
    included in any order (#1150)
  - Fix a stream of an enumeration or structure, and a procedure with a collection of
    enumerations, failing to compile (#1094, #1044)

- Connections
  - **Breaking:** Send protocol version 1 when connecting, and fail to connect to a server from
    before 0.7 (#1162)
  - Add `krpc::connect_local`, which connects to a server on the same machine over a unix domain
    socket (#1065)
  - Add a `timeout` parameter to `krpc::connect`, bounding how long a connection is waited for
    (#1065)
  - Fix `krpc::Connection::close` failing to link (#1065)
  - Reduce the cost of a remote procedure call, most for one that carries a collection (#1056)
  - Update to protobuf v36.2 and ASIO 1.38.2 (#1139)

## [v0.6.0]
- Update to protobuf v35.1 (#850)
- Update to ASIO 1.38.0 (#850)
- Distribute via vcpkg (#874)
- **Breaking:** Drop autotools build; CMake is now the only supported build system (#851)
- **Breaking:** Modernise CMake build: require CMake 3.15+, C++17, target-based usage requirements (#834)
- Add CMake package config so consumers can use `find_package(krpc CONFIG REQUIRED)` (#851)
- CMake dependency options (`KRPC_FETCH_DEPS`, `KRPC_FETCH_PROTOBUF`, `KRPC_FETCH_ASIO`, `KRPC_FETCH_ABSL`):
  when OFF (default) the system install is required; when ON `FetchContent` is used. (#857)
- Mark deprecated members with the `[[deprecated]]` attribute (#904)
- An error from a service whose generated header is not included now throws an `RPCError`
  describing it (#1007)
- An exception thrown by a stream or event callback no longer escapes the stream update
  thread (#1007)
- Fix a data race on the exception thrower map between constructing a service and handling an
  error (#1007)
- Fix stream update notifications being lost, which could leave
  `Client::wait_for_stream_update` sleeping through the update it was waiting for (#974)
- Fix `Client::freeze_streams` blocking forever when no stream is producing updates (#974)

## [v0.5.2]
- Use protobuf-lite
- Roll back to protobuf v3.21.12 to avoid absl dependency

## [v0.5.0]
- Update to protobuf v3.22.0
- Update to ASIO 1.24.0
- Fix include path to ASIO header in CMake build scripts
- Fix CMake build scripts not building a static library with MSVC on Windows

## [v0.4.8]
- Update to protobuf v3.6.1
- Update to ASIO 1.12.1
- Add condition variable and callbacks that are called when a stream update message is processed (#473)
- Fix compilation issue on MacOS
- Fix cmake configuration detection of protoc binary

## [v0.4.7]
- Ensure .lib files are generated when building using CMake with MSVC (#474)

## [v0.4.6]
- Add methods to remove callbacks from streams and events (#451)
- Improve Autotools and CMake build scripts to regenerate the protobuf source if protoc is installed

## [v0.4.5]
- Update to protobuf v3.5.1

## [v0.4.4.post1]
- Add missing files to build scripts

## [v0.4.3]
- Add rate control for streams (#116, #141)

## [v0.4.0]
- Updated protocol in line with server changes
- Remove connection retries. Client will now fail fast if it fails to connect to the server.
- Add support for RPCs and streams to throw exceptions

## [v0.3.11]
- Update to protobuf v3.4.1

## [v0.3.10]
- Set `ASIO_STANDLONE` in build script instead of source code so that configure script can find the header correctly
- Add support for building using Cygwin
- Update to protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0

## [v0.3.8]
- Update to protobuf v3.2.0
- Update build scripts to check for protobuf >= 3.2 (#374)
- Move `krpc::connect` into client constructor to allow use of smart pointers
- Fix bug in `StreamManager` when client is not set

## [v0.3.7]
- Add missing header to automake script
- Add stream freezing (#357)
- Make stream objects default and copy constructible (#358)

## [v0.3.6]
- Fix configure script

## [v0.3.4]
- Update to protobuf v3.0.0-beta-3

## [v0.2.3]
- Make client thread safe
- Add checks for `asio.hpp` and protobuf library to cmake script
- Add Windows with MSVC support to cmake script
- Fix compiler warnings reported by MSVC

## [v0.2.2]
- Add support for streams (#175)
- Add documentation to service header files
- Add autotools and cmake build scripts
- Move header file `krpc/krpc.hpp` -> `krpc.hpp`
- Remove support for protobuf enumeration and custom protobuf messages
- Add comparison operations for remote objects

## [v0.2.1]
- Remove dependency on boost
- Use standalone ASIO library for network communication

## [v0.2.0]
- Update to protobuf 3.0.0-beta-2

## [v0.1.12]
- Initial version
