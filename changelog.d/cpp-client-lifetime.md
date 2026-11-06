## client/cpp

- Connections
  - Add `Client::close`, which closes the connection for every copy of the client (#1286)
  - **Breaking:** Services, objects, streams and events hold the connection open, so they stay valid once the client is destroyed (#1286)
  - **Breaking:** A client that is not connected throws `ConnectionError`, as does a default-constructed object (#1286)
- **Breaking:** Object, stream and event constructors take the client by reference, as in `Vessel(conn, id)` (#1286)
- Service constructors accept a temporary client, as in `SpaceCenter sc(krpc::connect())` (#1286)
