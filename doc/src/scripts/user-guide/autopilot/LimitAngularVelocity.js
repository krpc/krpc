import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ap = await vessel.getAutoPilot();
// limit the angular velocity by maxAngularVelocity only
await ap.setLimitAngularVelocity(false);
await ap.setMaxAngularVelocity([0.3, 0.5, 0.3]);
// [/docs]
await conn.close();
