## client/cnano

- Transports
  - **Breaking:** Build every transport into the library, and choose one when opening a
    connection with `krpc_open_serial`, `krpc_open_tcp` or `krpc_open_localsocket`, in place of
    `krpc_open` (#1176)
  - **Breaking:** Remove `KRPC_COMMUNICATION_TCP`, `KRPC_COMMUNICATION_LOCALSOCKET` and
    `krpc_connection_config_t`, and the `tcp` and `localsocket` vcpkg features (#1176)
  - **Breaking:** Replace `KRPC_COMMUNICATION_CUSTOM` with a `krpc_transport_t` that a program
    fills in (#1176)
  - **Breaking:** On Arduino, open any `Stream`, such as `SoftwareSerial` or a USB port, with
    `krpc_open_stream`, or start a serial port with `krpc_open_serial` (#1176)
