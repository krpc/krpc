## client/cpp

- Errors
  - **Breaking:** Derive every error of the client from the new `krpc::RemoteError` (#1333)
  - **Breaking:** Rename `krpc::RPCError` to `krpc::ServerError` (#1333)
  - **Breaking:** Throw `StreamError` when the server removes a stream or event (#1333)
  - **Breaking:** Throw `std::invalid_argument` for a local socket path that is too long (#1333)
  - **Breaking:** Throw `EncodingError` for a null function value of a type that cannot hold null (#1333)

## client/python

- Errors
  - **Breaking:** Derive every error of the client from the new `krpc.error.RemoteError` (#1333)
  - **Breaking:** Rename `krpc.error.RPCError` to `ServerError`, and
    `FunctionCompilationError` to `CompilationError` (#1333)
  - Derive the exception classes a service declares, and a mapped exception with no builtin,
    from `ServerError` (#1333)
  - **Breaking:** Raise `StreamError` when the server removes a stream or event (#1333)
  - **Breaking:** Raise `ValueError` for a tuple or structure of the wrong length, a negative
    unsigned value, a null in a position that cannot hold one, and a local socket path that is
    too long (#1333)
  - **Breaking:** Raise `ValueError` in place of `StreamError` for arguments to a streamed
    function, and for a compiled function with parameters passed to `add_stream` or `add_event` (#1333)
  - **Breaking:** Raise `ConnectionError` for malformed service definitions (#1333)
  - Raise `EncodingError` for every value that fails to decode, including a string or tuple cut
    short (#1333)

## tools/krpctools

- Generate C++ mapped exceptions as aliases of the standard exceptions, and derive the other
  exceptions in C++ and Python stubs from `ServerError` (#1333)
