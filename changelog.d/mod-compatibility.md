## service/SpaceCenter

- Fix setting a `PartField` skipping the callbacks the right-click menu runs, which left changes
  such as a B9PartSwitch subtype switch without effect (#1314)
- Support the curved solar panels from Near Future Solar in `SolarPanel` (#1314)
- Include the cost of part modules, such as fairing panels and TweakScale's scaling, in
  `Part.Cost` (#1314)
