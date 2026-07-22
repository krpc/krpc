## [0.6.0] - unreleased
- Add the `TestCase.expansions` attribute, to declare KSP expansions a test class
  requires; the class is skipped when a required expansion is not installed
- Run the integration tests with `pytest`, replacing the custom test runner
- Automatically launch KSP and install the required mods when running the tests,
  relaunching only when the required mod set changes
- Replace the shell scripts with the `krpc-install` and `krpc-run-ksp` console scripts
- Add `krpc-run-ksp` `--load-*` options (forwarded to `TestingTools` as `--krpctest-load-*`
  arguments) to auto-load a save, switch vessel or launch a craft on startup, failing
  loudly when given an invalid argument
- Add the `set_flight`, `fill_all_resources`, `set_crew_to_pilot` and `launch_vessel_from_sph`
  test helpers
- Add `assertRadiansAlmostEqual`
- Avoid reloading the save in `new_save` when the game is already in the expected state
- Fix a crash when waiting for a vessel and there is no active vessel yet
- Migrate packaging to `pyproject.toml` (`hatchling`) and require Python 3.10+

## [0.3.9]
- Update test saves for KSP 1.3

## [0.3.8]
- Clean up code to meet PEP 8 guidelines

## [0.3.3]
- Initial version
