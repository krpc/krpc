## core

- Add `internal` to the `Procedure` message
- Mark the stream, event and server side function procedures of the `KRPC` service as internal
- **Breaking:** Reject a service that holds a stream or an event anywhere but a return value, or a
  stream returned by a procedure that is not internal

## client/cnano

- **Breaking:** Leave internal procedures out of the services, such as `krpc_KRPC_AddStream`

## client/csharp

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.AddStream`
- Add `EncodingException`, thrown when the server returns null for a value that cannot be null

## client/cpp

- **Breaking:** Leave internal procedures out of the services, such as `KRPC::add_stream`
- Raise the exception types of the `KRPC` service, such as `KRPC::InvalidOperationException`,
  without first creating a `KRPC` service object

## client/java

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.addStream`

## client/lua

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.add_stream`

## client/python

- **Breaking:** Leave internal procedures out of the services, such as `KRPC.add_stream`

## tools/krpctools

- Leave internal procedures out of generated services and API documentation
