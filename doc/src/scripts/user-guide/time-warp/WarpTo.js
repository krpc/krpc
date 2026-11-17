import { connect } from "@krpc-org/krpc";

const conn = await connect();
const spaceCenter = conn.spaceCenter;
const vessel = await spaceCenter.getActiveVessel();

// [docs]
const orbit = await vessel.getOrbit();
const apoapsisUT =
  (await spaceCenter.getUT()) + (await orbit.getTimeToApoapsis());
await spaceCenter.warpTo(apoapsisUT - 10);
// [/docs]
await conn.close();
