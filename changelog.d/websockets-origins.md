## core

- **Breaking:** Refuse WebSockets connections from web pages whose origin is not allowed, by
  default any page not served from this machine (#1392)

## server

- Add the origins whose web pages may connect to WebSockets servers in the server window (#1392)

## tools/krpctools

- Add `--websockets-allowed-origins` to `krpc-testserver` (#1392)
