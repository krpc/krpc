import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const orbit = await vessel.getOrbit();
const frame = await (await orbit.getBody()).getNonRotatingReferenceFrame();

// [docs]
const direction = await vessel.direction(frame);
const prograde = await (await vessel.flight(frame)).getPrograde();
const angle = await conn.stdLib.vectorAngle(direction, prograde);
console.log(await conn.stdLib.radiansToDegrees(angle));
// [/docs]
await conn.close();
