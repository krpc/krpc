## [v0.7.0] - unreleased
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
