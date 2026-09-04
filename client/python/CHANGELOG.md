## [v0.7.0] - unreleased
- A dynamically created service escapes an enumeration member whose name is a python keyword,
  such as `Class` to `class_`, matching the pre-generated stubs (#1094)
- Support a nullable structure field, list element, tuple item and dictionary value, which is
  `None` when absent (#1091)
- A service with pre-generated stubs also gains the class members that only the server
  declares (#1077)
- Support structure types. A value is a named tuple, and a tuple or list with one element per
  field is accepted where one is expected (#1066)
- A service definition naming an unknown type skips the member that names it with a warning,
  instead of failing to connect (#1066)
- **Breaking:** Support `None` for any nullable type (#1017)
- Fix a service failing to load when one of its procedures defaults to a member of an
  enumeration from a service that had not been loaded yet (#1044)
- Add `krpc.limits`, naming the extremes of the numeric types kRPC carries over the wire, such
  as `krpc.limits.SINT32_MAX` (#1045)
- Reduce the cost of a remote procedure call, caching type objects, dispatching on the type
  code, and reading a response in one system call (#1056)
- Add `krpc.connect_local`, which connects to a server on the same machine over a unix domain
  socket (#1065)
- Add a `timeout` parameter to `krpc.connect`, bounding how long a connection is waited for
  (#1065)

## [v0.6.0]
- **Breaking:** Requires Python 3.10+ (#837)
- Update to protobuf v7.35.1 (#850)
- Allow calling static class methods from a class instance (#832)
- Emit a `DeprecationWarning` when calling a deprecated member, and note deprecation in docstrings (#904)
- Fix static class method calls being sent to the wrong connection when
  multiple clients are connected (#979)
- An exception raised by a stream or event callback no longer ends the stream update thread (#1008)
- Fix a deadlock between the stream update thread and a thread waiting for an update (#1008)
- An error from a service this client does not know about now raises an `RPCError` describing it (#1008)
- Fix decoding of `sint64` values at or above `2**62` (#1008)
- A closed connection is now reported instead of being mistaken for no data having arrived (#1008)
- Removing a stream, or closing the client, now wakes threads waiting for an update on it,
  which previously waited for an update that could never arrive (#1008)
- The client can now be closed from a stream or event callback, which raised `RuntimeError` and
  left the client half closed (#1008)
- Fix a stream returning `None` rather than its value, or the error saying it has none, when
  read as its first update was being stored (#1008)

## [v0.5.4]
- Fix streams for services without pre-generated stubs (#774)

## [v0.5.3]
- Fix assertion error when connecting to a server with third party services installed (#754)

## [v0.5.2]
- Requires Python 3.7+
- Add type hints (#703)
- Pre-generated stubs now include implementation of services as well as type hints
- Fix various type hint bugs in generated stubs
- Allow importing types from a service using, for example "`from krpc.services.spacecenter import Vessel`"

## [v0.5.0]
- Fix protobuf requirement to be >=3.6 (#506, #510)
- Update to protobuf v4.22.0

## [v0.4.8]
- Update to protobuf v3.6.1
- Add condition variable and callbacks that are called when a stream update message is processed (#473)

## [v0.4.6]
- Add methods to remove callbacks from streams and events (#451)

## [v0.4.5]
- Update to protobuf v3.5.1

## [v0.4.3]
- Add rate control for streams (#116, #141)

## [v0.4.0]
- Updated protocol in line with server changes
- Remove connection retries. Client will now fail fast if it fails to connect to the server
- Reorder parameters to `krpc.connect()` so that name is first - to be consistent with other client libraries
- Add support for RPCs and streams to throw exceptions
- Add stream waiting and update callbacks
- Add support for events
- Don't execute an initial RPC when a stream is created, wait for the first update instead

## [v0.3.10]
- Update to protobuf v3.4.0

## [v0.3.9.post1]
- Fix compatibility with protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0

## [v0.3.8.post1]
- Rename protobuf generated files to `*_pb2.py` to fix issue with protobuf 3.2.0 (#378, #376)
- Relax package requirements to protobuf >= 3

## [v0.3.8]
- Clean up code to meet PEP 8 guidelines
- Update to protobuf v3.2.0
- Change package requirements to protobuf >= 3.2

## [v0.3.7]
- Fix bug parsing nested collection types

## [v0.3.6]
- Fix values not being documented in generated enumeration classes

## [v0.3.5]
- Add check for number of elements in a tuple before invoking an RPC (#276)
- Fix unicode issue (#284)

## [v0.3.4]
- Update protobuf to v3.0.0-beta-3

## [v0.2.2]
- Fix exception when stream thread shuts down (#197)
- Remove support for protobuf enumeration and custom protobuf messages
- Add comparison methods to remote objects so that they are sortable

## [v0.2.1]
- Fix bug with `setup.py` on Windows
- Add version number to python module

## [v0.2.0]
- Update protobuf 3.0.0-beta-2
- Fix bug in keyword arg handling (#168)
- Removed `TestServer.exe` and associated binaries from release archive

## [v0.1.12]
- Server connection method now retries 10 times every 0.1 seconds

## [v0.1.11]
- Docstrings generated from documentation returned by `KRPC.GetServices` (#31)

## [v0.1.10]
- Bump version number

## [v0.1.9]
- Bump version number

## [v0.1.8]
- Improved dynamic creation of service methods
- Support for static class methods (#106)
- Improve enums: return an `Enum` object instead of an `int`
- Fix bug with types across multiple connections (#110)

## [v0.1.7]
- Support for Python 3
- Upgrade to Protocol Buffers 3.0.0-alpha-1
- Checking of address and port parameters before connecting
- Connecting to the stream server is now optional
- Improve detection of protobuf message and enum types and improve support for 3rd party types (#38)
- Fix unicode decoding/encoding bugs (#104)

## [v0.1.6]
- None, bumped version to match server version

## [v0.1.5]
- Add `Client.close()`

## [v0.1.4]
- Improved network code to fix bugs and make it more robust
- Add python version checks
- Make connections thread safe

## [v0.1.3]
- Fix bug with encoding/decoding of infinities and NaNs

## [v0.1.2]
- Convert parameter names to snake_case

## [v0.1.1]
- Update example script

## [v0.1.0]
- Initial pre-release
