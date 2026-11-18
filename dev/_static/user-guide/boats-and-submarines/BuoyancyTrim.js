import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Buoyancy Trim" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();

if (!(await body.getHasOcean())) {
  throw new Error(`${await body.getName()} has no ocean`);
}

const flight = await vessel.flight(await vessel.getSurfaceReferenceFrame());
const density = await body.getOceanDensity();
const gravity = await body.getSurfaceGravity();

// The fully submerged figures, which do not depend on where the vessel is
const buoyancy = await flight.buoyantForceAt(density, gravity);
const torque = await flight.buoyantTorqueAt(density, gravity);
const weight = (await vessel.getMass()) * gravity;
const displacement = await flight.getDisplacement();

console.log(`Displacement:  ${displacement.toFixed(1)} m^3`);
console.log(`Buoyancy:      ${buoyancy.toFixed(0)} N fully submerged`);
console.log(`Weight:        ${weight.toFixed(0)} N`);
console.log(`Floats:        ${buoyancy > weight}`);
console.log(
  `Torque:        ${Math.hypot(...torque).toFixed(0)} N m fully submerged`,
);

// The trim, read in the surface reference frame. Its x axis points up, so
// the y and z axes span the horizontal. A horizontal offset between the two
// centers tips the hull until they line up again.
const centerOfBuoyancy = await flight.getCenterOfBuoyancy();
/** @param {number} x */
const signed = (x) => (x >= 0 ? "+" : "") + x.toFixed(2);

if (centerOfBuoyancy === null) {
  console.log("Trim:          vessel is out of the water");
} else {
  const centerOfMass = await flight.getCenterOfMass();
  const north = centerOfBuoyancy[1] - centerOfMass[1];
  const east = centerOfBuoyancy[2] - centerOfMass[2];
  const submerged = 100 * (await flight.getSubmergedPortion());
  console.log(`Submerged:     ${submerged.toFixed(0)}%`);
  console.log(
    `Trim:          ${Math.hypot(north, east).toFixed(2)} m horizontally`,
  );
  console.log(`Trim north:    ${signed(north)} m`);
  console.log(`Trim east:     ${signed(east)} m`);
}
await conn.close();
