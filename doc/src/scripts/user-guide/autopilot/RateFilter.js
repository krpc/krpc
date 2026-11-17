import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ap = await vessel.getAutoPilot();
// notch a known 1.4 Hz pitch and yaw bending mode from the start
await ap.setPitchYawRateFilterMode(conn.spaceCenter.RateFilterMode.notch);
await ap.setPitchYawOscillationFrequency(1.4);
// [/docs]
await conn.close();
