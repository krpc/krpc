import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const flight = await vessel.flight(await vessel.getSurfaceReferenceFrame());
const centerOfBuoyancy = await flight.getCenterOfBuoyancy();
const centerOfMass = await flight.getCenterOfMass();
// [/docs]
if (centerOfBuoyancy !== null) {
  const north = centerOfBuoyancy[1] - centerOfMass[1];
  const east = centerOfBuoyancy[2] - centerOfMass[2];
  console.log(
    `Trim north = ${north.toFixed(2)} m, east = ${east.toFixed(2)} m`,
  );
}
await conn.close();
