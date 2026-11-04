## core

- Run a stream of a call or server side function with no value on each update, for its
  effects (#1198)

## client/csharp

- Streams and events
  - Stream a lambda or server side function with no value as a `Stream` (#1198)
  - Add an overload of `Connection.GetCall` for a lambda with no value (#1198)
  - **Breaking:** Derive `Stream<T>` from a new class `Stream`, ambiguous with `System.IO.Stream`
    in a file that uses both namespaces (#1198)

## client/cpp

- Streams and events
  - Stream a procedure or server side function with no value as a `Stream<void>` (#1198)

## client/java

- Streams and events
  - Stream a procedure or server side function with no value as a `Stream<Void>` (#1198)

## client/python

- Streams and events
  - Stream a procedure, property setter or server side function with no value as a stream of
    `None` (#1198)
  - Build the call of a property setter with `get_call` (#1198)

## tools/krpctools

- Generate a C++ `_stream` function for a method with no return value (#1198)
