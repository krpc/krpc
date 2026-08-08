## [v0.7.0] - unreleased

- **Breaking:** Colors are given as `(red, green, blue, alpha)` rather than
  `(red, green, blue)`, so that an element can be drawn translucent. Affects `Text.Color`
  and the color of a `UI.Message` (#1040)
- **Breaking:** `InputField.Changed` is only set when the user types in the field, and no
  longer when a client sets `InputField.Value`. `Changed` on the controls added in this
  release behaves the same way (#1040)
- **Breaking:** User interface elements are drawn using the skin that the game draws its
  own interface in, so the size and color that `Text` starts out with are no longer fixed
  values (#1040)
- **Breaking:** `Visible` is the element's own setting, rather than whether it and
  everything it is inside are visible. An element is only drawn when everything it is
  inside is visible as well, so an interface can be built with its parts visible, inside a
  panel that is not, and shown all at once (#1040)
- User interface elements can be created in every game scene, not just flight (#1040)
- User interface elements are removed when the game scene changes (#1040)
- Add `Interactable` to every control, to gray one out and stop it responding to the user
  without hiding it (#1040)
- Add `Toggle`, a check box, and `ToggleGroup`, which makes a set of toggles behave as
  radio buttons. A group refers to its toggles rather than containing them, so they can be
  grouped independently of how they are laid out (#1040)
- Add `Layout` to arrange the contents of a `Panel` in a row, a column or a grid, rather
  than positioning every element by hand. `Panel.AddHorizontalLayout`,
  `Panel.AddVerticalLayout` and `Panel.AddGridLayout` create one (#1040)
- Add `Slider`, `Dropdown` and `Image`. An image shows the contents of a PNG or JPEG file,
  or a plain colored rectangle (#1040)
- Add `Panel.Style`, to draw a panel as a box rather than a window, and `Panel.Color`.
  A box with a `Text` caption makes a group box (#1040)
- Add `Panel.Draggable`, so a panel can be moved by the user and used as a window (#1040)
- Add `ScrollView`, a view onto a panel larger than the space available for it, with
  scroll bars to move around it (#1040)
- Add `LayoutElement`, on every user interface object, for how much space it asks a layout
  for, and `Panel.SizeFitter`, for sizing a panel to fit what it contains (#1040)
- Add `InputField.Placeholder`, the hint drawn in an input field while it is empty (#1040)
- Add `InputField.ContentType`, filtering what the user can type into a field as they type
  it, so a numeric field never shows text a client has to reject, along with
  `InputField.CharacterLimit` and `InputField.ReadOnly` (#1040)
- Add `Color` to every control, tinting the sprite it is drawn with so a script can show a
  state of its own, an engaged autopilot for instance, by color (#1040)
- Add `Tooltip` to every control, a short piece of text shown beside the pointer while it
  rests on the control (#1040)
- Add `Panel.BringToFront`, to draw a panel in front of the elements beside it. A
  draggable panel also brings itself to the front when the user presses it, the way a
  window comes to the front when clicked (#1040)
- `AddSlider` can create a vertical slider, running from bottom to top (#1040)
- Add `Button.Pressed`, true while the user is holding the button down, so a script can
  repeat an action for as long as it is held (#1040)
- Add `Text.WordWrap`, so a value label can be kept to one line rather than wrapping and
  reflowing its layout as the value changes (#1040)
- Add `Image.SetPixels`, drawing a picture from raw pixels rather than a file, so a script
  can draw a graph, or anything else, and redraw it as often as it likes.
  `Image.UpdatePixels` redraws a block of the picture and leaves the rest, so a picture
  that changes a little at a time only sends what changed (#1040)
- Two user interface objects that refer to the same element now compare equal, so a
  client can tell whether it already has an element without keeping track of it (#1040)
- A canvas added with `UI.AddCanvas` follows the interface scale the player has set, as
  the stock canvas does, so the same interface is the same size on either (#1040)
- Fix `Text.Font` making a font of its own every time it is set, and leaving it behind when
  the label went. A font of a given name is made once and shared by the labels using it
  (#1040)
- A user interface object raises `KRPC.ObjectDestroyedException` once the element it
  stands for is gone, which removing it, `UI.Clear`, the client that made it disconnecting
  and changing scene all do. They previously failed on a destroyed game object, and were
  kept for the rest of the session (#1051)
- `RectTransform`, `Layout`, `LayoutElement` and `SizeFitter` do the same once the element
  they were taken from is gone, and are dropped with it rather than being kept for the rest
  of the session (#1051)
- Fix `RectTransform` objects accumulating for the rest of the session, one for every read
  of any object's `RectTransform`. Reading the same one twice now gives the same object
  (#1051)

## [v0.6.0]
- Fix locale issues with `UI.Message` (#993)

## [v0.3.5]
- Add `Canvas` class (#281)
- Add `UI.StockCanvas` to get the stock KSP UI canvas and `UI.AddCanvas` to create additional canvases
- Move `UI.AddPanel` and `UI.RectTransform` to `Canvas` class

## [v0.3.4]
- Initial version
