## [0.6.0] - unreleased
- Requires Python 3.10+
- Allow calling static class methods from a class instance (#832)
- Update to protobuf v7.35.1
- Emit a `DeprecationWarning` when calling a deprecated member, and note deprecation in docstrings (#904)
- Fix static class method calls being sent to the wrong connection when
  multiple clients are connected
- An exception raised by a stream or event callback no longer ends the stream update thread,
  which stopped every stream and event on the connection from updating again. It is reported
  through the thread excepthook and the remaining callbacks still run (#1008)
- Fix a deadlock between the stream update thread and a thread waiting for an update while
  holding a stream or event condition, as waiting for one requires (#1008)
- An error from a service this client does not know about now raises an `RPCError` describing
  it, instead of a `RuntimeError` about the exception that could not be built (#1008)
- Fix decoding of `sint64` values at or above `2**62`, which were returned with the wrong sign;
  `long.MaxValue` decoded as -1 and `long.MinValue` as 0. Encoding was unaffected (#1008)
- A closed connection is now reported instead of being mistaken for no data having arrived
  yet. Both loops reading a message size retried it at full speed indefinitely, so losing the
  server left the client spinning with streams frozen and no error raised, and on the RPC
  connection it did so holding the connection lock, blocking every other thread. Denying a
  connection in the in-game dialog left `krpc.connect` spinning rather than reporting it (#1008)
- Removing a stream, or closing the client, now wakes threads waiting for an update on it,
  which previously waited for an update that could never arrive (#1008)
- The client can now be closed from a stream or event callback, which raised `RuntimeError` and
  left the client half closed (#1008)
- Fix a stream returning `None` rather than its value, or the error saying it has none, when
  read as its first update was being stored (#1008)

## [0.5.4]
- Fix streams for services without pre-generated stubs (#774)

## [0.5.3]
- Fix assertion error when connecting to a server with third party services installed (#754)

## [0.5.2]
- Requires Python 3.7+
- Add type hints (#703)
- Pre-generated stubs now include implementation of services as well as type hints
- Fix various type hint bugs in generated stubs
- Allow importing types from a service using, for example "`from krpc.services.spacecenter import Vessel`"

## [0.5.0]
- Fix protobuf requirement to be >=3.6 (#506, #510)
- Update to protobuf v4.22.0

## [0.4.8]
- Update to protobuf v3.6.1
- Add condition variable and callbacks that are called when a stream update message is processed (#473)

## [0.4.6]
- Add methods to remove callbacks from streams and events (#451)

## [0.4.5]
- Update to protobuf v3.5.1

## [0.4.3]
- Add rate control for streams (#116, #141)

## [0.4.0]
- Updated protocol in line with server changes
- Remove connection retries. Client will now fail fast if it fails to connect to the server
- Reorder parameters to `krpc.connect()` so that name is first - to be consistent with other client libraries
- Add support for RPCs and streams to throw exceptions
- Add stream waiting and update callbacks
- Add support for events
- Don't execute an initial RPC when a stream is created, wait for the first update instead

## [0.3.10]
- Update to protobuf v3.4.0

## [0.3.9.post1]
- Fix compatibility with protobuf v3.4.0

## [0.3.9]
- Update to protobuf v3.3.0

## [0.3.8.post1]
- Rename protobuf generated files to `*_pb2.py` to fix issue with protobuf 3.2.0 (#378, #376)
- Relax package requirements to protobuf >= 3

## [0.3.8]
- Clean up code to meet PEP 8 guidelines
- Update to protobuf v3.2.0
- Change package requirements to protobuf >= 3.2

## [0.3.7]
- Fix bug parsing nested collection types

## [0.3.6]
- Fix values not being documented in generated enumeration classes

## [0.3.5]
- Add check for number of elements in a tuple before invoking an RPC (#276)
- Fix unicode issue (#284)

## [0.3.4]
- Update protobuf to v3.0.0-beta-3

## [0.2.2]
- Fix exception when stream thread shuts down (#197)
- Remove support for protobuf enumeration and custom protobuf messages
- Add comparison methods to remote objects so that they are sortable

## [0.2.1]
- Fix bug with `setup.py` on Windows
- Add version number to python module

## [0.2.0]
- Update protobuf 3.0.0-beta-2
- Fix bug in keyword arg handling (#168)
- Removed `TestServer.exe` and associated binaries from release archive

## [0.1.12]
- Server connection method now retries 10 times every 0.1 seconds

## [0.1.11]
- Docstrings generated from documentation returned by `KRPC.GetServices` (#31)

## [0.1.10]
- Bump version number

## [0.1.9]
- Bump version number

## [0.1.8]
- Improved dynamic creation of service methods
- Support for static class methods (#106)
- Improve enums: return an `Enum` object instead of an `int`
- Fix bug with types across multiple connections (#110)

## [0.1.7]
- Support for Python 3
- Upgrade to Protocol Buffers 3.0.0-alpha-1
- Checking of address and port parameters before connecting
- Connecting to the stream server is now optional
- Improve detection of protobuf message and enum types and improve support for 3rd party types (#38)
- Fix unicode decoding/encoding bugs (#104)

## [0.1.6]
- None, bumped version to match server version

## [0.1.5]
- Add `Client.close()`

## [0.1.4]
- Improved network code to fix bugs and make it more robust
- Add python version checks
- Make connections thread safe

## [0.1.3]
- Fix bug with encoding/decoding of infinities and NaNs

## [0.1.2]
- Convert parameter names to snake_case

## [0.1.1]
- Update example script

## [0.1.0]
- Initial pre-release
