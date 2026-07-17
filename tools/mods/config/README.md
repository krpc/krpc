# Managed-mod config overlays

Config files layered onto a managed test mod after it is installed into KSP's `GameData`
(see `_mod_config_overlay` in `tools/krpctest/krpctest/install.py`). Two uses: suppressing a
mod's first-run window so the unattended game the integration tests drive is not blocked, and
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

Both are RealChute dependencies; RealChute itself needs no overlay.
