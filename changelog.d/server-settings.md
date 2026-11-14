## core

- Turn on "Auto-start server" by default
- **Breaking:** Remove the "One RPC per update" setting and
  `KRPC.ServerSettings.OneRpcPerUpdate`

## server

- Start a new install with one server, with the default settings
- Keep the server settings on an upgrade, as the release no longer includes `settings.cfg`
- Keep each readable setting from a damaged `settings.cfg`, and back up a file with no readable
  settings as `settings.cfg.bak`
- Widen the server window so the advanced settings fit
- Line up the labels in the server window's advanced settings with their text fields
- Give the kRPC windows an opaque background
