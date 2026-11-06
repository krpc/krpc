## client/cpp

- Connections
  - Add `Client::close`, which closes the connection for every copy of the client (#1286)
  - **Breaking:** Services, objects, streams and events hold the connection open, so they stay valid once the client is destroyed (#1286)
