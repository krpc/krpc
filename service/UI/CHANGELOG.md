## [v0.7.0]

- Breaking changes
  - **Breaking:** Return and set colors as a `StdLib.ColorRGBA` structure with an alpha
    component, for `Text.Color` and the color of a `UI.Message` (#1040, #1148)
  - **Breaking:** Draw user interface elements with the game's own skin, so `Text` starts out
    with the game's size and color (#1040)
  - **Breaking:** Make `Visible` the element's own setting. An element is drawn when everything
    it is inside is visible too (#1040)
  - **Breaking:** Set `InputField.Changed` only when the user types in the field (#1040)
  - **Breaking:** Removing an element another client created, or one the server no longer
    holds, reports that it is not among those this client created (#1072)

- Elements
  - Add `Toggle`, a check box, and `ToggleGroup`, which makes a set of toggles behave as radio
    buttons (#1040)
  - Add `Slider`, horizontal or vertical, and `Dropdown` (#1040)
  - Add `Image`, showing a PNG or JPEG file, a plain colored rectangle, or raw pixels from
    `Image.SetPixels` and `Image.UpdatePixels` (#1040)
  - Add `ScrollView`, a view onto a panel larger than the space available for it (#1040)
  - Add `Layout` to arrange the contents of a `Panel` in a row, column or grid, with
    `LayoutElement` and `Panel.SizeFitter` for sizing (#1040)

- Properties and behavior
  - Create user interface elements in every game scene, and remove them when the scene changes
    (#1040)
  - Add `Interactable`, `Color` and `Tooltip` to every control (#1040)
  - Add `Panel.Style`, `Panel.Color`, `Panel.Draggable` and `Panel.BringToFront`, to use a panel
    as a movable window (#1040)
  - Add `InputField.Placeholder`, `ContentType`, `CharacterLimit` and `ReadOnly` (#1040)
  - Add `Button.Pressed`, true while the user holds the button down, and `Text.WordWrap` (#1040)
  - Scale a canvas added with `UI.AddCanvas` by the player's interface scale, as the stock
    canvas is (#1040)
  - Compare two user interface objects for the same element equal (#1040)
  - Raise `KRPC.ObjectDestroyedException` from a user interface object, `RectTransform`,
    `Layout`, `LayoutElement` or `SizeFitter` once its element is gone (#1051)
  - Fix `Text.Font` creating a font on every set and leaking it (#1040)
  - Fix `RectTransform` objects accumulating for the rest of the session (#1051)

## [v0.6.0]
- Fix locale issues with `UI.Message` (#993)

## [v0.3.5]
- Add `Canvas` class (#281)
- Add `UI.StockCanvas` to get the stock KSP UI canvas and `UI.AddCanvas` to create additional canvases
- Move `UI.AddPanel` and `UI.RectTransform` to `Canvas` class

## [v0.3.4]
- Initial version
