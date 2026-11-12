## client/cpp

- Errors
  - **Breaking:** Derive every error of the client from the new `krpc::RemoteError` (#1333)
  - **Breaking:** Rename `krpc::RPCError` to `krpc::ServerError` (#1333)
  - **Breaking:** Throw `StreamError` when the server removes a stream or event (#1333)
  - **Breaking:** Throw `std::invalid_argument` for a local socket path that is too long (#1333)
  - **Breaking:** Throw `EncodingError` for a null function value of a type that cannot hold null (#1333)

## client/csharp

- Errors
  - **Breaking:** Derive every exception of the client from the new `RemoteException` (#1333)
  - **Breaking:** Rename `RPCException` to `ServerException`, and `FunctionCompilationException`
    to `CompilationException` (#1333)
  - **Breaking:** Remove the generated classes of mapped exceptions, which were never thrown, such
    as `KRPC.ArgumentException`, and of `KRPC.StreamRemovedException`. The .NET exception is
    thrown, as before (#1333)
  - **Breaking:** Throw the new `StreamException` when a stream or event is removed (#1333)
  - **Breaking:** Throw `ConnectionException` for a call after the connection is disposed (#1333)
  - **Breaking:** Throw `ArgumentException` for a function whose value does not match the type
    parameter (#1333)
  - **Breaking:** Throw `EncodingException` for every value that fails to decode, and for a null
    stream value of a type that cannot hold one (#1333)

## client/java

- Errors
  - **Breaking:** Make every exception unchecked, deriving the new `RemoteException` (#1333)
  - **Breaking:** Rename `RPCException` to `ServerException` (#1333)
  - **Breaking:** Remove `throws IOException` from `Connection.newInstance`, `newLocalInstance`
    and `close` (#1333)
  - **Breaking:** Remove the generated classes of mapped exceptions, which were never thrown, such
    as `KRPC.ArgumentException`, and of `KRPC.StreamRemovedException` (#1333)
  - **Breaking:** Throw `StreamException` when the server removes a stream or event (#1333)
  - **Breaking:** Throw `ConnectionException` for a call or stream the connection ends, in place
    of an `RPCException` or `StreamException` wrapping it (#1333)
  - **Breaking:** Throw `CancellationException` for an interrupted call or wait (#1333)
  - **Breaking:** Throw `IllegalArgumentException` for a null object, a null in a position that
    cannot hold one, a local socket path that is too long, and a method `addStream` or `getCall`
    cannot find (#1333)
  - Throw `EncodingException` for a tuple with too few items (#1333)
  - Give an `EncodingException` the message of its cause (#1333)

## client/lua

- Errors
  - **Breaking:** Raise errors as objects of the new classes `krpc.RemoteError`,
    `krpc.ConnectionError`, `krpc.ServerError` and `krpc.EncodingError`, in place of strings.
    An argument the client rejects still raises a string (#1333)
  - **Breaking:** Drop the source position and the `Service.Name:` prefix from the message of
    an error, which the error's fields now carry (#1333)
  - **Breaking:** Raise `ConnectionError` for malformed service definitions (#1333)
  - **Breaking:** Raise `bad argument #2 to 'connect_local'` for a local socket path that is too
    long (#1333)

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
  exceptions from `ServerError` or `ServerException` (#1333)
- Generate no class for a mapped exception in C# and Java stubs (#1333)
