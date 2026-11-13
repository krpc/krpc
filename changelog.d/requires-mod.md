## core

- Add the `RequiresMod` attribute, which fails the calls of a member whose mod is missing, failed to
  load or is not ready (#1342)

## server

- Fail the class members of a mod service with the reason when its mod is missing or failed to
  load (#1342)

## service/SpaceCenter

- Fail the FAR and RealFuels members, and `Part.Tag`, with the reason when the mod is missing or
  failed to load (#1342)
