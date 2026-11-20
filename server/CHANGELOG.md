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

## [v0.6.0]
- Fix server version reported to clients (read from `KRPC.dll` not `KRPC.Core.dll`) (#848)
- Fix Logger to write to both `KSP.log` and `Player.log` (#780)
- Improve RPC and stream update performance (#879)
- Fix Stream RPCs Executed and Stream RPC Rate always showing 0 in the info window (#879)
- Improve the server window UI (#919)
- Fix incorrect unit shown for "max. time per update" and "receive timeout" (#919)
- Support switching the game scene by setting `KRPC.GameScene` (#897)
- Fix repeated toolbar icons appearing in some situations (#939)

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
