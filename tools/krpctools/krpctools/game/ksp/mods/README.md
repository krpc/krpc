# Mods

The supported mods are pinned in the registry, `service/registry.bzl` in the kRPC repository.
The subdirectories here are config overlays, layered onto a mod after it is installed into
KSP's `GameData` (see `KSP.install_mod` in `../game.py`). Overlays have three
uses: suppressing a mod's first-run window, which blocks the unattended game the tests drive,
adding a test-only part config for a mod whose archive ships a part module but no parts, and
adding test-only props or pages.

Each mod's overlays are in a subdirectory named after the mod. In it, each subdirectory mirrors
one of the mod's `GameData/<folder>` trees, and its files are copied over the freshly-installed
mod (overwriting on conflict).

* `RasterPropMonitor/JSI/KRPCTestDisplay.cfg` — a ModuleManager patch adding a page named `krpctest` to every
  Raster Prop Monitor screen, showing the vessel's display named `test`. kRPC's own patch gives
  each screen a page for its own display, and this one lets the tests show one display on
  several screens.

* `DMagic/DMagicScienceAnimate/TestPart.cfg` — a ModuleManager patch adding `dmagicSensorTest`, a part
  whose science experiment is a `DMModuleScienceAnimateGeneric`, so the SpaceCenter `Experiment`
  tests can exercise kRPC's DMagic support. The DMagic archive is only the part module, no parts.
* `KerbalActuators/WildBlueIndustries/TestPart.cfg` - a ModuleManager patch adding two Juno clones with a
  `WBIPropSpinner`, `kerbalActuatorsReverserTest` and `kerbalActuatorsFixedTest`, for the
  KerbalActuators thrust reverser tests. KerbalActuators ships the part module but no parts.

* `B9PartSwitch/B9PartSwitch/TestPart.cfg` — a ModuleManager patch adding `b9PartSwitchTest`, an FL-T400
  whose tank switches between three B9PartSwitch subtypes. The B9PartSwitch archive has no parts.

* `ClickThroughBlocker/000_ClickThroughBlocker/Global.cfg` — the global focus-handling default. Its presence is
  what makes ClickThroughBlocker skip its first-run focus popup on a new/loaded save (the popup
  only shows when this file is absent); `PopUpShown.cfg` alone does not suppress it.
* `ClickThroughBlocker/000_ClickThroughBlocker/PluginData/PopUpShown.cfg` — the "popup already shown once" marker
  ClickThroughBlocker writes after dismissal; seeded too so the install matches a dismissed one.
* `ToolbarControl/001_ToolbarControl/PluginData/ToolbarControl.cfg` — `showWindowAtStartup = False`
  suppresses ToolbarControl's first-run intro window.

Both of those are RealChute dependencies; RealChute itself needs no overlay.

* `KSPCommunityFixes/KSPCommunityFixes/PluginData/PNGTextureCache.cfg` — the record of the answer to the
  texture-caching question KSPCommunityFixes asks on first load. Its loader only puts the
  dialog up while `userOptInChoiceDone` is unset, so seeding the file skips it. Caching is
  declined: the cache is written inside the mod's own directory, which is replaced on every
  install, so it would be rebuilt each time and never pay for itself.

  KSPCommunityFixes is a RealFuels dependency (by way of ROUtils); neither of those, nor
  RealFuels itself, needs an overlay.
