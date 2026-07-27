## [v0.7.0] - unreleased
- Fix one server stopping emptying the object store that every server shares, which invalidated
  the objects held by clients of the servers still running (#1020)
- Support for nullable values across all types (#1017)
- **Breaking:** Null values are now signaled by an `is_null` field rather than by an object id
  of 0, and nullability is enforced uniformly for every type. Class-typed arguments are no longer implicitly nullable (#1017)
- Fix a request that names an object the server no longer has leaving the connection stuck, with
  every later call failing; the failure is now reported to the client as the error it is (#1019)

## [v0.6.0]
- Add `Version` property to `Core`, set by the server plugin on startup (#848)
- Enable `TCP_NODELAY` on client TCP connections, reducing RPC round-trip latency (#879)
- Surface deprecated members (annotated with `[Obsolete]`) in the service definition and over the wire (#904)
- Add `KRPC.GameScene` and make it settable to switch the current game scene (#897)
- **Deprecated:** `KRPC.CurrentGameScene`, kept as a read-only alias of `KRPC.GameScene` (#897)
- Add `AstronautComplex`, `MissionControl`, `ResearchAndDevelopment`, `Administration` and `MissionBuilder` game scenes (#897)
- Fix the game scenes a procedure is available in never being sent by `GetServices`; the
  `game_scenes` field of every procedure was always empty (#991)
- Reduce copying and allocation overhead receiving protobuf messages (#972)
- Fix the websocket servers rejecting connection requests that are split across multiple reads (#973)
- Fix websocket connection URL query parameter parsing (#973)
- Fix locale issues with type codes in the service description, and HTTP and websocket protocol tokens (#993)

## [v0.5.4]
- Initial version, split off from `KRPC.dll`
