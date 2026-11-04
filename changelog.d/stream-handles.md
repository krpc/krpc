## core

- **Breaking:** Encode a stream or an event as the `uint64` id of its stream (#1266)
- **Breaking:** Remove the `Stream` and `Event` messages (#1266)

## client/cnano

- **Breaking:** Return a stream or an event as the `uint64_t` id of its stream (#1266)

## client/csharp

- Streams and events
  - **Breaking:** Return the stream id from `KRPC.AddStream` and `KRPC.AddFunctionStream` (#1266)
  - **Breaking:** Take the type code of the return value in `IConnection.Invoke` (#1266)

## client/cpp

- Streams and events
  - **Breaking:** Return the stream id from `KRPC::add_stream` and `KRPC::add_function_stream` (#1266)
  - **Breaking:** Remove `Client::Returns::event`, as a call that returns an event uses
    `Returns::stream` (#1266)

## client/java

- Streams and events
  - **Breaking:** Return the stream id from `KRPC.addStream` and `KRPC.addFunctionStream` (#1266)

## client/lua

- **Breaking:** Return a stream or an event as the id of its stream (#1266)

## client/python

- Streams and events
  - **Breaking:** Return the stream id from `KRPC.add_stream` and `KRPC.add_function_stream` (#1266)
