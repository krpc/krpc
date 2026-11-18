import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const body = await (await vessel.getOrbit()).getBody();
const flight = await vessel.flight();
const displacement = await flight.getDisplacement();
const buoyancy = await flight.buoyantForceAt(
  await body.getOceanDensity(),
  await body.getSurfaceGravity(),
);
// [/docs]
console.log(
  `Displacement = ${displacement.toFixed(1)} m^3, ` +
    `buoyancy = ${buoyancy.toFixed(0)} N`,
);
await conn.close();
