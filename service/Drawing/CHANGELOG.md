## [v0.7.0] - unreleased
- Add `Drawing.AddNavballMarker`, which draws a marker on the navball pointing in a given
  direction, for example to show a script's target attitude while the player flies the vessel.
  The marker's icon, color and size can be changed, and it hides and fades as the navball's own
  markers do (#1037)

## [v0.5.0]
- Add `Drawing.AddDirectionFromCoM`
- Change `Drawing.AddDirection` to start the line at the origin of the reference frame (#518)

## [v0.4.8]
- Fix reference frames for `AddDirection` so that the line always starts at the active vessel's CoM (#486)

## [v0.4.0]
- Make `Test.AvailableFonts` a static method

## [v0.3.4]
- Initial version (#253)
