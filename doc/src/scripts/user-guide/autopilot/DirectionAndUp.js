import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ap = await vessel.getAutoPilot();
await ap.setReferenceFrame(await vessel.getSurfaceReferenceFrame());
/** @type {[number, number, number]} */
const up = [0, 1, 0]; // north, in the surface frame, off the flight path

// straight up at the pad, roof toward north
await ap.setDirectionAndUp([1, 0, 0], up, 0);
await ap.setEngaged(true);
await ap.wait();

// ease over to the eastern horizon; roll stays defined through vertical
for (let i = 1; i <= 90; i++) {
  const pitch = ((90 - i) * Math.PI) / 180; // 89 down to 0 degrees
  await ap.setDirectionAndUp([Math.sin(pitch), 0, Math.cos(pitch)], up, 0);
  await ap.wait();
}
// [/docs]
await conn.close();
