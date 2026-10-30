## [v0.8.0] - unreleased

- Connections
  - **Breaking:** Connect over one connection, removing the `stream_port` and `stream_path`
    parameters of `connect` and `connect_local`. A server from before 0.8 refuses the
    connection (#1219)
  - **Breaking:** Rename the `rpc_port` and `rpc_path` parameters of `connect` and
    `connect_local` to `port` and `path` (#1219)
  - **Breaking:** Give up after 5 seconds if the server does not accept the connection. Pass
    `handshake_timeout=None` to wait for the player to approve it (#1219)
  - Add `handshake_timeout` and `stall_timeout` parameters to `connect` and `connect_local`
    (#1219)
  - Send calls from several threads without waiting for each other's responses (#1219)
  - Fix later calls reading the wrong response after a call is interrupted by an exception
    (#1219)
  - Tell the server when the connection closes (#1219)
  - Add `status` and `reason` to `ConnectionError` for a refused or ended connection (#1219)
  - **Breaking:** Raise `ConnectionError` when the server cannot be reached, with the
    `OSError` as its cause (#1219)

- Streams and events
  - **Breaking:** Raise `ValueError` from `Stream.wait` and `Event.wait` for a negative or NaN
    timeout, and wait with no deadline for an infinite one (#1219)
  - Keep a stream running when a value fails to decode, dropping the value (#1219)
  - Fix `Stream.wait` and `Event.wait` returning before the timeout on Windows (#1220)

- Link the references in service docstrings with Sphinx roles (#1185)
- Fix the lists and parameter references in service docstrings, which were invalid
  reStructuredText (#1185)
- Write the units and formulas in service documentation as Unicode text in docstrings (#1169)
- Fix a `SyntaxWarning` for an invalid escape sequence on importing the `SpaceCenter` stubs (#1169)

- Exceptions
  - Raise a `KRPC` exception as its own class, derived from the builtin it was raised as
    before, so `except conn.krpc.ArgumentException` catches it (#1172)

## [v0.7.0]

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `Stream.start` and `Event.start`
    (#1122)
  - Return `True` from `Stream.wait` and `Event.wait` on an update, and `False` on a timeout
    (#1122)
  - End a stream on an error from the server, and every stream with `ConnectionError` when the
    connection closes or fails (#1122, #315)
  - Raise `StreamError` when reading or waiting on a removed stream or event (#1122)
  - Add `add_end_callback` to streams and events, called with the exception that ends the
    stream (#1122)
  - Call a callback added to a stream or event straight away for a value or firing already
    received (#1122)
  - Fix `Event.wait` missing an earlier firing, and waking only one of two waiting threads
    (#1122)
  - Fix the stream update thread stopping on a value it cannot decode (#1122, #198)
  - Fix a second removal of a stream removing a newer stream for the same call, and an error
    ending an event calling its callbacks (#1122)

- Server side functions
  - Add `Client.compile`, which compiles a Python function or lambda into a server side
    function (#1127)
  - Compile operators, comprehensions, f-strings, slices, `print`, a subset of the builtins and
    the `math` module, and the statements of a function body (#1127)
  - Compile a captured `nonlocal` variable to a state variable, which keeps its value between
    runs (#1127)
  - Raise `FunctionCompilationError` naming an unsupported construct (#1127)
  - Add `Client.run` and `Client.add_event`, and stream a function with `Client.add_stream` and
    `Client.stream` (#1127)
  - Add `krpc.get_object_id`, which returns the id of a remote object for
    `Expression.constant_object` (#1160)

- Types and errors
  - **Breaking:** Raise the matching built-in exception, such as `TypeError`, `KeyError`,
    `IndexError` or `TimeoutError`, for an error reported by any service, in place of
    `RPCError` (#1124)
  - **Breaking:** Support `None` for any nullable type, including a structure field, list
    element, tuple item and dictionary value (#1017, #1091)
  - **Breaking:** Rename the `LiDAR` service class to `LIDAR`, and `conn.li_dar` to
    `conn.lidar` in a client without stubs (#1149)
  - Support structure types as named tuples, accepting a tuple or list with one element per
    field (#1066)
  - Skip a service member naming an unknown type with a warning, and connect (#1066)
  - Add the class members only the server declares to a service with pre-generated stubs
    (#1077)
  - Escape an enumeration member named after a keyword in a dynamically created service, such
    as `Class` to `class_`, as the stubs do (#1094)
  - Add `krpc.limits`, the extremes of the numeric types kRPC carries, such as
    `krpc.limits.SINT32_MAX` (#1045)
  - Fix a service failing to load when a default value names an enumeration of a service not
    yet loaded (#1044)
  - Fix type checkers skipping code after a name the service defines, typing a stream value as
    `object`, and rejecting `stream_port=None` (#1134)

- Connections
  - **Breaking:** Send protocol version 1 when connecting, and fail to connect to a server from
    before 0.7 (#1162)
  - Add `krpc.connect_local`, which connects to a server on the same machine over a unix domain
    socket (#1065)
  - Add a `timeout` parameter to `krpc.connect`, bounding how long a connection is waited for
    (#1065)
  - Reduce the cost of a remote procedure call (#1056)
  - Require protobuf 7.36.2 or later (#1139)

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
