# Managed-mod config overlays

Config files layered onto a managed test mod after it is installed into KSP's `GameData`
(see `_mod_config_overlay` in `tools/krpctest/krpctest/install.py`). The mod archives ship no
settings, so some mods pop a first-run window on launch that blocks the unattended game the
integration tests drive. These files mark those windows already-dismissed.

Each subdirectory mirrors a mod's `GameData/<subdir>` tree, and its files are copied over the
freshly-installed mod (overwriting on conflict).

* `000_ClickThroughBlocker/Global.cfg` — the global focus-handling default. Its presence is
  what makes ClickThroughBlocker skip its first-run focus popup on a new/loaded save (the popup
  only shows when this file is absent); `PopUpShown.cfg` alone does not suppress it.
* `000_ClickThroughBlocker/PluginData/PopUpShown.cfg` — the "popup already shown once" marker
  ClickThroughBlocker writes after dismissal; seeded too so the install matches a dismissed one.
* `001_ToolbarControl/PluginData/ToolbarControl.cfg` — `showWindowAtStartup = False`
  suppresses ToolbarControl's first-run intro window.

Both are RealChute dependencies; RealChute itself needs no overlay.
