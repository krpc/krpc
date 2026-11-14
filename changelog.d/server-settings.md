## core

- Turn on "Auto-start server" by default (#1349)
- **Breaking:** Remove the "One RPC per update" setting and
  `KRPC.ServerSettings.OneRpcPerUpdate` (#1349)

## server

- Start a new install with one server, with the default settings (#1349)
- Keep the server settings on an upgrade, as the release no longer includes `settings.cfg` (#1349)
- Keep each readable setting from a damaged `settings.cfg`, and back up a file with no readable
  settings as `settings.cfg.bak` (#1349)
- Widen the server window so the advanced settings fit (#1349)
- Line up the labels in the server window's advanced settings with their text fields (#1349)
- Give the kRPC windows an opaque background (#1349)
