import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Visual Debugging" });
const vessel = await conn.spaceCenter.getActiveVessel();

// Draw the axes of the surface reference frame
await conn.drawing.addAxes(await vessel.getSurfaceReferenceFrame());

// Draw the direction of the surface velocity, as a labeled arrow
const refFrame = await vessel.getSurfaceVelocityReferenceFrame();
const line = await conn.drawing.addDirectionFromCom([0, 1, 0], refFrame);
await line.setEndArrow(true);
await line.setLabel("Surface velocity");
while (true) {
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
