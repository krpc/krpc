## [v0.7.0] - unreleased

- Add `Drawing.AddPolyline`, `Drawing.AddCircle` and `Drawing.AddArc` (#1116)
- Add `Drawing.AddOrbit`, which draws the path of an orbit (#1116)
- Add `Drawing.AddPoint`, which draws a dot that keeps its size on screen (#1116)
- Add `Drawing.AddAxes`, which draws the axes of a reference frame (#1116)
- Add arrow heads to lines, polygons and the new paths, with `StartArrow`, `EndArrow` and
  `ArrowSize` (#1116)
- Add `Text.FaceCamera`, which turns text to face the camera (#1116)
- Add `Colors`, `Dashed` and `Label` to lines, polygons and the new paths (#1116)
- Add `Drawing.AddBox` and `Drawing.AddSphere` (#1116)
- Add `Drawing.AddMapMarker`, which draws a marker in the map view with one of the game's
  own map icons (#1116)
- Add `Drawing.AddImage`, which loads a PNG or JPEG image to draw map and navball markers
  with (#1116)
- Draw lines, polygons and text in the map view, the editor and the tracking station (#1115)
- **Breaking:** Removing a drawing object another client created, or one the server no longer
  holds, reports that it is not among those this client created (#1072)
- Line, polygon, text and navball marker objects raise `KRPC.ObjectDestroyedException` once
  the object they draw is gone, and are freed (#1051)
- A drawing object whose reference frame is defined against something the game has destroyed
  is not drawn, rather than failing on every frame (#1051)
- Add `Drawing.AddNavballMarker`, which draws a marker on the navball pointing in a given
  direction. Its icon, color and size can be changed (#1037)

## [v0.5.0]
- Add `Drawing.AddDirectionFromCoM`
- Change `Drawing.AddDirection` to start the line at the origin of the reference frame (#518)

## [v0.4.8]
- Fix reference frames for `AddDirection` so that the line always starts at the active vessel's CoM (#486)

## [v0.4.0]
- Make `Test.AvailableFonts` a static method

## [v0.3.4]
- Initial version (#253)
