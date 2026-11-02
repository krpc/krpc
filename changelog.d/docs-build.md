## tools/krpctools

- Generate several pages in one `krpc-docgen` run, with `--page` (#1252)
- **Breaking:** Replace the `SOURCE` and `OUTPUT` arguments of `krpc-docgen` with `--page` (#1252)
- Fix `krpc-docgen` Java links to a service's own procedures and properties, and to enumeration
  values (#1252)
- **Breaking:** Write the Lua API documentation from `krpc-docgen` for `sphinx-lua-ls`, in place of
  `sphinxcontrib-luadomain`, with typed signatures (#1202)

## service/SpaceCenter

- Fix the `Vessel.Flight` link to the `Flight` class (#1252)
