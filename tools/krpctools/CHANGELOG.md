## [v0.7.0] - unreleased
- Fix the python stubs generated for a service that names a type the KRPC service defines
  failing to import. The stubs import the module the naming service's types are in, and the
  KRPC service's module is named `krpc`, which hid the `krpc` package the stubs read their
  limits and schema from (#1069)
- Java enumeration member names are no longer escaped before being upper-cased. Java's
  keywords are all lower case so a constant's name cannot collide with one, and escaping
  produced names such as `DOUBLE__` in place of `DOUBLE` (#1069)
- Generate and document the structure types a service defines, for every client (#1066)
- **Breaking:** A generator module given to `krpc-clientgen` by path is constructed with
  the definitions of every service that was loaded, rather than with those of the single
  service it generates (#1044)
- Support for nullable values across all types (#1017)
- Generate an enumeration default value as the member it names, instead of a cast of its
  integer value (#1044)
- Fix generating a default value whose type contains an enumeration, such as a list of
  them, failing (#1044)
- Fix Lua documentation writing default values in Python's syntax rather than Lua's
  (#1044)
- Fix C++ documentation writing a collection default value as a constructor call rather
  than a braced initializer list, so `std::vector<int32_t>(1, 2, 3)` named a constructor
  that builds something else entirely (#1044)
- A default value naming an undeclared enumeration value, or one whose enumeration no
  supplied service defines, is now reported naming the service, procedure and parameter
  it came from (#1044)
- The special values of the numeric types - NaN, the infinities and the extremes of the
  integer and floating point ranges - can now be used as parameter default values.
  Generated code and documentation name them as the target language does, such as
  `double.NaN` in C# and `(std::numeric_limits<int32_t>::max)()` in C++, rather than
  writing a decimal literal that does not compile. Neither Python nor Lua has a name of its
  own for the finite extremes, so both name the new `krpc.limits` constants (#1045)
- Fix a 32-bit float default value being written as the full decimal expansion of the
  double it widens to, so `0.1f` read as `0.10000000149011612`, in Python, C# and C++
  code and in the Python, C#, C++ and Lua documentation. C++ also writes it as a float
  literal now, rather than a double one (#1045)

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
