## [v0.8.0]

- Packaging
  - Relicense krpctools, including the bundled ServiceDefinitions tool, under the LGPLv3
    (#1221)
  - Publish a wheel in place of an sdist (#1289)

- Client generation
  - Generate the JavaScript client's TypeScript modules with `krpc-clientgen js` (#1346)
  - Name the connection class `Connection` in C++ and Python, and decode values with `Decoder`
    in C# and Java (#1332)
  - Generate exceptions that stand for their language's equivalent, and derive the others from
    `ServerError` or `ServerException` (#1172, #1333)
  - Generate the C-nano `_call` functions, and the `KRPC_EXCEPTION_`, `KRPC_CLASS_`,
    `KRPC_ENUMERATION_`, `KRPC_STRUCT_` and `KRPC_FIELD_` macros (#1267, #1333, #1336)
  - Generate a C++ `_stream` function for a method with no return value (#1198)
  - Support flags enumerations in `krpc-clientgen` and `krpc-docgen` (#1311)
  - Leave internal procedures out of generated services and API documentation (#1269)
  - Name the module of another service a generated Python module imports with `--include` (#1304)
  - **Deprecated:** `--ksp` in `krpc-clientgen`, superseded by `--reference-dir` (#1228)
  - Fix bytes default arguments in generated C# and C++ code and in the Lua documentation
    (#1194)

- Documentation generation
  - **Breaking:** Replace the `SOURCE` and `OUTPUT` arguments of `krpc-docgen` with `--page`,
    which generates several pages in one run (#1252)
  - Add the `js` language to `krpc-docgen` (#1346)
  - Add the `http` language to `krpc-docgen` (#1391)
  - **Breaking:** Write the Lua API documentation for `sphinx-lua-ls`, in place of
    `sphinxcontrib-luadomain`, with typed signatures (#1202)
  - Add `<unit>` and a LaTeX subset for `<math>` to service documentation, typeset by
    `krpc-docgen` and written as Unicode text by `krpc-clientgen` (#1169)
  - Report invalid documentation markup as an error from `krpc-docgen`, and a warning from
    `krpc-clientgen` (#1169)
  - Link each `<see cref>` to the member it names, including a service, and warn about one
    `krpc-clientgen` cannot resolve (#1185)
  - Say in the reference docs what each client raises for each exception of a service (#1333)
  - Print a plain error when the ordering file is missing a member (#1228)
  - Fix the links to a C-nano property with only a setter, and the Java links to a service's own
    members and to enumeration values (#1185, #1252)

- Service definitions
  - **Breaking:** Pass the directories of the assemblies a service references to
    `krpc-servicedefs` with `--reference-dir`, in place of the Kerbal Space Program directory
    (#1228)
  - Write the `mapped_type` of each exception to the service definitions (#1172)
  - Run `krpc-servicedefs` on Windows, and write its members in a fixed order (#1228, #1296)
  - Reject unknown options and invalid values in `krpc-servicedefs` and `krpc-testserver`
    (#1296)

- Test server
  - Add `krpc-testserver`, which runs the bundled `TestServer`, on Windows too (#1228)
  - **Breaking:** Stop releasing the `TestServer` archive and Docker image, replaced by
    `krpc-testserver` (#1228)
  - **Breaking:** Set the port of the WebSockets server with `--websockets-port`, in place of
    `--tcpip-port` (#1220)
  - Add the `http` server type, with `--http-port` and `--http-allowed-origins`, and
    `--websockets-allowed-origins` (#1391, #1392)
  - Add `--frame-limit`, `--fixed-rate`, `--tick-work-us`, `--frame-work-us` and `--preset`,
    which model a game's frame loop (#1376)
  - Add `--flow-control`, for RTS/CTS over serial IO (#1386)
  - Fix serial IO (#1228)

- Testing
  - Add `krpc-conformance`, which runs the client conformance tests against a client library
    (#1220)
  - Ship the `TestService2`, `ConformanceService` and `DeprecatedService` definitions beside
    `TestService.json`, and add `DeprecationSignals` to the testee protocol (#1284, #1322)
  - Add `krpc-game`, which installs kRPC, its test add-on and the requested mods into KSP, and
    runs the game (#1299)
  - Add `krpctools.game`, with a pytest plugin and test case classes for testing a mod against
    a running game (#1299)

## [v0.7.0]
- **Breaking:** Require `--include` in `krpc-clientgen` for each service whose types a C++ or C
  nano service uses, giving the path of its header (#1150)
- **Breaking:** Generate C# and C++ stubs that mark the calls returning a stream or an event,
  which need a v0.7.0 client (#1122)
- **Breaking:** Construct a generator module given to `krpc-clientgen` by path with the
  definitions of every loaded service (#1044)
- **Breaking:** Run `krpc-servicedefs` and `TestServer` on the .NET 10 runtime, in place of
  .NET 8 (#1139)
- Generate and document the structure types and nullable values a service defines, for every
  client (#1066, #1017, #1091)
- Read the definitions of several services in `krpc-clientgen`, from JSON files alongside DLLs
  (#1148)
- Generate python stubs that declare a service's classes, enumerations, structures and
  exceptions for type checkers (#1134)
- Allow NaN, the infinities and the numeric extremes as parameter default values, named as the
  target language names them (#1045)
- Generate an enumeration default value as the member it names, and report an undeclared one
  with its service, procedure and parameter (#1044)
- Fix default values containing an enumeration failing to generate, and Lua and C++
  documentation writing default values in the wrong syntax (#1044)
- Fix a 32-bit float default value being written in full, so `0.1f` read as
  `0.10000000149011612` (#1045)
- Fix the escaping of member names that are keywords in the generated Python, C++ and Java code
  and documentation (#1094)
- Fix the python stubs for a service that names a type the KRPC service defines failing to
  import, and its exception types naming no service (#1094)
- Fix `krpc-clientgen` failing to load a generator from a file (#1134)

## [v0.6.0]
- **Breaking:** Requires Python 3.10+
- **Breaking:** `krpc-servicedefs` now runs the `ServiceDefinitions` tool
  on the .NET 8 runtime; Mono is no longer required
- Surface deprecated members in generated documentation and client stubs (#904)
- Fix generated Java clients failing to compile when a service has enough procedures to
  push the generated type tables past the JVM's method size limit
- Generated C-nano functions are now defined as static inline, so the generated service
  headers can be safely included from multiple translation units
- Fix C-nano client generation emitting zero-sized argument arrays for procedures with
  no parameters
- Fix C-nano documentation rendering an entire service's API as an indented block quote
- Fix `docgen` dropping remarks in service-level documentation
- Fix cross-service member references not resolving in generated C# and Lua documentation (#897)
- Fix member references in generated Python documentation using lowerCamelCase
  instead of snake_case
- Fix generating client stubs and documentation from a KSP install failing when the machine's
  locale is not UTF-8 and a service's documentation contains non-ASCII characters

## [v0.5.4]
- Fix incorrect protobuf DLL (#755)
- Fix required KSP assemblies not being copied (#755)
- Fix tmp directory not being deleted (#755)
- Fix `clientgen` generating invalid python code for undocumented enumeration values (#757)

## [v0.4.9]
- Fix issue finding `KSP_x64_Data` directory (#523)
- Add `TestServer` archive to bin directory (#532)

## [v0.4.8]
- Fix Python 3 compatibility
- Fix template loading in `docgen` on Windows
- Add documentation of game scenes for each RPC

## [v0.3.8]
- Clean up code to meet PEP 8 guidelines

## [v0.3.7]
- Fix bug parsing nested collection types
- Fix bug generating key type for dictionaries

## [v0.3.6]
- Fix generating Java client stubs using `krpc-clientgen` with Python 3 (#308)

## [v0.3.4]
- Update protobuf to v3.0.0-beta-3

## [v0.2.2]
- Refactor into '`krpctools`' package containing '`krpc-clientgen`', '`krpc-docgen`' and '`krpc-servicedefs`'.
- Fixes caused by removal of support for protobuf enumeration and custom protobuf messages from server and clients

## [v0.2.1]
- Initial version
