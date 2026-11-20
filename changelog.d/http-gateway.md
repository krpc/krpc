## core

- Add a JSON over HTTP server, for calling procedures with no client library (#1391)
- Serve an OpenAPI document of the HTTP server's paths (#1391)

## server

- Add *JSON over HTTP* to the protocols in the server window, on port 50003 by default, with the
  origins whose web pages may call it (#1391)

## tools/krpctools

- Add the `http` server type, `--http-port` and `--http-allowed-origins` to `krpc-testserver` (#1391)
- Add the `http` language to `krpc-docgen` (#1391)
