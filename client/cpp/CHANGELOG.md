## [v0.8.0] - unreleased

- Fix the documentation of the services that Doxygen warns about, such as parameter names
  that do not match (#1185)
- Exceptions
  - **Breaking:** Derive the `KRPC` exceptions with a standard equivalent from it, e.g.
    `ArgumentException` from `std::invalid_argument`, in place of `RPCError` (#1172)
  - **Breaking:** Add the server stack trace to an exception's message (#1172)

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
