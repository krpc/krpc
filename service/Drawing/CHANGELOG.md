## [v0.7.0]

- **Breaking:** Return and set colors as a `StdLib.ColorRGB` structure, in place of a tuple (#1148)
- **Breaking:** Removing a drawing object another client created, or one the server no longer
  holds, reports that it is not among those this client created (#1072)
- Add `Drawing.AddPolyline`, `AddCircle`, `AddArc`, `AddBox`, `AddSphere`, `AddPoint`, `AddAxes`
  and `AddOrbit` (#1116)
- Add `StartArrow`, `EndArrow`, `ArrowSize`, `Colors`, `Dashed` and `Label` to lines, polygons
  and paths, and `Text.FaceCamera` (#1116)
- Add `Drawing.AddMapMarker`, which draws one of the game's map icons in the map view, and
  `Drawing.AddImage` for custom marker images (#1116)
- Add `Drawing.AddNavballMarker`, which draws a marker on the navball pointing in a given
  direction (#1037)
- Draw lines, polygons and text in the map view, the editor and the tracking station (#1115)
- Raise `KRPC.ObjectDestroyedException` from a drawing object once the object it draws is gone,
  and skip drawing one whose reference frame is gone (#1051)

## [v0.5.0]
- Add `Drawing.AddDirectionFromCoM`
- Change `Drawing.AddDirection` to start the line at the origin of the reference frame (#518)

## [v0.4.8]
- Fix reference frames for `AddDirection` so that the line always starts at the active vessel's CoM (#486)

## [v0.4.0]
- Make `Test.AvailableFonts` a static method

## [v0.3.4]
- Initial version (#253)
