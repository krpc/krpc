import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Navball directions" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ap = await vessel.getAutoPilot();
await ap.setReferenceFrame(await vessel.getSurfaceReferenceFrame());
await ap.setEngaged(true);

// Point the vessel north on the navball, with a pitch of 0 degrees
await ap.setTargetDirection([0, 1, 0]);
await ap.wait();

// Point the vessel vertically upwards on the navball
await ap.setTargetDirection([1, 0, 0]);
await ap.wait();

// Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
await ap.setTargetDirection([0, 0, -1]);
await ap.wait();

await ap.setEngaged(false);
await conn.close();
