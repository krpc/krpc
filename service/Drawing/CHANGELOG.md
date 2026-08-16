## [v0.7.0] - unreleased

- Line, polygon, text and navball marker objects raise `KRPC.ObjectDestroyedException` once
  the object they draw is gone, which removing it, `Drawing.Clear`, the client that made it
  disconnecting and leaving the flight scene all do. They previously failed on a destroyed
  game object, and were kept for the rest of the session (#1051)
- A drawing object whose reference frame is defined against something the game has destroyed
  is not drawn, rather than failing on every frame (#1051)
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
