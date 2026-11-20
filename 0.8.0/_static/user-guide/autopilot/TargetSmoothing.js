import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ap = await vessel.getAutoPilot();
await ap.setReferenceFrame(await vessel.getSurfaceReferenceFrame());
await ap.targetPitchAndHeading(90, 90); // start pointing straight up
await ap.setEngaged(true);
await ap.wait();

// ease a 10 degree pitch-over, 90 to 80, in over 20 seconds
await ap.setTargetSmoothingTime(20);
await ap.targetPitchAndHeading(80, 90);
await ap.wait(); // returns once the 80 degree target is reached
// [/docs]
await conn.close();
