## client/python

- Streams and events
  - Remove a stream or an event at the end of a `with` statement on it (#1291)
  - **Deprecated:** `Client.stream`, superseded by `add_stream` in a `with` statement (#1291)

## client/csharp

- Streams and events
  - Make `Stream` and `Event` `IDisposable`, to remove them at the end of a `using` block (#1291)

## client/java

- Streams and events
  - Make `Stream` and `Event` `AutoCloseable`, to remove them at the end of a try-with-resources block (#1291)
