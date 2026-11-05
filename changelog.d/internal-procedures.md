## core

- Add `internal` to the `Procedure` message (#1269)
- Mark the stream, event and server side function procedures of the `KRPC` service as internal
  (#1269)
- **Breaking:** Reject a service that holds a stream or an event anywhere but a return value, or a
  stream returned by a procedure that is not internal (#1269)

## client/cnano

- **Breaking:** Leave internal procedures out of the services, such as `krpc_KRPC_AddStream` (#1269)

## client/csharp

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.AddStream` (#1269)
- Add `Connection.Compile` for a server side function built with the expression API (#1269)
- Add `EncodingException`, thrown when the server returns null for a value that cannot be null
  (#1269)

## client/cpp

- **Breaking:** Leave internal procedures out of the services, such as `KRPC::add_stream` (#1269)
- Add `krpc::compile` for a server side function built with the expression API (#1269)
- Raise the exception types of the `KRPC` service, such as `KRPC::InvalidOperationException`,
  without first creating a `KRPC` service object (#1269)

## client/java

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.addStream` (#1269)
- Add `Connection.compile` for a server side function built with the expression API (#1269)

## client/lua

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.add_stream` (#1269)

## client/python

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.add_stream` (#1269)
- Accept a server side function built with the expression API in `Client.compile` (#1269)

## tools/krpctools

- Leave internal procedures out of generated services and API documentation (#1269)
