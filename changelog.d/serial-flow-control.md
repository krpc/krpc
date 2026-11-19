## core

- Fix a serial IO server stopping when it sends a response over a megabyte, such as
  `KRPC.GetServices` with every service installed (#1386)

## server

- Add RTS/CTS flow control to serial IO servers, set in the server window (#1386)
- Build the serial port code into `KRPC.Core.dll`, in place of a separate `KRPC.IO.Ports.dll` (#1386)

## tools/krpctools

- Add `--flow-control` to the `TestServer`, for RTS/CTS over serial IO (#1386)
