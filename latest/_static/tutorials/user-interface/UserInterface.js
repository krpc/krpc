import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "User Interface Example" });
const { Padding } = conn.ui;
const { ColorRGBA } = conn.stdLib;
const canvas = await conn.ui.getStockCanvas();

// Get the size of the game window in pixels
const screenSize = await (await canvas.getRectTransform()).getSize();

// Add a panel to contain the UI elements. The user can drag it around the
// screen with the mouse.
const panel = await canvas.addPanel();
await panel.setDraggable(true);

// Position the panel on the left of the screen
const rect = await panel.getRectTransform();
await rect.setSize([200, 120]);
await rect.setPosition([110 - screenSize[0] / 2, 0]);

// Lay the contents of the panel out in a column
const layout = await panel.addVerticalLayout();
await layout.setPadding(
  new Padding({ left: 10, right: 10, top: 10, bottom: 10 }),
);
await layout.setSpacing(6);

// Add a button to set the throttle to maximum, with a tooltip shown while
// the mouse rests on it
const button = await panel.addButton("Full Throttle");
await button.setTooltip("Set the throttle to maximum");
await (await button.getLayoutElement()).setPreferredSize([-1, 30]);

// Add a slider to set the throttle by hand
const slider = await panel.addSlider();
await (await slider.getLayoutElement()).setPreferredSize([-1, 20]);

// Add some text displaying the total engine thrust
const text = await panel.addText("Thrust: 0 kN");
await text.setColor(new ColorRGBA({ r: 1, g: 1, b: 1, a: 1 }));
await text.setSize(18);

// Set up streams to monitor the button and the slider
const buttonClicked = await conn.addStream(() => button.getClicked());
const sliderChanged = await conn.addStream(() => slider.getChanged());

const vessel = await conn.spaceCenter.getActiveVessel();
const control = await vessel.getControl();
while (true) {
  // Handle the throttle button being clicked
  if (await buttonClicked.get()) {
    await control.setThrottle(1);
    await slider.setValue(1);
    await button.setClicked(false);
  }

  // Handle the user moving the throttle slider
  if (await sliderChanged.get()) {
    await control.setThrottle(await slider.getValue());
    await slider.setChanged(false);
  }

  // Update the thrust text
  const thrust = await vessel.getThrust();
  await text.setContent(`Thrust: ${Math.trunc(thrust / 1000)} kN`);

  await new Promise((resolve) => setTimeout(resolve, 100));
}
