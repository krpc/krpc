import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();

// The three calls are in flight at once
const [altitude, speed, pitch] = await Promise.all([
  flightInfo.getMeanAltitude(),
  flightInfo.getSpeed(),
  flightInfo.getPitch(),
]);
console.log(altitude, speed, pitch);
