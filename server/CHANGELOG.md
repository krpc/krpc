## [v0.8.0]

- Servers and settings
  - **Breaking:** Listen on one port or socket path per server, removing the stream port and
    stream path settings (#1219)
  - **Breaking:** Reset each existing server's settings to their defaults, so a TCP/IP and a
    WebSockets server both use port 50000 until one is changed (#1219)
  - **Breaking:** Turn on "Auto-accept new clients" for new installs. An existing settings file
    keeps its value (#1219)
  - Start a new install with a TCP/IP server on port 50000 and a WebSockets server on port
    50001, each named for its protocol (#1349, #1371)
  - Add *JSON over HTTP* to the protocols, on port 50003 by default, and the origins whose web
    pages may connect to WebSockets and HTTP servers (#1391, #1392)
  - Add RTS/CTS flow control to serial IO servers (#1386)
  - Keep the server settings on an upgrade, as the release no longer includes `settings.cfg`
    (#1349)
  - Keep each readable setting from a damaged `settings.cfg`, and back up a file with no
    readable settings as `settings.cfg.bak` (#1349)
  - Build the serial port code into `KRPC.Core.dll`, in place of a separate
    `KRPC.IO.Ports.dll` (#1386)

- Performance
  - **Breaking:** Replace "Max. time per update" with "Max. time per frame", 10 ms by default,
    and "Blocking receives" and "Receive timeout" with "Max. call wait", 5 ms by default (#1376)
  - Hold the frame rate at the new "Target frame rate" setting with adaptive rate control,
    60 fps by default (#1376)
  - Add presets to the advanced settings: "Game first", "Balanced" and "Calls first" (#1376)

- Server windows
  - Show the number of connected clients beside each server, and remember which servers are
    expanded (#1385)
  - Name a server added in the window "Server", "Server 2" and so on, and default the port of a
    WebSockets server to 50001 (#1371, #1385)
  - Name the TCP/IP protocol "Protobuf over TCP/IP" (#1225)
  - Reorganize the info window around the frame, with deferred calls, stream updates and a
    "Limited by" row naming what holds down the call rate (#1244, #1376)
  - Widen the server window so the advanced settings fit, and line up their labels (#1349)
  - Give the kRPC windows an opaque background, and hide them while the game hides its UI
    (#1349, #1376)
  - Fix the server window keeping its height after clients disconnect (#1186)
  - Fix "Clear statistics" leaving the data read and written (#1376)

- Mods and service errors
  - Keep running when a service has errors, and leave out only the members with them and the
    members that use them (#1345)
  - Load each supported mod whatever its version, and fail only the calls that use a missing
    member (#1341)
  - Disable only the integration of a mod that fails to load, and give the reason in the errors
    of its RPCs (#1341, #1342)
  - List the members left out, the RPCs that use them, and the mods whose support failed to
    load, in a window at the main menu (#1345)

- Logging
  - Log a connection the player is asked to allow once, naming the client, and an auto-accepted
    connection at the debug level (#1235)
  - Log auto-starting the servers only when one is stopped (#1235)
  - Fix the game scene in log lines reading `Inherit, Flight` (#1235)
  - Fix errors logged on every scene change, and on quitting, when the server fails its startup
    checks (#1376)
  - Log each request at the debug level as one line naming its calls and their errors, and stop
    logging each stream update (#1219)
  - Log the transport connection lines and object store sweeps at the debug level (#1235)
  - Log the address and settings of a server when it starts (#1235)
  - Name a client in log lines by its address and the start of its identifier, and leave out
    the time, which the game log adds (#1219, #1235)

- Protocol
  - **Breaking:** Carry each client on one connection, as protocol version 2, where every
    message is a `Message`. Clients from earlier releases cannot connect (#1219)
  - Add an id to each request, which the server copies onto its response (#1219)
  - End a connection with a `Disconnect` message giving the reason, and refuse one the player
    denies with the status `DENIED` (#1219)
  - Start a new session on a `ConnectionRequest` sent during a session, on every transport
    (#1219)
  - **Breaking:** Encode a stream or an event as the `uint64` id of its stream, and remove the
    `Stream` and `Event` messages (#1266)
  - Add `internal` to the `Procedure` message, set for the stream, event and server side
    function procedures of the `KRPC` service (#1269)
  - **Breaking:** Rename `name` in the `Error` message to `exception`, and add `service_id` and
    `exception_id` (#1324)
  - Report the .NET exception each kRPC exception stands for, as `mapped_type` in
    `KRPC.Exception` (#1172)

- Procedure and type ids
  - **Breaking:** Derive each procedure id from a hash of its name, which changes every id (#1265)
  - Give each class, enumeration, structure and exception an id, a hash of its name (#1324)
  - Add `id` to the `Service`, `Procedure`, `Class`, `Enumeration`, `Struct` and `Exception`
    messages, and `service_id` and `id` to the `Type` message (#1265, #1324)

- Calls and arguments
  - **Breaking:** Reject an argument with bytes left over, an unknown field, a 32-bit number out
    of range, a tuple with the wrong number of items, an undeclared enum value, or a bool other
    than 0 or 1 (#1280)
  - **Breaking:** Fail a call with an argument position past the procedure's last parameter, or
    with two arguments at one position (#1290)
  - Fail only the call whose argument the server cannot decode, and run the other calls in the
    request (#1280)

- Connections and performance
  - Wait for a client's next call by its measured time between calls, so a client that pauses
    briefly between calls gets several calls per frame (#1376)
  - Read and write each connection on threads of its own, so a client that stops reading cannot
    freeze the game (#1376)
  - Read up to 16 requests ahead of the one running, and end the connection of a client that
    leaves more than 16 MB of messages unread (#1376)
  - Read from a serial port as bytes arrive, and accept the stream RPCs over serial IO (#1219,
    #1376)
  - End a connection whose message stalls partway, after 3 seconds over serial IO and 30 seconds
    otherwise (#1219)
  - Move the default local socket to `krpc.sock` in `XDG_RUNTIME_DIR` or `LOCALAPPDATA`, or to
    `/tmp/krpc-<user>.sock` (#1219)
  - Start the remaining servers when one fails to start (#1330)
  - Load the services faster in games with many mods (#1353)
  - Fix a serial IO server stopping when it sends a response over a megabyte (#1386)
  - Fix a WebSockets connection with a frame too large to buffer throwing
    `OverflowException` (#1193)
  - Fix RPCs stopping on .NET after a client disconnects during a yielding call (#1189)

- HTTP and WebSockets
  - Add a JSON over HTTP server, with an OpenAPI document, for calling procedures with no client
    library (#1391)
  - **Breaking:** Refuse WebSockets connections from web pages whose origin is not allowed, by
    default any page not served from this machine (#1392)

- Streams and events
  - **Breaking:** Fire a `KRPC.AddEvent` event once each time its function becomes true (#1234)
  - Update a stream when its value changes between 0.0 and -0.0, or a collection it returns
    changes in place (#1199)
  - Run a stream of a call or server side function with no value on each update, for its
    effects (#1198)
  - Fix rate limited streams updating at as little as half their rate (#1310)

- Server side functions
  - Add the `KRPC.Expression.Match` and `KRPC.Expression.Switch` statements (#1334)
  - Combine flags enumeration values with `KRPC.Expression.And`, `Or`, `ExclusiveOr` and `Not`
    (#1311, #1313)
  - Add variants of the procedures that take ids in place of names, such as
    `KRPC.Type.ClassTypeById` and `KRPC.Expression.GetFieldByIndex` (#1336)
  - **Breaking:** `KRPC.Expression.CallWithArguments` takes a call template, and throws an
    exception for a call that carries arguments (#1352)
  - Fail when a function is built, in place of when it runs, for a null constant passed to a
    parameter that cannot be null (#1352)

- Server status and settings
  - **Breaking:** Return `KRPC.GetStatus` as the struct `KRPC.ServerStatus`, with the settings
    and statistics in nested structs, and remove the `Status` message (#1244)
  - **Breaking:** Replace the statistics per update with statistics per frame, adding
    `FrameRate`, `BudgetPerFrame` and `MedianTurnaround` (#1376)
  - **Breaking:** Rename the stream counters to `Streams`, `StreamEvaluations` and
    `StreamEvaluationRate`, and add `TickHoldTimeout`, `DeferredCalls` and `StreamUpdatesSent`
    (#1244)
  - **Breaking:** Replace the settings `MaxTimePerUpdate`, `BlockingRecv` and `RecvTimeout` with
    `TargetFrameRate`, `MaxTimePerFrame` and `MaxCallWait` (#1376)
  - **Breaking:** Remove the "One RPC per update" setting and
    `KRPC.ServerSettings.OneRpcPerUpdate` (#1349)
  - Turn on "Auto-start server" by default (#1349)

- Writing services
  - Support flags enumerations, a `KRPCEnum` with `[Flags]` whose values a client can combine,
    marked by `flags` in the `Enumeration` message (#1311)
  - Add `Id` to the attributes, with `GetterId` and `SetterId` on `KRPCProperty`, to resolve an
    id clash, and `KRPCProcedureId` for a procedure a class inherits (#1265, #1324)
  - Add `ModAPI`, `ModBinder`, `MemberBinder` and the `RequiresMod` attribute, for a service
    that binds to another mod by name (#1341, #1342, #1382)
  - Add `StdLib.ImageFormat`, the format of the images from the camera services (#1341)
  - Allow a `<see cref>` in service documentation to link to an exception, or a member of a
    service with a custom name (#1185, #1188)
  - **Breaking:** Reject two services, two types of a service, or two members, values, fields or
    parameters that share a name, ignoring case (#1324, #1325, #1338)
  - **Breaking:** Reject a type named after its service, a method `GetFoo` or `SetFoo` beside a
    property `Foo`, or a parameter or value name that is not ASCII letters and digits (#1325,
    #1338)
  - **Breaking:** Reject a service declared by more than one class, or a parameter defaulting to
    a value its enumeration does not declare (#1192, #1325)
  - **Breaking:** Reject a stream or an event outside a return value, or a stream returned by a
    procedure that is not internal (#1269)

## [v0.7.0]
- **Breaking:** KSP 1.12.5 is the minimum supported version, matching the maximum (#1048)
- Add the local socket protocol to the server window. It opens no network port (#1065)
- Add the `TickHoldTimeout` setting, which bounds how long a client may hold the game on one
  physics tick (#1070)
- Update the server before the game collects control inputs, so a control input takes effect on
  the tick of its call (#1070)
- Stop the server whenever no game is loaded, disconnecting clients when the player quits to the
  main menu (#1041)
- Save the game when setting `KRPC.GameScene` leaves flight, unless the game cannot be saved
  (#1117)
- Remove objects whose game objects are gone from the object store when the game loads, changes
  scene or destroys a part or vessel (#1051)
- Fix setting `KRPC.GameScene` from one editor to the other, or back to flight, breaking the
  flight camera or failing with a null reference (#1038, #1117)
- Fix the error from a call that waits on the game being reported as `Exception has been thrown
  by the target of an invocation` (#1083)
- Fix the end of a long serial port name being hidden in the server window (#1065)

- Server side functions
  - Extend `KRPC.Expression` to build server side functions, which the server evaluates
    (#503, #517, #521, #1124)
  - Allow a function to declare local variables, loop, branch, assign, return early, raise and
    handle exceptions, and call a procedure that returns no value (#1124)
  - Allow a function to work on strings, nullable values, and lists, sets and dictionaries,
    including changing them (#1124)
  - Add `KRPC.RunFunction`, which runs a function once within a single physics tick, and
    `KRPC.CompileFunction` and `KRPC.AddFunctionStream` (#1124)
  - Add `KRPC.Expression.StateVariable`, a variable that keeps its value between evaluations of
    a function (#1124)
  - Add `KRPC.Expression.CallWithArguments`, `CreateStruct`, `GetField`, `IsNull`, `Negate`,
    `Conditional`, `ConditionalAnd`, `ConditionalOr`, the empty collections and a constant for
    every type (#1124)
  - Add `KRPC.Expression.ReturnType` and `HasReturnType` (#1124)
  - Add `KRPC.TypeCode` and the `Code`, `Service`, `Name`, `Types` and `Nullable` properties of
    `KRPC.Type`, which names every type a procedure takes or returns (#1124)
  - Add the `StdLib` service, of scalar mathematics and vector and quaternion operations, with
    `StdLib.Print` for debugging a function (#1125)
  - Widen numbers, tuples and collections to a common type in `KRPC.Expression.Invoke`, `Cast`,
    `CreateList`, `CreateSet`, `CreateDictionary` and `Concat` (#1124)
  - Fix `KRPC.Expression.Count` on a returned collection, and `OrderBy` ordering strings by the
    system locale (#1124)
  - Fix `KRPC.Expression.Equal`, `NotEqual`, `Contains`, `ToSet` and `CreateSet` comparing
    objects, tuples, bytes and collections by reference (#1124)
  - Fix the integer arithmetic of `KRPC.Expression` for mixed operand types, on overflow and for
    the smallest integer divided by -1 (#1124)
  - **Breaking:** Rename `KRPC.Expression.Function` to `Lambda`, and the parameter of
    `KRPC.AddEvent` from `expression` to `function` (#1124)
  - **Breaking:** Renumber the procedures of the `KRPC` service, so a client that calls them by
    id must use stubs generated for this version (#1124)
  - **Breaking:** Type `KRPC.Expression.Power` by both operands, require a constant index in
    `Get`, limit `CreateTuple` to seven elements, and reject calls to `KRPC` procedures (#1124)

- Errors
  - Add `KRPC.NullReferenceException`, `KeyNotFoundException`, `IndexOutOfRangeException`,
    `DivideByZeroException`, `NotSupportedException`, `TimeoutException` and
    `OverflowException`, raised by any service and by a function (#1124)
  - Add `KRPC.Error`, a general error a function can raise (#1124)
  - Add `KRPC.ObjectDestroyedException`, for an object whose game object is gone, and
    `KRPC.ObjectNotLoadedException`, for one that is not loaded (#1051, #1120)
  - **Breaking:** Throw `KRPC.UnsupportedSceneException` for a procedure called outside its
    game scenes (#1119)

- Streams and events
  - **Breaking:** Start a stream when it is added, removing `KRPC.StartStream` and the `start`
    argument of `KRPC.AddStream` (#1122)
  - **Breaking:** Create a new stream for every `KRPC.AddStream`, so removing one leaves the
    others for the same call (#1122, #902)
  - Hold a stream or event over an unloaded object, or a procedure unavailable in the game
    scene, until it is available (#1122, #877)
  - Add `KRPC.StreamRemovedException`, the final result of a stream the server ends (#1122)
  - Throw an exception when streaming a procedure that returns a stream or event, and from
    `KRPC.RemoveStream` for an unknown stream id (#1122)
  - Fix a failing stream or event stopping the others, and the server keeping memory for every
    stream sent on a connection (#1122)

- Types and protocol
  - **Breaking:** Support null for any nullable type, signaled by an `is_null` field. Class-typed
    arguments are not implicitly nullable (#1017)
  - **Breaking:** Carry nullability in `Type.nullable` at every position, replacing
    `Parameter.nullable` and `Procedure.return_is_nullable` (#1091)
  - **Breaking:** Return `KRPC.Clients` as a list of `KRPC.ClientInfo` structures (#1148)
  - Allow a structure field, list element, tuple item and dictionary value to be nullable,
    declared with `Nullable<T>` or `KRPCNullable` (#1091)
  - Add structure types, a compound value with named fields, declared with `KRPCStruct` on a C#
    `struct` (#1066)
  - Add `StdLib.ColorRGB` and `StdLib.ColorRGBA`, the colors of the SpaceCenter, Drawing and UI
    services (#1148)
  - Add extension members, which a service declares on another service's class as C# extension
    methods (#1077)
  - Add a communication protocol over a unix domain socket, for clients on the same machine as
    the game (#1065)
  - Fix a serial connection freezing the game while it carries an RPC (#1035)
  - Add a protocol version to the connection handshake, sent over WebSockets as the subprotocol
    `krpc.N` and reported by `KRPC.GetStatus` (#1162)
  - Fix an exception on a serial connection request that carries no `ConnectionRequest` (#1162)
  - Encode and decode values without reflection, cutting the server cost of a call that passes
    or returns an object by about three quarters (#1091)

- Game state
  - **Breaking:** Remove `KRPC.CurrentGameScene`, replaced by `KRPC.GameScene` (#1140)
  - Add `KRPC.HoldTick`, `KRPC.ReleaseTick` and `KRPC.NextTick`, which hold the game on one
    physics tick so a control loop can read, compute and write within it (#1070)
  - Add `KRPC.GameStateLoads`, the number of game states that have finished loading (#1121)
  - Remove objects whose game objects are gone from the object store when a game state loads
    (#1051)
  - Fix one server stopping emptying the object store that every server shares (#1020)
  - Fix a request naming an object the server no longer has leaving the connection stuck
    (#1019)

## [v0.6.0]
- Fix server version reported to clients (read from `KRPC.dll` not `KRPC.Core.dll`) (#848)
- Fix Logger to write to both `KSP.log` and `Player.log` (#780)
- Improve RPC and stream update performance (#879)
- Fix Stream RPCs Executed and Stream RPC Rate always showing 0 in the info window (#879)
- Improve the server window UI (#919)
- Fix incorrect unit shown for "max. time per update" and "receive timeout" (#919)
- Support switching the game scene by setting `KRPC.GameScene` (#897)
- Fix repeated toolbar icons appearing in some situations (#939)
- Add `Version` property to `Core`, set by the server plugin on startup (#848)
- Enable `TCP_NODELAY` on client TCP connections, reducing RPC round-trip latency (#879)
- Surface deprecated members (annotated with `[Obsolete]`) in the service definition and over the wire (#904)
- Add `KRPC.GameScene` and make it settable to switch the current game scene (#897)
- **Deprecated:** `KRPC.CurrentGameScene`, kept as a read-only alias of `KRPC.GameScene` (#897)
- Add `AstronautComplex`, `MissionControl`, `ResearchAndDevelopment`, `Administration` and `MissionBuilder` game scenes (#897)
- Fix the game scenes a procedure is available in never being sent by `GetServices`; the
  `game_scenes` field of every procedure was always empty (#991)
- Reduce copying and allocation overhead receiving protobuf messages (#972)
- Fix the websocket servers rejecting connection requests that are split across multiple reads (#973)
- Fix websocket connection URL query parameter parsing (#973)
- Fix locale issues with type codes in the service description, and HTTP and websocket protocol tokens (#993)

## [v0.5.4]
- Fix memory leaks when switching game scene (#779)

## [v0.5.2]
- Update to protobuf v3.10.1
- Fix game lagging when first client connects (#712)
- Change `KRPCDefaultValueAttribute` so that it applies to parameters directly (#677)

## [v0.5.0]
- Support for KSP 1.8+
- Update to protobuf v3.9.1
- Update to ModuleManager 4.2.2
- Fix bug with streams where a stream update is not sent initially after a stream is created, if the stream value is null (#515)

## [v0.4.8]
- Support for KSP 1.5.1
- Update to protobuf v3.6.1
- Add support for specifying more granular game scenes for RPCs (#471)

## [v0.4.7]
- Support for KSP 1.4.4
- Fix launch site clear checks (#483)

## [v0.4.6]
- Support for KSP 1.4.3
- Remove `CompatibilityChecker` as it's deprecated - instead rely on CKAN and KSP-AVC version checks
- Update icons to be 64x64 to fix blurring when loading as textures (#453)
- Fix equality checking for streams of collections (#452)
- Fix RPC errors being reported in `Response.error` instead of individual `ProcedureResult.error` fields (#467)

## [v0.4.5]
- Support KSP 1.4.1
- Update to protobuf v3.5.1
- Fix server error and client hang when quickly removing and re-adding a stream

## [v0.4.4]
- Add support for more complex server side expressions including function definitions and calls, and operations on collections (#435)
- Fix service ids to not clash (affected cnano client) (#433)

## [v0.4.3]
- Add compatibility for KSP 1.2.2 and 1.3.0
- Add rate control for streams (#116, #141)
- Add ability to call an RPC using an integer identifier for the service and procedure, rather than its full service and procedure name (reduces code size and communication overhead for cnano client)

## [v0.4.2]
- Include missing `KRPC.IO.Ports` DLL

## [v0.4.1]
- Add SerialIO server
- Fix connection timeout when client connects with auto-accept connections disabled (#425)
- Add option to pause the server when the game is paused
- New connections time out after 3 seconds if a connection request message is not received (#428)
- Fix issue with partial receipt of connection request messsages

## [v0.4.0]
- New server architecture that allows multiple concurrent servers, using different settings and protocols
- Add websockets server protocol for communication with browsers
- Improved communication protocol (#325)
  - Multiple RPCs can now be grouped into a single network message
  - Remove unnecessary message fields
  - Improved message encoding when establishing a connection
  - Significantly reduced the size of the message returned by GetServices
  - More compact encoding of signed integers
- Added ability for RPCs to throw custom exception types in the calling client
- Add start flag to `KRPC.AddStream` and add `KRPC.StartStream`. Allows streams to be added but not started until a later time
- If a stream returns an error, it is removed from the server automatically (#269)
- Add support for streaming RPCs that yield execution to a future game update
- Add `KRPC.GetClientID` and `KRPC.GetClientName`
- Add support for events
- Server no longer freezes if the game is paused (#347)
- Added `KRPC.Paused` property to get/set whether the game is paused (#422)

## [v0.3.11]
- Support for KSP 1.3.1

## [v0.3.10]
- Update to protobuf v3.4.0

## [v0.3.9]
- Update to protobuf v3.3.0
- KSP 1.3 support
- Fix client activity indicators in UI

## [v0.3.8]
- Update to protobuf v3.2.0

## [v0.3.7]
- Support for KSP 1.2.2
- Add `KRPCDefaultValue` attribute. Can be used to set default arguments to non-compile time constants.
- Update to Protocol Buffers v3.1.0

## [v0.3.6]
- Add `KRPC.Clients` that clients can use to get a list of connected clients (#304)
- Make `KRPC.Core.Instance` public so that other mods can subscribe to server events and access other server APIs (#304)
- Add "Yes (don't show again)" options to dialogs, add option to skip confirmation when force disconnecting a client

## [v0.3.5]
- Support for KSP 1.1.3
- Fix issue with uncaught exceptions when receiving malformed tuples from clients (#276)
- Fix issue causing empty stream update messages to be sent to clients

## [v0.3.4]
- Update protobuf to v3.0.0-beta-3 (#270)
- Move `settings.cfg` to PluginData directory to avoid trashing module managers cache
- Fix stack trace output when verbose errors is enabled
- Performance improvements and reduced memory allocation pressure in main server loop
- Renamed `Parameter.default_argument` to `default_value` in protobuf definitions

## [v0.3.3]
- Fix popup dialogs not displaying (#266)

## [v0.3.2]
- Fix `KRPC.CurrentGameScene` (#262)

## [v0.2.4]
- Shut down server when switching to main menu (or any other non-game scene)

## [v0.2.3]
- Added `KRPC.CurrentGameScene` (#237)

## [v0.2.2]
- Stream updates are only sent to clients when the result changes (#170)
- Removed support for protobuf enumerations and custom protobuf messages as RPC parameter/return values. Note: these features were not used anywhere, and only cause complications for client libraries
- Fix bug where KSP hangs if the game is quit whilst the server is running
- Remove support for blizzy's toolbar, in favour of using the stock applauncher

## [v0.2.1]
- Add "Any" option to server address drop down

## [v0.2.0]
- Update to protobuf 3.0.0-beta-2

## [v0.1.12]
- Built for KSP 1.0.5

## [v0.1.11]
- Documentation returned by calling `KRPC.GetServices` (#31)
- Fix bug preventing disconnection of clients from the server window (#161)
- Fix clearing bytes read/written statistics when the server is restarted
- Add "Clear Statistics" button to server info window
- Documentation at https://krpc.github.io/krpc/ generated by calling `KRPC.GetServices` (#31)

## [v0.1.10]
- KSP 1.0.4 support (#151)
- Performance improvements (#129,#131,#139)
- Fix UI disappearing bug (#133)

## [v0.1.9]
- None

## [v0.1.8]
- KSP 1.0.2 support (#114)
- Add support for static class methods (#106)

## [v0.1.7]
- Fix CKAN install bug on Windows (#105)

## [v0.1.6]
- Nicer errors returned to client (#97)
- Include protobuf source and compiled Python, Java and C++ versions in release (#100)

## [v0.1.5]
- Server now persists across game scene changes
- Services specify during which game scenes they are available
- Use a combo box for the server address
- Display service errors when KSP loads (#89)
- Add KSP compatibility checker
- Add logging severity levels
- Fix bug where yielded continuations are run more than once per update

## [v0.1.4]
- Support for KSP 0.90.0
- Add support for tuples (#49,#50)
- Add support for streams (#24,#56)
- Fix issue with null values passed as a parameter (#65)
- Add KSP AppLauncher button
- No longer bundle Toolbar plugin (kRPC will still use Toolbar plugin if available)
- Performance improvements (#68)

## [v0.1.3]
- Add support for collections (lists, dictionaries and sets)

## [v0.1.2]
- None

## [v0.1.1]
- Minor server code improvements

## [v0.1.0]
- Initial pre-release
