# Mods

`__init__.py` defines the supported mods: each one's pinned archive, its dependencies and its
presence check. The subdirectories are config overlays, layered onto a mod after it is
installed into KSP's `GameData` (see `KSP.install_mod` in `../game.py`). Overlays have two uses:
suppressing a mod's first-run window, which blocks the unattended game the tests drive, and
adding a test-only part config for a mod whose archive ships a part module but no parts.

Each subdirectory mirrors a mod's `GameData/<subdir>` tree, and its files are copied over the
freshly-installed mod (overwriting on conflict).

* `DMagicScienceAnimate/TestPart.cfg` — a ModuleManager patch adding `dmagicSensorTest`, a part
  whose science experiment is a `DMModuleScienceAnimateGeneric`, so the SpaceCenter `Experiment`
  tests can exercise kRPC's DMagic support. The DMagic archive is only the part module, no parts.

* `000_ClickThroughBlocker/Global.cfg` — the global focus-handling default. Its presence is
  what makes ClickThroughBlocker skip its first-run focus popup on a new/loaded save (the popup
  only shows when this file is absent); `PopUpShown.cfg` alone does not suppress it.
* `000_ClickThroughBlocker/PluginData/PopUpShown.cfg` — the "popup already shown once" marker
  ClickThroughBlocker writes after dismissal; seeded too so the install matches a dismissed one.
* `001_ToolbarControl/PluginData/ToolbarControl.cfg` — `showWindowAtStartup = False`
  suppresses ToolbarControl's first-run intro window.

Both of those are RealChute dependencies; RealChute itself needs no overlay.

* `KSPCommunityFixes/PluginData/PNGTextureCache.cfg` — the record of the answer to the
  texture-caching question KSPCommunityFixes asks on first load. Its loader only puts the
  dialog up while `userOptInChoiceDone` is unset, so seeding the file skips it. Caching is
  declined: the cache is written inside the mod's own directory, which is replaced on every
  install, so it would be rebuilt each time and never pay for itself.

  KSPCommunityFixes is a RealFuels dependency (by way of ROUtils); neither of those, nor
  RealFuels itself, needs an overlay.
