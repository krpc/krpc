import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Docking" });
const spaceCenter = conn.spaceCenter;
const drawing = conn.drawing;
const { ColorRGB } = conn.stdLib;
const vessel = await spaceCenter.getActiveVessel();
const control = await vessel.getControl();
const autoPilot = await vessel.getAutoPilot();
const parts = await vessel.getParts();
const port = await (await parts.getControlling()).getDockingPort();
const target = await spaceCenter.getTargetDockingPort();
const frame = await target.getReferenceFrame();
const vesselFrame = await vessel.getReferenceFrame();

// Turn to face the target port
console.log("Turning to face the target");
await control.setSAS(false);
await control.setRCS(true);
await autoPilot.setReferenceFrame(frame);
await autoPilot.setTargetDirection([0, -1, 0]);
await autoPilot.setEngaged(true);
await autoPilot.wait();

// Draw the docking axis, and a line from our port to the target
const axis = await drawing.addLine([0, 0, 0], [0, 50, 0], frame);
await axis.setColor(new ColorRGB({ r: 0, g: 1, b: 0 }));
const approach = await drawing.addLine([0, 0, 0], [0, 0, 0], frame);
await approach.setColor(new ColorRGB({ r: 1, g: 1, b: 0 }));

// Approach the target port along its axis
console.log("Approaching");
const position = await conn.addStream(() => port.position(frame));
const velocity = await conn.addStream(() => vessel.velocity(frame));
const { docking, docked } = spaceCenter.DockingPortState;
let state = await port.getState();
while (state !== docking && state !== docked) {
  const [x, y, z] = await position.get();
  await approach.setStart([x, y, z]);

  // Line up on the axis, then close in along it
  const offAxis = Math.hypot(x, z);
  const goalY = offAxis < 0.5 ? 0 : Math.max(y, 5);
  const [ex, ey, ez] = [-x, goalY - y, -z];
  const distance = Math.hypot(ex, ey, ez);
  const speed = Math.min(1, Math.max(0.1, 0.2 * distance));
  const scale = speed / Math.max(distance, 0.01);

  // Thrust towards the velocity we want, in the vessel's frame
  const [vx, vy, vz] = await velocity.get();
  const [right, forward, down] = await spaceCenter.transformDirection(
    [2 * (ex * scale - vx), 2 * (ey * scale - vy), 2 * (ez * scale - vz)],
    frame,
    vesselFrame,
  );
  await control.setRight(Math.max(-1, Math.min(1, right)));
  await control.setForward(Math.max(-1, Math.min(1, forward)));
  await control.setUp(Math.max(-1, Math.min(1, -down)));
  await new Promise((resolve) => setTimeout(resolve, 50));
  state = await port.getState();
}

// Stop the thrusters, and remove the drawing
await control.setRight(0);
await control.setForward(0);
await control.setUp(0);
await autoPilot.setEngaged(false);
await drawing.clear();
console.log("Docked");
await conn.close();
