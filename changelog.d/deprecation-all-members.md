## client/cnano

- Mark deprecated classes, enumerations, enumeration values, structures and structure fields with
  `KRPC_DEPRECATED` (#1322)
- Mark the `_call` functions of deprecated procedures with `KRPC_DEPRECATED` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)

## client/csharp

- Mark a deprecated service's extension method and `Service` class with `[Obsolete]` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)

## client/cpp

- Declare a deprecated service's class `[[deprecated]]` (#1322)
- Declare the `_stream` and `_call` functions of deprecated procedures `[[deprecated]]` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)
- Fix generated headers warning about their own deprecated members (#1322)

## client/java

- Mark a deprecated service's class with `@Deprecated` (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)

## client/lua

- Warn once per member on standard error on a use of a deprecated service, procedure, method,
  property, class, enumeration, enumeration value or structure (#1322)
- Warn when `get_call` builds the call of a deprecated procedure, method or property (#1322)
- Deprecate each member of a deprecated service, class or enumeration (#1322)

## client/python

- Warn when a stream or `get_call` uses a deprecated procedure, method or property (#1322)
- Warn on a use of a deprecated service, class, enumeration, enumeration value, structure,
  structure field or exception (#1322)
- Mark deprecated members of the generated stubs with `@deprecated`, so type checkers report them (#1322)
- Deprecate each member of a deprecated service, class, enumeration or structure (#1322)

## tools/krpctools

- Add the `DeprecationSignals` command to the conformance testee protocol, which a testee needs for
  the deprecation tests (#1322)
- Ship the `DeprecatedService` definitions beside `TestService.json` (#1322)
