import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const flight = await vessel.flight(await vessel.getSurfaceReferenceFrame());
// [docs]
const torque = await flight.buoyantTorqueAt(
  await body.getOceanDensity(),
  await body.getSurfaceGravity(),
);
// [/docs]
const components = torque.map((x) => x.toFixed(0)).join(", ");
console.log(`Torque = (${components}) N m`);
await conn.close();
