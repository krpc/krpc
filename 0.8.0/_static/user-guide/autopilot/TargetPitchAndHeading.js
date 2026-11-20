import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ap = await vessel.getAutoPilot();
await ap.setReferenceFrame(await vessel.getSurfaceReferenceFrame());
// 80 degrees above the horizon, facing east
await ap.targetPitchAndHeading(80, 90);
await ap.setEngaged(true);
await ap.wait(); // blocks until the vessel has settled on the target
await ap.setEngaged(false);
// [/docs]
await conn.close();
