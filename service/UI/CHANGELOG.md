## [v0.7.0] - unreleased

- **Breaking:** Removing a user interface element another client created, or one the server no
  longer holds, reports that it is not among those this client created (#1072)
- **Breaking:** Colors are given as `(red, green, blue, alpha)` rather than
  `(red, green, blue)`, so an element can be drawn translucent. Affects `Text.Color` and the
  color of a `UI.Message` (#1040)
- **Breaking:** `InputField.Changed` is only set when the user types in the field (#1040)
- **Breaking:** User interface elements are drawn using the skin the game draws its own
  interface in, so the size and color that `Text` starts out with are not fixed values (#1040)
- **Breaking:** `Visible` is the element's own setting. An element is drawn when everything it
  is inside is visible as well, so an interface can be built inside a hidden panel and shown
  all at once (#1040)
- User interface elements can be created in every game scene (#1040)
- User interface elements are removed when the game scene changes (#1040)
- Add `Interactable` to every control, to gray one out and stop it responding to the user
  (#1040)
- Add `Toggle`, a check box, and `ToggleGroup`, which makes a set of toggles behave as radio
  buttons. A group refers to its toggles rather than containing them (#1040)
- Add `Layout` to arrange the contents of a `Panel` in a row, a column or a grid.
  `Panel.AddHorizontalLayout`, `Panel.AddVerticalLayout` and `Panel.AddGridLayout` create
  one (#1040)
- Add `Slider`, `Dropdown` and `Image`. An image shows the contents of a PNG or JPEG file, or
  a plain colored rectangle (#1040)
- Add `Panel.Style`, to draw a panel as a box rather than a window, and `Panel.Color` (#1040)
- Add `Panel.Draggable`, so a panel can be moved by the user and used as a window (#1040)
- Add `ScrollView`, a view onto a panel larger than the space available for it (#1040)
- Add `LayoutElement`, for how much space a user interface object asks a layout for, and
  `Panel.SizeFitter`, for sizing a panel to fit what it contains (#1040)
- Add `InputField.Placeholder`, the hint drawn in an input field while it is empty (#1040)
- Add `InputField.ContentType`, filtering what the user can type into a field as they type it,
  along with `InputField.CharacterLimit` and `InputField.ReadOnly` (#1040)
- Add `Color` to every control, tinting the sprite it is drawn with (#1040)
- Add `Tooltip` to every control, a short piece of text shown beside the pointer while it
  rests on the control (#1040)
- Add `Panel.BringToFront`, to draw a panel in front of the elements beside it. A draggable
  panel also brings itself to the front when the user presses it (#1040)
- `AddSlider` can create a vertical slider, running from bottom to top (#1040)
- Add `Button.Pressed`, true while the user is holding the button down (#1040)
- Add `Text.WordWrap`, so a value label can be kept to one line (#1040)
- Add `Image.SetPixels`, drawing a picture from raw pixels rather than a file.
  `Image.UpdatePixels` redraws a block of the picture and leaves the rest (#1040)
- Two user interface objects that refer to the same element compare equal (#1040)
- A canvas added with `UI.AddCanvas` follows the interface scale the player has set, as the
  stock canvas does (#1040)
- Fix `Text.Font` making a font of its own every time it is set, and leaving it behind when
  the label went. A font of a given name is made once and shared (#1040)
- A user interface object raises `KRPC.ObjectDestroyedException` once its element is gone,
  which removing it, `UI.Clear`, the client that made it disconnecting and changing scene all
  do (#1051)
- `RectTransform`, `Layout`, `LayoutElement` and `SizeFitter` do the same once the element
  they were taken from is gone, and are dropped with it (#1051)
- Fix `RectTransform` objects accumulating for the rest of the session. Reading the same one
  twice gives the same object (#1051)

## [v0.6.0]
- Fix locale issues with `UI.Message` (#993)

## [v0.3.5]
- Add `Canvas` class (#281)
- Add `UI.StockCanvas` to get the stock KSP UI canvas and `UI.AddCanvas` to create additional canvases
- Move `UI.AddPanel` and `UI.RectTransform` to `Canvas` class

## [v0.3.4]
- Initial version
