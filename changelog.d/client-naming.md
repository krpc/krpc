## client/cpp

- **Breaking:** Rename the framing classes `krpc::Connection` and `krpc::LocalConnection` to
  `krpc::TCPIPTransport` and `krpc::LocalTransport`, in the header `krpc/transport.hpp` (#1332)
- **Breaking:** Rename `krpc::Client` to `krpc::Connection`, in the header `krpc/connection.hpp`
  in place of `krpc/client.hpp`. `krpc.hpp` includes it as before (#1332)

## client/python

- **Breaking:** Rename `krpc.Connection`, the socket of a connection, to
  `krpc.TCPIPTransport`, and `krpc.LocalConnection` to `krpc.LocalTransport` (#1332)
- **Breaking:** Rename `krpc.Client` to `krpc.Connection`, `krpc.ksp.KSPClient` to
  `krpc.ksp.KSPConnection`, and each service module's `<Service>Client` to `<Service>Connection` (#1332)

## tools/krpctools

- Name the connection class `Connection` in generated C++ and Python stubs (#1332)
